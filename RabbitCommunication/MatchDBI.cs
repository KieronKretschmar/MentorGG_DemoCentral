using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;

namespace DemoCentral.RabbitCommunication
{
    public class MatchDBI : Consumer<AnalyzerTransferModel>
    {
        private readonly DemoCentralDBInterface _dbInterface;

        public MatchDBI(IQueueConnection queueConnection, DemoCentralDBInterface dbInterface) : base(queueConnection)
        {
            _dbInterface = dbInterface;
        }


        protected override void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _dbInterface.UpdateUploadStatus(matchId, model.Success);
        }
    }
}
