using Database.Enumerals;
using DemoCentral.Communication.HTTP;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitCommunicationLib.Enums;
using RabbitMQ.Client.Events;

namespace DemoCentral.RabbitCommunication
{
    /// <summary>
    /// Consumer for the Gatherer queue
    /// If a message is received , <see cref="HandleMessage(IBasicProperties, GathererTransferModel)"/> is called
    /// and the message is forwarded to the demodownloader
    /// </summary>
    public class Gatherer : Consumer<DemoEntryInstructions>
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly IDemoDownloader _demoDownloader;
        private readonly ILogger<Gatherer> _logger;
        private IUserInfoOperator _userInfoOperator;
        private IInQueueDBInterface _inQueueDBInterface;

        public Gatherer(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface, IDemoDownloader demoDownloader, IUserInfoOperator userInfoOperator, ILogger<Gatherer> logger, IInQueueDBInterface inQueueDBInterface) : base(queueConnection)
        {

            _dbInterface = dbInterface;
            _demoDownloader = demoDownloader;
            _userInfoOperator = userInfoOperator;
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }

        /// <summary>
        /// Handle downloadUrl from GathererQueue, create new entry and send to downloader if unique, else delete and forget
        /// </summary>
        public async override Task HandleMessageAsync(BasicDeliverEventArgs ea, DemoEntryInstructions model)
        {
            AnalyzerQuality requestedQuality = await _userInfoOperator.GetAnalyzerQualityAsync(model.UploaderId);
            //TODO OPTIONAL FEATURE handle duplicate entry
            //Currently not inserted into db and forgotten afterwards
            //Maybe saved to special table or keep track of it otherwise
            if (_dbInterface.TryCreateNewDemoEntryFromGatherer(model, requestedQuality, out long matchId))
            {
                var forwardModel = new DemoDownloadInstructions
                {
                    DownloadUrl = model.DownloadUrl
                };

                _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);

                _demoDownloader.SendMessageAndUpdateStatus(matchId.ToString(), forwardModel);

                _logger.LogInformation($"Demo#{matchId} assigned to {model.DownloadUrl}");
            }
            else
            {
                _logger.LogInformation($"DownloadUrl {model.DownloadUrl} was duplicate of Demo#{matchId}");
            }
        }
    }
}
