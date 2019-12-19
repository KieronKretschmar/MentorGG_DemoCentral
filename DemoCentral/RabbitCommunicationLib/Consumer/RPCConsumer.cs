using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitTransfer.Interfaces;
using RabbitTransfer.Producer;
using System;
using System.Collections.Generic;
using System.Text;
using static TransferModel;

namespace RabbitTransfer.Consumer
{
    /// <summary>
    /// An Abstract IHostedService AMQP RPC Consumer with managed Start and Stop calls.
    /// </summary>
    /// <typeparam name="TConsumeModel">TransferModel to consume.</typeparam>
    /// <typeparam name="TProduceModel">Transfer Model to produce.</typeparam>
    public abstract class RPCConsumer<TConsumeModel, TProduceModel> : Consumer<TConsumeModel>
        where TConsumeModel: ITransferModel
        where TProduceModel: ITransferModel
    {
        /// <summary>
        /// Producer for publishing replies.
        /// </summary>
        private readonly Producer<TProduceModel> producer;

        /// <summary>
        /// Set the AMQP Connection.
        /// </summary>
        public RPCConsumer(IQueueConnection queueConnection) : base(queueConnection)
        {
            producer = new Producer<TProduceModel>(queueConnection);
        }

        /// <summary>
        /// Handle the event if a Rabbit consumer receives a message.
        /// </summary>
        /// <param name="channel">AMQP Connection channel</param>
        /// <param name="ea">Event Arguments</param>
        protected override void OnConsumerReceived(IModel channel, BasicDeliverEventArgs ea)
        {
            // Let the overidden method handle the message and return a response.

            TProduceModel replyModel = HandleMessageAndReply(
                ea.BasicProperties,
                TransferModelFactory<TConsumeModel>.FromBytes(ea.Body));

            channel.BasicAck(
                deliveryTag: ea.DeliveryTag,
                multiple: false);

            // Publish reply message

            producer.PublishMessage(ea.BasicProperties.CorrelationId, replyModel);
        }

        /// <summary>
        /// Abstract method to handle a Transfer Model and reply.
        /// </summary>
        /// <param name="properties">AMQP Properties</param>
        /// <param name="model">Received message</param>
        protected abstract TProduceModel HandleMessageAndReply(IBasicProperties properties, TConsumeModel model);

        /// <summary>
        /// Warning: InvalidOperation - Use `HandleMessageAndReply`.
        /// </summary>
        /// <param name="properties">AMQP Properties</param>
        /// <param name="model">Received message</param>
        protected override void HandleMessage(IBasicProperties properties, TConsumeModel model)
        {
            throw new InvalidOperationException("RPC Consumer expects a replyMessage, use HandleMessageAndReply!");
        }
    }
}
