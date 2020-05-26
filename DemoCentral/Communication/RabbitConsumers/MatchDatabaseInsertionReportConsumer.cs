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
    /// Consumer for the MatchWriter upload report queue.
    /// Messages are being processed by <see cref="MatchDatabaseInsertionReportProcessor"/>.
    /// </summary>
    public class MatchDatabaseInsertionReportConsumer : Consumer<TaskCompletedReport>
    {

        private readonly IServiceProvider _serviceProvider;
        private ILogger<MatchDatabaseInsertionReportConsumer> _logger;

        public MatchDatabaseInsertionReportConsumer(
            IServiceProvider serviceProvider,
            ILogger<MatchDatabaseInsertionReportConsumer> logger,
            IQueueConnection queueConnection
            ) : base(queueConnection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Handle Upload report.
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport model)
        {
            _logger.LogInformation($"Received {model.GetType()} for match [ {model.MatchId} ]. Message: [ {model.ToJson()} ]");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<MatchDatabaseInsertionReportProcessor>();
                    await processor.WorkAsync(model);
                    return ConsumedMessageHandling.Done;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Failed to handle message from MatchWriter Upload Report queue. [ {model} ]");
                return ConsumedMessageHandling.Resend;
            }
        }
    }
}
