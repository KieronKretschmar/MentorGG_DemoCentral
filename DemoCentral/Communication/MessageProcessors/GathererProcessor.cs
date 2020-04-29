using System;
using System.Threading.Tasks;
using Database.Enumerals;
using DataBase.Enumerals;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Communication.MessageProcessors
{
    public class GathererProcessor
    {
        private readonly IDemoDBInterface _dbInterface;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private readonly ILogger<GathererProcessor> _logger;
        private IInQueueDBInterface _inQueueDBInterface;

        public GathererProcessor(
            IDemoDBInterface dbInterface,
            IProducer<DemoDownloadInstruction> demoDownloaderProducer,
            IUserIdentityRetriever userInfoGetter,
            ILogger<GathererProcessor> logger,
            IInQueueDBInterface inQueueDBInterface)
        {

            _dbInterface = dbInterface;
            _demoDownloaderProducer = demoDownloaderProducer;
            _userIdentityRetriever = userInfoGetter;
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }


        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoInsertInstruction model)
        {
            AnalyzerQuality requestedQuality = await _userIdentityRetriever.GetAnalyzerQualityAsync(model.UploaderId);

            //TODO OPTIONAL FEATURE handle duplicate entry
            //Currently not inserted into db and forgotten afterwards
            //Maybe saved to special table or keep track of it otherwise
            if (_dbInterface.TryCreateNewDemoEntryFromGatherer(model, requestedQuality, out long matchId))
            {
                _logger.LogInformation($"Demo [ {matchId} ] assigned to [ {model.DownloadUrl} ]");

                var forwardModel = new DemoDownloadInstruction
                {
                    MatchId = matchId,
                    DownloadUrl = model.DownloadUrl
                };

                _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);

                _dbInterface.SetFileStatus(matchId, FileStatus.Downloading);
                _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoDownloader, true);
                _demoDownloaderProducer.PublishMessage(forwardModel);

                _logger.LogInformation($"Published demo [ {matchId} ] to DemoDownloadInstruction queue");

            }
            else
            {
                _logger.LogInformation($"DownloadUrl [ {model.DownloadUrl} ] was duplicate of Demo [ {matchId} ]");
            }        
        }
    }
}