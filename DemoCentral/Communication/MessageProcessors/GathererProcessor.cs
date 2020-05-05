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
        private readonly ILogger<GathererProcessor> _logger;
        private readonly IDemoDBInterface _dbInterface;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private IInQueueDBInterface _inQueueDBInterface;

        public GathererProcessor(
            ILogger<GathererProcessor> logger,
            IDemoDBInterface dbInterface,
            IProducer<DemoDownloadInstruction> demoDownloaderProducer,
            IUserIdentityRetriever userInfoGetter,
            IInQueueDBInterface inQueueDBInterface)
        {
            _logger = logger;
            _dbInterface = dbInterface;
            _demoDownloaderProducer = demoDownloaderProducer;
            _userIdentityRetriever = userInfoGetter;
            _inQueueDBInterface = inQueueDBInterface;
        }


        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoInsertInstruction model)
        {
            AnalyzerQuality requestedQuality = await _userIdentityRetriever.GetAnalyzerQualityAsync(model.UploaderId);

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