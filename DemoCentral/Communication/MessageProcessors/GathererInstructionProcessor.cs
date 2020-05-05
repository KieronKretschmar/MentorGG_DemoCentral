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
    public class GathererInstructionProcessor
    {
        private readonly ILogger<GathererInstructionProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private IInQueueTableInterface _inQueueTableInterface;

        public GathererInstructionProcessor(
            ILogger<GathererInstructionProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IProducer<DemoDownloadInstruction> demoDownloaderProducer,
            IUserIdentityRetriever userInfoGetter,
            IInQueueTableInterface inQueueTableInterface)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _demoDownloaderProducer = demoDownloaderProducer;
            _userIdentityRetriever = userInfoGetter;
            _inQueueTableInterface = inQueueTableInterface;
        }


        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoInsertInstruction model)
        {
            AnalyzerQuality requestedQuality = await _userIdentityRetriever.GetAnalyzerQualityAsync(model.UploaderId);

            if (_demoTableInterface.TryCreateNewDemoEntryFromGatherer(model, requestedQuality, out long matchId))
            {
                _logger.LogInformation($"Demo [ {matchId} ] assigned to [ {model.DownloadUrl} ]");

                var forwardModel = new DemoDownloadInstruction
                {
                    MatchId = matchId,
                    DownloadUrl = model.DownloadUrl
                };

                _inQueueTableInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);

                _demoTableInterface.SetFileStatus(matchId, FileStatus.Downloading);
                _inQueueTableInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoDownloader, true);
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