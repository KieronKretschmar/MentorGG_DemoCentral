using Database.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitCommunicationLib.Enums;
using RabbitMQ.Client.Events;
using DataBase.Enumerals;

namespace DemoCentral.RabbitCommunication
{
    /// <summary>
    /// Consumer for the Gatherer queue
    /// If a message is received , <see cref="HandleMessage(IBasicProperties, GathererTransferModel)"/> is called
    /// and the message is forwarded to the demodownloader
    /// </summary>
    public class Gatherer : Consumer<DemoInsertInstruction>
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly IDemoDownloader _demoDownloader;
        private readonly ILogger<Gatherer> _logger;
        private IInQueueDBInterface _inQueueDBInterface;

        public Gatherer(IQueueConnection queueConnection, IDemoCentralDBInterface dbInterface, IDemoDownloader demoDownloader, ILogger<Gatherer> logger, IInQueueDBInterface inQueueDBInterface) : base(queueConnection)
        {

            _dbInterface = dbInterface;
            _demoDownloader = demoDownloader;
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }

        /// <summary>
        /// Handle downloadUrl from GathererQueue, create new entry and send to downloader if unique, else delete and forget
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoInsertInstruction model)
        {
            _logger.LogInformation($"Received download url from DemoInsertInstruction queue \n url={model.DownloadUrl}");
            AnalyzerQuality requestedQuality = model.RequestedQuality;
            //TODO OPTIONAL FEATURE handle duplicate entry
            //Currently not inserted into db and forgotten afterwards
            //Maybe saved to special table or keep track of it otherwise
            if (_dbInterface.TryCreateNewDemoEntryFromGatherer(model, requestedQuality, out long matchId))
            {
                _logger.LogInformation($"Demo #{matchId} assigned to {model.DownloadUrl}");

                var forwardModel = new DemoDownloadInstruction
                {
                    MatchId = matchId,
                    DownloadUrl = model.DownloadUrl
                };

                _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);

                _dbInterface.SetFileStatus(matchId, FileStatus.Downloading);
                _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoDownloader, true);
                _demoDownloader.PublishMessage(forwardModel);

                _logger.LogInformation($"Sent demo #{matchId} to DemoDownloadInstruction queue");

            }
            else
            {
                _logger.LogInformation($"DownloadUrl {model.DownloadUrl} was duplicate of Demo #{matchId}");
            }

            return ConsumedMessageHandling.Done;
        }
    }
}
