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
using DemoCentral.Communication.MessageProcessors;

namespace DemoCentral.Communication.RabbitConsumers
{
    /// <summary>
    /// Consumer for the Gatherer queue.
    /// Messages are being processed by <see cref="GathererProcessor"/>.
    /// </summary>
    public class GathererConsumer : Consumer<DemoInsertInstruction>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GathererConsumer> _logger;

        public GathererConsumer(
            IServiceProvider serviceProvider,
            ILogger<GathererConsumer> logger,
            IQueueConnection queueConnection
            ) : base(queueConnection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Handle downloadUrl from GathererQueue.
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoInsertInstruction model)
        {
            _logger.LogInformation($"Received {model.GetType()}. Message: [ {model.ToJson()} ]");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<GathererProcessor>();
                    await processor.WorkAsync(model);
                    return ConsumedMessageHandling.Done;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Failed to handle message from DemoInsertInstruction queue. [ {model} ]");
                return ConsumedMessageHandling.Resend;
            }
        }
    }
}
