using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public class Gatherer : Consumer<GathererTransferModel>
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly DemoDownloader _demoDownloader;

        public Gatherer(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface, DemoDownloader demoDownloader) : base(queueConnection)
        {

            _dbInterface = dbInterface;
            _demoDownloader = demoDownloader;
        }

        public override void HandleMessage(IBasicProperties properties, GathererTransferModel model)
        {
            //TODO handle duplicate entry, currently not inserted into db and forgotten afterwards
            if (_dbInterface.TryCreateNewDemoEntryFromGatherer(model, out long matchId))
            {
                var forwardModel = new DC_DD_Model
                {
                    DownloadUrl = model.DownloadUrl
                };
                _demoDownloader.PublishMessage(matchId.ToString(), forwardModel);
            }
        }
    }
}
