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
    public class ManualDownloadReportProcessor
    {
        private readonly IDemoDBInterface _demoCentralDBInterface;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly ILogger<DemoDownloaderReportProcessor> _logger;
        private IInQueueDBInterface _inQueueDBInterface;

        private const int MAX_RETRIES = 2;

        public ManualDownloadReportProcessor(
            ILogger<DemoDownloaderReportProcessor> logger,
            IDemoDBInterface dbInterface,
            IUserIdentityRetriever userIdentityRetriever,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IInQueueDBInterface inQueueDBInterface)
        {
            _logger = logger;
            _demoCentralDBInterface = dbInterface;
            _userIdentityRetriever = userIdentityRetriever;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _inQueueDBInterface = inQueueDBInterface;
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
            var matchId = _demoCentralDBInterface.CreateNewDemoEntryFromManualUpload(model, requestedAnalyzerQuality);

            _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);
            var analyzeInstructions = _demoCentralDBInterface.CreateAnalyzeInstructions(matchId);

            _demoFileWorkerProducer.PublishMessage(analyzeInstructions);
            _logger.LogInformation($"Sent demo [ {matchId} ] to DemoAnalyzeInstruction queue");
            _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoFileWorker, true);
        }
    }
}