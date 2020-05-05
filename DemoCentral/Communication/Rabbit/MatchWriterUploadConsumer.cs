using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RabbitCommunicationLib.Enums;
using System;

namespace DemoCentral.Communication.Rabbit
{
    /// <summary>
    /// Handle Upload-TaskCompletedReports from MatchWriter
    /// </summary>
    public class MatchWriterUploadReportConsumer : Consumer<TaskCompletedReport>
    {
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly ILogger<MatchWriterUploadReportConsumer> _logger;

        public MatchWriterUploadReportConsumer(
            IQueueConnection queueConnection, 
            IDemoTableInterface demoTableInterface, 
            ILogger<MatchWriterUploadReportConsumer> logger
            ) : base(queueConnection)
        {
            _demoTableInterface = demoTableInterface;
            _logger = logger;
        }

        /// <summary>
        /// Handle response from  MatchDBI, update upload status, set database version
        /// </summary>
        public override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport model)
        {
            try
            {
                UpdateDBFromResponse(model);

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not update demo [ {model.MatchId} ] from matchDBI response");
                return Task.FromResult(ConsumedMessageHandling.ThrowAway);
            }

            return Task.FromResult(ConsumedMessageHandling.Done);
        }

        private void UpdateDBFromResponse(TaskCompletedReport model)
        {
            long matchId = model.MatchId;
            var dbDemo = _demoTableInterface.GetDemoById(matchId);
            _demoTableInterface.SetUploadStatus(dbDemo, model.Success);

            if (model.Success)
            {
                _demoTableInterface.SetDatabaseVersion(dbDemo, model.Version);
            }

            string log = model.Success ? "was uploaded" : "failed upload";
            _logger.LogInformation($"Demo [ {matchId} ] " + log);
        }
    }
}
