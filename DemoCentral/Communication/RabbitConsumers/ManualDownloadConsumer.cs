using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using DemoCentral.Communication.HTTP;
using Microsoft.Extensions.DependencyInjection;
using DemoCentral.Communication.MessageProcessors;

namespace DemoCentral.Communication.RabbitConsumers
{
    /// <summary>
    /// Consumer for the ManualDownload report queue.
    /// Messages are being processed by <see cref="ManualDownloadReportProcessor"/>.
    /// </summary>
    public class ManualDownloadConsumer : Consumer<ManualDownloadReport>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ManualDownloadConsumer> _logger;

        public ManualDownloadConsumer(
            IServiceProvider serviceProvider,
            ILogger<ManualDownloadConsumer> logger,
            IQueueConnection queueConnection
            ) : base(queueConnection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, ManualDownloadReport model)
        {
            _logger.LogInformation($"Received {model.GetType()}. Message: [ {model.ToJson()} ]");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<ManualDownloadReportProcessor>();
                    await processor.WorkAsync(model);
                    return ConsumedMessageHandling.Done;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Failed to handle message from ManualDownloadReport queue. [ {model} ]");
                return ConsumedMessageHandling.Resend;
            }
        }
    }
}
