using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public class SituationsOperator : Consumer<AnalyzerTransferModel>
    {
        private readonly IInQueueDBInterface _inQueueDBInterface;

        public SituationsOperator(IQueueConnection queueConnection, IInQueueDBInterface inQueueDBInterface) : base(queueConnection)
        {
            _inQueueDBInterface = inQueueDBInterface;
        }

        /// <summary>
        /// Handle response from SituationsOperator, update queue status
        /// </summary>
        public override void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _inQueueDBInterface.UpdateQueueStatus(matchId, "SO", model.Success);
        }
    }
}

