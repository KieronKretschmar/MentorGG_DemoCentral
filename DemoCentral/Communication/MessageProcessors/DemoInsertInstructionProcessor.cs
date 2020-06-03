using System;
using System.Threading.Tasks;
using Database.DatabaseClasses;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using DemoCentral.Helpers;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Communication.MessageProcessors
{
    /// <summary>
    /// Handles instructions for insertion of new matches received from the Gatherers.
    /// </summary>
    public class DemoInsertInstructionProcessor
    {
        private readonly ILogger<DemoInsertInstructionProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private IInQueueTableInterface _inQueueTableInterface;

        public DemoInsertInstructionProcessor(
            ILogger<DemoInsertInstructionProcessor> logger,
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
                _logger.LogInformation($"Demo with MatchId [ {matchId} ] has DownloadUrl [ {model.DownloadUrl} ]");

                var demo = _demoTableInterface.GetDemoById(matchId);
                
                _demoDownloaderProducer.PublishMessage(demo.ToDownloadInstruction());
                
                _inQueueTableInterface.Add(matchId, Queue.DemoDownloader);

                _logger.LogInformation($"Published demo [ {matchId} ] to DemoDownloadInstruction queue");

            }
            else
            {
                _logger.LogInformation($"DownloadUrl [ {model.DownloadUrl} ] was duplicate of Demo [ {matchId} ]");
            }        
        }
    }
}