using Database.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitCommunicationLib.Enums;
using RabbitMQ.Client.Events;
using DataBase.Enumerals;
using DemoCentral.Communication.HTTP;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace DemoCentral.Communication.Rabbit
{
    /// <summary>
    /// Consumer for the Gatherer queue
    /// If a message is received , <see cref="HandleMessage(IBasicProperties, GathererTransferModel)"/> is called
    /// and the message is forwarded to the demodownloader
    /// </summary>
    public class GathererConsumer : Consumer<DemoInsertInstruction>
    {
        private Logger<GathererConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GathererConsumer(
            IQueueConnection queueConnection,
            IServiceProvider serviceProvider) : base(queueConnection)
        {
            _logger = serviceProvider.GetRequiredService<Logger<GathererConsumer>>();
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handle downloadUrl from GathererQueue.
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoInsertInstruction model)
        {
            try
            {
                // Require the `GathererWorker` service upon receiving a message, ensuring a new instance and disposal.
                await _serviceProvider.GetRequiredService<GathererWorker>().WorkAsync(model);
                return ConsumedMessageHandling.Done;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Failed to handle message from DemoInsertInstruction queue. [ {model} ]");
                return ConsumedMessageHandling.Resend;
            }
        }
    }
}
