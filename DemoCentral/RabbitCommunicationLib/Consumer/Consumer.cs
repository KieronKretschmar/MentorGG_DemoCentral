
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
    public abstract class Consumer<TConsumeModel> : IHostedService where TConsumeModel: ITransferModel
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
        /// Handle the event if a Rabbit consumer receives a message.
        /// </summary>
        /// <param name="channel">AMQP Connection channel</param>
        /// <param name="ea">Event Arguments</param>
        protected virtual void OnConsumerReceived(IModel channel, BasicDeliverEventArgs ea)
        {
            // Let the overidden method handle the message and return a response.

            HandleMessage(
                ea.BasicProperties,
                TransferModelFactory<TConsumeModel>.FromBytes(ea.Body));

            channel.BasicAck(
                deliveryTag: ea.DeliveryTag,
                multiple: false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            channel = _queueConnection.Connection.CreateModel();

            channel.QueueDeclare(
                queue: _queueConnection.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            consumer = new EventingBasicConsumer(channel);

            // Subscribe to the Consumer Received event.
            consumer.Received += (model, ea) => OnConsumerReceived(channel, ea);

            channel.BasicConsume(
                queue: _queueConnection.QueueName,
                autoAck: false,
                consumer: consumer);

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            channel.ModelShutdown += (sender, ea) => {
                consumer.HandleModelShutdown((IModel)sender, ea);
            };

            channel.Dispose();
            _queueConnection.Connection.Dispose();

            await Task.CompletedTask;
        }
    }
}
