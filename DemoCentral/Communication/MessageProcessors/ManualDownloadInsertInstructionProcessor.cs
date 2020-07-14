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
    /// Handles reports regarding the receipt of manual uploads received from DemoDownloader.
    /// </summary>
    public class ManualDownloadInsertInstructionProcessor
    {
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly ILogger<DemoDownloadReportProcessor> _logger;
        private IInQueueTableInterface _inQueueTableInterface;

        public ManualDownloadInsertInstructionProcessor(
            ILogger<DemoDownloadReportProcessor> logger,
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
        public async Task WorkAsync(ManualDownloadInsertInstruction model)
        {
            try
            {
                if (model.BlobUrl is null)
                    throw new ArgumentNullException("BlobUrl can not be null");

                await InsertNewDemo(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not handle message [ {model.ToJson()} ] from ManualDownloadInsertInstruction.");
            }
        }

        private async Task InsertNewDemo(ManualDownloadInsertInstruction model)
        {
            var requestedAnalyzerQuality = await _userIdentityRetriever.GetAnalyzerQualityAsync(model.UploaderId);
            var matchId = _demoTableInterface.CreateNewDemoEntryFromManualUpload(model, requestedAnalyzerQuality);

            _inQueueTableInterface.Add(matchId, Queue.DemoFileWorker);

            var demo = _demoTableInterface.GetDemoById(matchId);
            _demoFileWorkerProducer.PublishMessage(demo.ToAnalyzeInstruction());
            _logger.LogInformation($"Sent demo [ {matchId} ] to DemoAnalyzeInstruction queue");

            var queuedDemo = _inQueueTableInterface.GetDemoById(matchId);
        }
    }
}