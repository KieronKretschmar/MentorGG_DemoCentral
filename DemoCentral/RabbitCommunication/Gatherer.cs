using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public interface IGatherer
    {
        /// <summary>
        /// Handle downloadUrl from GathererQueue, create new entry and send to downloader if unique, else delete and forget
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="model"></param>
        void HandleMessage(IBasicProperties properties, GathererTransferModel model);
    }

    public class Gatherer : Consumer<GathererTransferModel>, IGatherer
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly DemoDownloader _demoDownloader;

        public Gatherer(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface, DemoDownloader demoDownloader) : base(queueConnection)
        {

            _dbInterface = dbInterface;
            _demoDownloader = demoDownloader;
        }

        /// <summary>
        /// Handle downloadUrl from GathererQueue, create new entry and send to downloader if unique, else delete and forget
        /// </summary>
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
