using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitTransfer.Producer
{

    /// <summary>
    /// An Abstract IHostedService AMQP RPC Consumer with managed Start and Stop calls.
    ///
    /// Use this Producer if you want to send a message and receive a reply.
    /// </summary>
    /// <typeparam name="TProduceModel">Transfer Model to produce</typeparam>
    /// <typeparam name="TConsumeModel">Transfer Model to consume</typeparam>
    public abstract class RPCClient<TProduceModel, TConsumeModel> : IHostedService
        where TProduceModel : ITransferModel
        where TConsumeModel : ITransferModel
    {

        private readonly ActionConsumer<TConsumeModel> consumer;
        private readonly Producer<TProduceModel> producer;

        /// <summary>
        /// Set the AMQP Connection.
        /// </summary>
        /// <param name="connection"></param>
        protected RPCClient(
            IRPCQueueConnections queueConnections,
            bool persistantMessageSending = true)
        {
            producer = new Producer<TProduceModel>(
                queueConnections.ProduceConnection,
                persistantMessageSending);

            consumer = new ActionConsumer<TConsumeModel>(
                queueConnections.ConsumeConnection,
                HandleMessage);
        }

        /// <summary>
        /// Publish a message using the attached producer.
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="produceModel"></param>
        public void PublishMessage(string correlationId, TProduceModel produceModel)
            => producer.PublishMessage(correlationId, produceModel);

        /// <summary>
        /// Abstract method to handle a Transfer Model. (A reply in this context)
        ///
        /// Overide this method to get the functionality.
        /// </summary>
        /// <param name="properties">headers of the message</param>
        /// <param name="consumeModel">Transfer Model of the message</param>
        public abstract void HandleMessage(IBasicProperties properties, TConsumeModel consumeModel);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await producer.StartAsync(cancellationToken);
            await consumer.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await producer.StopAsync(cancellationToken);
            await consumer.StopAsync(cancellationToken);
        }
    }
}
