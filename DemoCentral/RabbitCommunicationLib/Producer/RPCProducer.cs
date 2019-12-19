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
    /// </summary>
    /// <typeparam name="TProduceModel">ITransferModel to sent out</typeparam>
    /// <typeparam name="TConsumeModel">ITransferModel to receive</typeparam>
    public abstract class RPCProducer<TProduceModel, TConsumeModel> : Producer<TProduceModel>
        where TProduceModel : ITransferModel
        where TConsumeModel : ITransferModel
    {
        private readonly string _replyQueue;
        private readonly IConnection _connection;
        private readonly ReplyConsumer<TConsumeModel> consumer;

        /// <summary>
        /// Set the AMQP Connection.
        /// </summary>
        /// <param name="connection"></param>
        protected RPCProducer(IQueueReplyQueueConnection queueConnection, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _replyQueue = queueConnection.ReplyQueue;
            _connection = queueConnection.Connection;

            //Reverse the cnonnection, so the consumer listens to the response queue
            ReplyConnection reversedConnection = new ReplyConnection(queueConnection);

            consumer = new ReplyConsumer<TConsumeModel>(reversedConnection,HandleReply);
        }

        /// <summary>
        /// Abstract method to handle a message.
        /// </summary>
        /// <param name="properties">headers of the message</param>
        /// <param name="consumeModel">transfermodel of the message</param>
        public abstract void HandleReply(IBasicProperties properties, TConsumeModel consumeModel);

        public new async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
            //Start consuming 
            await consumer.StartAsync(cancellationToken);
        }

        public new async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken); 
            //stop consuming
            await consumer.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Provide a concrete implementation of a consumer so the <see cref="Consumer{TConsumeModel}.HandleMessage(IBasicProperties, TConsumeModel)"/> function can be overwritten
    /// </summary>
    /// <typeparam name="TConsumeModel"></typeparam>
    internal class ReplyConsumer<TConsumeModel> : Consumer<TConsumeModel> where TConsumeModel : ITransferModel
    {
        /// <summary>
        /// Store the function which handles the received message
        /// </summary>
        private readonly Action<IBasicProperties,TConsumeModel> _handleReply;

        /// <summary>
        /// Create a Consumer, which <see cref="HandleMessage(IBasicProperties, TConsumeModel)"/> function can be overwritten via an action passed to the controller
        /// </summary>
        /// <param name="queueConnection"></param>
        /// <param name="handleReply">function, which is going to handle the received message</param>
        public ReplyConsumer(IQueueConnection queueConnection, Action<IBasicProperties,TConsumeModel> handleReply) : base(queueConnection)
        {
            _handleReply = handleReply;
        }

        protected override void HandleMessage(IBasicProperties properties, TConsumeModel model)
        {
            _handleReply(properties, model);
        }
    }

    /// <summary>
    /// Reverses the <see cref="IQueueReplyQueueConnection"/> , so the ReplyQueue is the <see cref="IQueueConnection.QueueName"/>.
    /// </summary>
    internal class ReplyConnection : IQueueConnection
    {
        public IConnection Connection { get ; set; }
        public string QueueName { get; set; }

        public ReplyConnection(IQueueReplyQueueConnection queueConnection)
        {
            Connection = queueConnection.Connection;
            QueueName = queueConnection.ReplyQueue;
        }
    }
}
