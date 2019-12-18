using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;

namespace DemoCentral.RabbitCommunication
{
    /// <summary>
    /// SituationsOperator Consumer
    /// This receives all the messages from the SO_DC Queue and updates their queue status
    /// </summary>
    public class SituationsOperator : Consumer<AnalyzerTransferModel>
    {
        private readonly IInQueueDBInterface _inQueueDBInterface;

        public SituationsOperator(IQueueConnection queueConnection, IInQueueDBInterface inQueueDBInterface) : base(queueConnection)
        {
            _inQueueDBInterface = inQueueDBInterface;
        }

        protected override void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _inQueueDBInterface.UpdateQueueStatus(matchId, "SO", model.Success);
        }
    }
}

