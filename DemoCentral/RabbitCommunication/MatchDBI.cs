using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public interface IMatchDBI
    {
        /// <summary>
        /// Handle response from  MatchDBI, update upload status
        /// </summary>
        void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model);
    }

    public class MatchDBI : Consumer<AnalyzerTransferModel>, IMatchDBI
    {
        private readonly IDemoCentralDBInterface _dbInterface;

        public MatchDBI(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface) : base(queueConnection)
        {
            _dbInterface = dbInterface;
        }

        /// <summary>
        /// Handle response from  MatchDBI, update upload status
        /// </summary>
        public override void HandleMessage(IBasicProperties properties, AnalyzerTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);
            _dbInterface.SetUploadStatus(matchId, model.Success);
        }
    }
}
