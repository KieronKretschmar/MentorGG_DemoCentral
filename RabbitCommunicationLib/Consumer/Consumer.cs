
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitTransfer;
using RabbitTransfer.Interfaces;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static TransferModel;

namespace RabbitTransfer.Consumer
{
    /// <summary>
    /// An Abstract IHostedService AMQP Consumer with managed Start and Stop calls.
    /// </summary>
    /// <typeparam name="TConsumeModel">Tranfer Model to consume.</typeparam>
    public abstract class Consumer<TConsumeModel> : IHostedService
        where TConsumeModel : ITransferModel
    {
        /// <summary>
        /// Connection to the AMQP Rabbit Instance.
        /// </summary>
        private readonly IQueueConnection _queueConnection;

        /// <summary>
        /// Connection channel.
        /// </summary>
        private IModel channel;

        /// <summary>
        /// AMQP Rabbit Consumer.
        /// </summary>
        private EventingBasicConsumer consumer;


        /// <summary>
        /// Set the AMQP Connection.
        /// </summary>
        protected Consumer(IQueueConnection queueConnection)
        {
            _queueConnection = queueConnection;
        }

        /// <summary>
        /// Abstract method to handle a Transfer Model.
        /// </summary>
        /// <param name="properties">AMQP Properties</param>
        /// <param name="model">Received message</param>
        protected abstract void HandleMessage(IBasicProperties properties, TConsumeModel model);


        /// <summary>
        /// Basic Acknowledge a message
        /// </summary>
        /// <param name="ea">Event Arguments</param>
        protected virtual void BasicAcknowledge(BasicDeliverEventArgs ea)
        {
            channel.BasicAck(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false);
        }

        /// <summary>
        /// Handle the event if a Rabbit consumer receives a message.
        /// </summary>
        /// <param name="channel">AMQP Connection channel</param>
        /// <param name="ea">Event Arguments</param>
        protected virtual void OnConsumerReceived(IModel channel, BasicDeliverEventArgs ea)
        {
            // If a model cannot be obtained from the body,
            // acknowledge the message as no work can be done with an invalid model..
            TConsumeModel model;
            try
            {
                model = TransferModelFactory<TConsumeModel>.FromBytes(ea.Body);
            }
            catch
            {
                BasicAcknowledge(ea);
                throw;
            }

            // If a message was Redelivered, acknowledge before handling,

            // WARNING: If the application crashes, the message will be acknowledged
            //          and will not be resent.
            if (ea.Redelivered)
            {
                BasicAcknowledge(ea);
                HandleMessage(ea.BasicProperties, model);
            }
            // If a message has not been Redilivered attempt to handle it, then acknowledge.
            else
            {
                HandleMessage(ea.BasicProperties, model);
                BasicAcknowledge(ea);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            channel = _queueConnection.Connection.CreateModel();

            channel.QueueDeclare(
                queue: _queueConnection.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            consumer = new EventingBasicConsumer(channel);

            // Subscribe to the Consumer Received event.
            consumer.Received += (model, ea) => OnConsumerReceived(channel, ea);

            channel.BasicConsume(
                queue: _queueConnection.Queue,
                autoAck: false,
                consumer: consumer);

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            channel.ModelShutdown += (sender, ea) =>
            {
                consumer.HandleModelShutdown((IModel) sender, ea);
            };

            channel.Dispose();
            _queueConnection.Connection.Dispose();
            await Task.CompletedTask;
        }
    }
}
