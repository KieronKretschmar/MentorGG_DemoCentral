using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    public class Gatherer : Consumer<GathererTransferModel>
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly IDemoDownloader _demoDownloader;

        public Gatherer(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface, IDemoDownloader demoDownloader) : base(queueConnection)
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
                _demoDownloader.SendMessageAndUpdateStatus(matchId.ToString(), forwardModel);
            }
        }
    }
}
