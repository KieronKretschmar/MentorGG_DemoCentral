using System;
using System.Collections.Generic;
using System.Threading;
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
    /// Handles reports regarding downloads of demo files received from DemoDownloader.
    /// </summary>
    public class DemoDownloadReportProcessor
    {
        private readonly ILogger<DemoDownloadReportProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private IInQueueTableInterface _inQueueTableInterface;

        /// <summary>
        /// Time waited before retrying after each failed attempt in seconds.
        /// </summary>
        private readonly int[] RETRY_INTERVALS = new int[] { 0, 30, 120, 300, 900 };

        public DemoDownloadReportProcessor(
            ILogger<DemoDownloadReportProcessor> logger,
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

        /// Remove the Demo from the Queue.
        /// Set the DemoAnalysisBlock to Unknown for the respective service.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="matchId"></param>
        private void ActOnUnknownFailure(Exception e, long matchId)
        {
            _logger.LogError(e, $"Failed to process Demo [ {matchId} ]. Unknown Failure. Removed from Queue.");
            Demo demo = _demoTableInterface.GetDemoById(matchId);
            InQueueDemo queueDemo = _inQueueTableInterface.GetDemoById(matchId);
            _inQueueTableInterface.Remove(queueDemo);
            _demoTableInterface.SetAnalyzeState(demo, false, DemoAnalysisBlock.DemoDownloader_Unknown);
        }

        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoDownloadReport model)
        {
            try
            {
                UpdateDemoStatusFromObtainReport(model);
            }
            catch (Exception e)
            {
                ActOnUnknownFailure(e, model.MatchId);
            }
        }

        private void UpdateDemoStatusFromObtainReport(DemoDownloadReport consumeModel)
        {
            long matchId = consumeModel.MatchId;
            var demo = _demoTableInterface.GetDemoById(matchId);

            if (consumeModel.Success)
            {
                var queuedDemo = _inQueueTableInterface.GetDemoById(matchId);

                _inQueueTableInterface.ResetRetry(queuedDemo);
                _demoTableInterface.SetBlobUrl(demo, consumeModel.BlobUrl);
                _demoFileWorkerProducer.PublishMessage(demo.ToAnalyzeInstruction());
                _inQueueTableInterface.UpdateCurrentQueue(queuedDemo, Queue.DemoFileWorker);
            }
            else
            {
                InQueueDemo queuedDemo;
                try
                {
                    queuedDemo = _inQueueTableInterface.GetDemoById(matchId);
                }
                catch (InvalidOperationException)
                {
                    _logger.LogWarning($"No InQueueDemo entry found for match [ {matchId} ] when trying to act on it. Setting it as failed.");
                    _demoTableInterface.SetAnalyzeState(demo, false, DemoAnalysisBlock.DemoDownloader_Unknown);
                    return;
                }

                int failedAttempts = _inQueueTableInterface.IncrementRetry(queuedDemo);
                if (failedAttempts >= RETRY_INTERVALS.Length)
                {
                    _inQueueTableInterface.Remove(queuedDemo);

                    _demoTableInterface.SetAnalyzeState(demo, false, DemoAnalysisBlock.DemoDownloader_Unknown);
                    _logger.LogError($"Demo [ {matchId} ] failed download more than {RETRY_INTERVALS.Length} times, no further analyzing");
                }
                else
                {
                    var delayMilliSeconds = RETRY_INTERVALS[failedAttempts - 1] * 1000;

                    _logger.LogInformation($"Waiting [ {delayMilliSeconds} ] seconds before starting retry number [ {failedAttempts} ] of downloading demo [ {matchId} ].");
                    Thread.Sleep(delayMilliSeconds);

                    _logger.LogInformation($"Sent demo [ {matchId} ] to DemoDownloadInstruction queue");

                    _demoDownloaderProducer.PublishMessage(demo.ToDownloadInstruction());

                    _logger.LogWarning($"Demo [ {matchId} ] failed download, retrying");                    
                }

            }
        }
    }
}