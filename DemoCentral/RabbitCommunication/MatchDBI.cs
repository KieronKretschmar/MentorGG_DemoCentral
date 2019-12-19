using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public class MatchDBI : Consumer<AnalyzerTransferModel>
    {
        private readonly IDemoCentralDBInterface _dbInterface;

        public MatchDBI(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface) : base(queueConnection)
        {
            _dbInterface = dbInterface;
        }

        public override void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _dbInterface.UpdateUploadStatus(matchId, model.Success);
        }
    }
}
