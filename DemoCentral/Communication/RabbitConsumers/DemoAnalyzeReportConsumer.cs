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
    /// Consumer for the DemoFileWorker report queue.
    /// Messages are being processed by <see cref="DemoAnalyzeReportProcessor"/>.
    /// </summary>
    public class DemoAnalyzeReportConsumer : Consumer<DemoAnalyzeReport>
    {

        private readonly IServiceProvider _serviceProvider;
        private ILogger<DemoAnalyzeReportConsumer> _logger;

        public DemoAnalyzeReportConsumer(
            IServiceProvider serviceProvider,
            ILogger<DemoAnalyzeReportConsumer> logger,
            IQueueConnection queueConnection
            ) : base(queueConnection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
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
                    var processor = scope.ServiceProvider.GetRequiredService<DemoAnalyzeReportProcessor>();
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
