using Database.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public class SituationsOperator : Consumer<AnalyzerTransferModel>
    {
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly ILogger<SituationsOperator> _logger;

        public SituationsOperator(IQueueConnection queueConnection, IInQueueDBInterface inQueueDBInterface, ILogger<SituationsOperator> logger) : base(queueConnection)
        {
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }

        /// <summary>
        /// Handle response from SituationsOperator, update queue status
        /// </summary>
        public override void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.SituationsOperator, model.Success);

            string successString = model.Success ? "finished" : "failed";
            _logger.LogInformation($"Demo#{matchId} " + successString + "siutationsoperator");
        }
    }
}

