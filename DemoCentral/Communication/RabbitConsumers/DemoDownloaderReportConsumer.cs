﻿using DemoCentral.Communication.MessageProcessors;
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
    public class DemoDownloaderReportConsumer : Consumer<DemoObtainReport>
    {

        private ILogger<DemoDownloaderReportConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DemoDownloaderReportConsumer(
            IQueueConnection queueConnection,
            ILogger<DemoDownloaderReportConsumer> logger,
            IServiceProvider serviceProvider) : base(queueConnection)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handle Download report.
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoObtainReport model)
        {
            _logger.LogInformation($"Received {model.GetType()} for match [ {model.MatchId} ]. Message: [ {model.ToJson()} ]");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<DemoDownloaderReportProcessor>();
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
