using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitTransfer.Interfaces;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitTransfer.Producer
{
    public class Producer<TProduceModel> : IHostedService
        where TProduceModel: ITransferModel
    {
        /// <summary>
        /// Connection to the AMQP Rabbit Instance.
        /// </summary>
        private readonly IQueueConnection _queueConnection;

        /// <summary>
        /// Bool, whether messages should be saved to disk
        /// </summary>
        private readonly bool _persistentMessageSending;

        /// <summary>
        /// Connection channel.
        /// </summary>
        private IModel channel;



        /// <summary>
        /// Set the AMQP Connection.
        /// </summary>
        /// <param name="connection"></param>
        public Producer(IQueueConnection queueConnection, bool persistentMessageSending = true)
        {
            _queueConnection = queueConnection;
            _persistentMessageSending = persistentMessageSending;
        }

        /// <summary>
        /// Publish a message to the Queue Channel.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="produceModel"></param>
        public void PublishMessage(string correlationId, TProduceModel produceModel)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            props.CorrelationId = correlationId;

            byte[] messageBody = produceModel.ToBytes();

            props.Persistent = _persistentMessageSending;

            channel.BasicPublish(
                exchange: "",
                routingKey: _queueConnection.Queue,
                basicProperties: props,
                body: messageBody);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            channel = _queueConnection.Connection.CreateModel();

            channel.QueueDeclare(
                queue: _queueConnection.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            channel.Dispose();
            _queueConnection.Connection.Dispose();
            await Task.CompletedTask;
        }
    }
}
