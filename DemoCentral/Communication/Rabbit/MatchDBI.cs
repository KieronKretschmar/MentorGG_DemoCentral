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
    public class MatchDBI : Consumer<TaskCompletedReport>
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly ILogger<MatchDBI> _logger;

        public MatchDBI(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface, ILogger<MatchDBI> logger) : base(queueConnection)
        {
            _dbInterface = dbInterface;
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
                _logger.LogError(e, $"Could not update demo #{model.MatchId} from matchDBI response");
                return Task.FromResult(ConsumedMessageHandling.ThrowAway);
            }

            return Task.FromResult(ConsumedMessageHandling.Done);
        }

        private void UpdateDBFromResponse(TaskCompletedReport model)
        {
            long matchId = model.MatchId;
            var dbDemo = _dbInterface.GetDemoById(matchId);
            _dbInterface.SetUploadStatus(dbDemo, model.Success);

            if (model.Success)
            {
                _dbInterface.SetDatabaseVersion(dbDemo, model.Version);
            }

            string log = model.Success ? "was uploaded" : "failed upload";
            _logger.LogInformation($"Demo #{matchId} " + log);
        }
    }
}
