using Database.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitCommunicationLib.Enums;
using RabbitMQ.Client.Events;
using Database.Enumerals;
using DemoCentral.Communication.HTTP;
using System;
using Microsoft.Extensions.DependencyInjection;
using DemoCentral.Communication.MessageProcessors;

namespace DemoCentral.Communication.RabbitConsumers
{
    /// <summary>
    /// Consumer for the Gatherer instruction queue.
    /// Messages are being processed by <see cref="GathererInstructionProcessor"/>.
    /// </summary>
    public class GathererInstructionConsumer : Consumer<DemoInsertInstruction>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GathererInstructionConsumer> _logger;

        public GathererInstructionConsumer(
            IServiceProvider serviceProvider,
            ILogger<GathererInstructionConsumer> logger,
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
                    var processor = scope.ServiceProvider.GetRequiredService<GathererInstructionProcessor>();
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
