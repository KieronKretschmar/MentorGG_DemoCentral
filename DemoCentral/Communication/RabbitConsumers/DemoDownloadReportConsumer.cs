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
    /// Messages are being processed by <see cref="DemoDownloadReportProcessor"/>.
    /// </summary>
    public class DemoDownloadReportConsumer : Consumer<DemoDownloadReport>
    {
        private readonly IServiceProvider _serviceProvider;
        private ILogger<DemoDownloadReportConsumer> _logger;

        public DemoDownloadReportConsumer(
            IServiceProvider serviceProvider,
            ILogger<DemoDownloadReportConsumer> logger,
            IQueueConnection queueConnection
            ) : base(queueConnection, 20)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Handle Download report.
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoDownloadReport model)
        {
            _logger.LogInformation($"Received {model.GetType()} for match [ {model.MatchId} ]. Message: [ {model.ToJson()} ]");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<DemoDownloadReportProcessor>();
                    await processor.WorkAsync(model);
                    return ConsumedMessageHandling.Done;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Failed to handle message from DemoDownloaderReport queue. [ {model} ]");
                return ConsumedMessageHandling.Resend;
            }
        }
    }
}
