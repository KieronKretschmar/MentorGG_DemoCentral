using Database.Enumerals;
using DemoCentral.Communication.HTTP;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;

namespace DemoCentral.RabbitCommunication
{
    /// <summary>
    /// Consumer for the Gatherer queue
    /// If a message is received , <see cref="HandleMessage(IBasicProperties, GathererTransferModel)"/> is called
    /// and the message is forwarded to the demodownloader
    /// </summary>
    public class Gatherer : Consumer<GathererTransferModel>
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly IDemoDownloader _demoDownloader;
        private readonly ILogger<Gatherer> _logger;
        private IUserInfo _userInfo;

        public Gatherer(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface, IDemoDownloader demoDownloader,IUserInfo userInfo, ILogger<Gatherer> logger) : base(queueConnection)
        {

            _dbInterface = dbInterface;
            _demoDownloader = demoDownloader;
            _userInfo = userInfo;
            _logger = logger;
        }

        /// <summary>
        /// Handle downloadUrl from GathererQueue, create new entry and send to downloader if unique, else delete and forget
        /// </summary>
        public async override void HandleMessage(IBasicProperties properties, GathererTransferModel model)
        {
            AnalyzerQuality currentQuality = await _userInfo.GetAnalyzerQualityAsync(model.UploaderId);
            //TODO OPTIONAL FEATURE handle duplicate entry
            //Currently not inserted into db and forgotten afterwards
            //Maybe saved to special table or keep track of it otherwise
            if (_dbInterface.TryCreateNewDemoEntryFromGatherer(model,currentQuality,  out long matchId))
            {
                var forwardModel = new DC_DD_Model
                {
                    DownloadUrl = model.DownloadUrl
                };
                _demoDownloader.SendMessageAndUpdateStatus(matchId.ToString(), forwardModel);

                _logger.LogInformation($"Demo#{matchId} assigned to {model.DownloadUrl}");
            }
            _logger.LogInformation($"DownloadUrl {model.DownloadUrl} was duplicate of Demo#{matchId}");
        }
    }
}
