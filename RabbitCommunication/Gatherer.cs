using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;

namespace DemoCentral.RabbitCommunication
{
    public class Gatherer : Consumer<GathererTransferModel>
    {
        private readonly IDemoCentralDBInterface _dbInterface;

        public Gatherer(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface) : base(queueConnection)
        {
            _dbInterface = dbInterface;
        }

        protected override void HandleMessage(IBasicProperties properties, GathererTransferModel model)
        {
            long matchId = long.Parse(properties.CorrelationId);

            //TODO handle duplicate entry, currently not inserted into db and forgotten afterwards
            if (_dbInterface.TryCreateNewDemoEntryFromGatherer(matchId, model))
            {
                //TODO Send to DemoDownloader
            }
        }
    }
}
