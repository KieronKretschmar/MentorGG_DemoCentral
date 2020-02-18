using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;

namespace DemoCentral.RabbitCommunication
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
        public override Task HandleMessageAsync(IBasicProperties properties, TaskCompletedReport model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _dbInterface.SetUploadStatus(matchId, model.Success);

            if (model.Success)
            {
                _dbInterface.SetDatabaseVersion(matchId, model.Version);
            }

            string log = model.Success ? "was uploaded" : "failed upload";
            _logger.LogInformation($"Demo#{matchId} " + log);

            return Task.CompletedTask;
        }
    }
}
