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
    /// Handles reports regarding downloads of demo files received from DemoDownloader.
    /// </summary>
    public class DemoDownloaderReportProcessor
    {
        private readonly ILogger<DemoDownloaderReportProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private IInQueueTableInterface _inQueueTableInterface;

        private const int MAX_RETRIES = 2;

        public DemoDownloaderReportProcessor(
            ILogger<DemoDownloaderReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IProducer<DemoDownloadInstruction> demoDownloaderProducer,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IInQueueTableInterface inQueueTableInterface)
        {

            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _demoDownloaderProducer = demoDownloaderProducer;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _inQueueTableInterface = inQueueTableInterface;
        }


        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoObtainReport model)
        {
            try
            {
                UpdateDemoStatusFromObtainReport(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not update demo [ {model.MatchId} ] from DemoObtainReport.");
            }
        }

        private void UpdateDemoStatusFromObtainReport(DemoObtainReport consumeModel)
        {
            long matchId = consumeModel.MatchId;
            var inQueueDemo = _inQueueTableInterface.GetDemoById(matchId);
            var dbDemo = _demoTableInterface.GetDemoById(matchId);

            if (consumeModel.Success)
            {
                _demoTableInterface.SetBlobUrl(dbDemo, consumeModel.BlobUrl);

                _demoTableInterface.SetFileStatus(dbDemo, FileStatus.InBlobStorage);

                var model = _demoTableInterface.CreateAnalyzeInstructions(dbDemo);

                _inQueueTableInterface.UpdateCurrentQueue(inQueueDemo, Queue.DemoFileWorker);
                _demoFileWorkerProducer.PublishMessage(model);
            }
            else
            {
                int attempts = _inQueueTableInterface.IncrementRetry(inQueueDemo);

                if (attempts > MAX_RETRIES)
                {
                    _inQueueTableInterface.Remove(inQueueDemo);
                    _demoTableInterface.SetFileStatus(dbDemo, FileStatus.DownloadFailed);
                    _demoTableInterface.SetAnalyzeState(dbDemo, false, DemoAnalysisBlock.UnknownDemoDownloader);
                    _logger.LogError($"Demo [ {matchId} ] failed download more than {MAX_RETRIES} times, no further analyzing");
                }
                else
                {
                    _demoTableInterface.SetFileStatus(dbDemo, FileStatus.DownloadRetrying);

                    var resendModel = _demoTableInterface.CreateDownloadInstructions(dbDemo);

                    _demoTableInterface.SetFileStatus(dbDemo, FileStatus.DownloadRetrying);
                    _logger.LogInformation($"Sent demo [ {matchId} ] to DemoDownloadInstruction queue");

                    _demoDownloaderProducer.PublishMessage(resendModel);

                    _logger.LogWarning($"Demo [ {matchId} ] failed download, retrying");
                }
            }
        }
    }
}