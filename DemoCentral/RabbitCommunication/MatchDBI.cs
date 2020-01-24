using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public class MatchDBI : Consumer<AnalyzerTransferModel>
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
        public override void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _dbInterface.SetUploadStatus(matchId, model.Success);

            if (model.Success)
            {
                _dbInterface.SetDatabaseVersion(matchId, model.AnalyzerVersion);
            }

            string log = model.Success ? "was uploaded" : "failed upload";
            _logger.LogInformation($"Demo#{matchId} " + log);
        }
    }
}
