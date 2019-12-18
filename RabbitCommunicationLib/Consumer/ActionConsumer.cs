using RabbitMQ.Client;
using RabbitTransfer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitTransfer.Consumer
{
    /// <summary>
    /// Provide a concrete implementation of a consumer so the <see cref="Consumer{TConsumeModel}.HandleMessage(IBasicProperties, TConsumeModel)"/> function can be overwritten
    /// </summary>
    /// <typeparam name="TConsumeModel"></typeparam>
    public class ActionConsumer<TConsumeModel> : Consumer<TConsumeModel>
        where TConsumeModel : ITransferModel
    {
        /// <summary>
        /// Store the function which handles the received message
        /// </summary>
        private readonly Action<IBasicProperties, TConsumeModel> _handleMessageAction;

        /// <summary>
        /// Create a Consumer, which <see cref="HandleMessage(IBasicProperties, TConsumeModel)"/> function can be overwritten via an action passed to the controller
        /// </summary>
        /// <param name="queueConnection"></param>
        /// <param name="handleReply">function, which is going to handle the received message</param>
        public ActionConsumer(IQueueConnection queueConnection, Action<IBasicProperties, TConsumeModel> handleReply) : base(queueConnection)
        {
            _handleMessageAction = handleReply;
        }

        protected override void HandleMessage(IBasicProperties properties, TConsumeModel model)
        {
            _handleMessageAction(properties, model);
        }
    }
}
