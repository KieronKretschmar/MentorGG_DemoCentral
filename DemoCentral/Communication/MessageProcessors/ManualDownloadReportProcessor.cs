using System;
using System.Threading.Tasks;
using Database.DatabaseClasses;
using Database.Enumerals;
using Database.Enumerals;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Communication.MessageProcessors
{
    /// <summary>
    /// Handles reports regarding the receipt of manual uploads received from DemoDownloader.
    /// </summary>
    public class ManualDownloadReportProcessor
    {
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly ILogger<DemoDownloaderReportProcessor> _logger;
        private IInQueueTableInterface _inQueueTableInterface;

        private const int MAX_RETRIES = 2;

        public ManualDownloadReportProcessor(
            ILogger<DemoDownloaderReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IUserIdentityRetriever userIdentityRetriever,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IInQueueTableInterface inQueueTableInterface)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _userIdentityRetriever = userIdentityRetriever;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _inQueueTableInterface = inQueueTableInterface;
        }


        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(ManualDownloadReport model)
        {
            try
            {
                if (model.BlobUrl is null)
                    throw new ArgumentNullException("BlobUrl can not be null");

                await InsertNewDemo(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not handle message [ {model.ToJson()} ] from ManualDownloadReport.");
            }
        }

        private async Task InsertNewDemo(ManualDownloadReport model)
        {
            var requestedAnalyzerQuality = await _userIdentityRetriever.GetAnalyzerQualityAsync(model.UploaderId);
            var matchId = _demoTableInterface.CreateNewDemoEntryFromManualUpload(model, requestedAnalyzerQuality);

            _inQueueTableInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);

            var demo = _demoTableInterface.GetDemoById(matchId);
            var analyzeInstructions = _demoTableInterface.CreateAnalyzeInstructions(demo);

            _demoFileWorkerProducer.PublishMessage(analyzeInstructions);
            _logger.LogInformation($"Sent demo [ {matchId} ] to DemoAnalyzeInstruction queue");

            var queuedDemo = _inQueueTableInterface.GetDemoById(matchId);
            _inQueueTableInterface.UpdateCurrentQueue(queuedDemo, Queue.DemoFileWorker);
        }
    }
}