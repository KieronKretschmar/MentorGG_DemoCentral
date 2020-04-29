using DemoCentral.Communication.MessageProcessors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.Communication.RabbitConsumers
{
    /// <summary>
    /// Consumer for the DemoDownloader report queue.
    /// </summary>
    public class DemoFileWorkerReportConsumer : Consumer<DemoAnalyzeReport>
    {

        private ILogger<DemoFileWorkerReportConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DemoFileWorkerReportConsumer(
            IQueueConnection queueConnection,
            ILogger<DemoFileWorkerReportConsumer> logger,
            IServiceProvider serviceProvider) : base(queueConnection)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handle Download report.
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoAnalyzeReport model)
        {
            _logger.LogInformation($"Received {model.GetType()} for match [ {model.MatchId} ]. Message: [ {model.ToJson()} ]");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<DemoFileWorkerReportProcessor>();
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
