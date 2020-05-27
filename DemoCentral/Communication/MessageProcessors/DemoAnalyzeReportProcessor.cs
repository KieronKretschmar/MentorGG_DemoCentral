using System;
using System.Threading.Tasks;
using Database.DatabaseClasses;
using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Communication.MessageProcessors
{
    /// <summary>
    /// Handles reports regarding analysis received from DemoFileWorker.
    /// </summary>
    public class DemoAnalyzeReportProcessor
    {
        private readonly ILogger<DemoAnalyzeReportProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly IProducer<MatchDatabaseInsertionInstruction> _fanoutProducer;
        private IInQueueTableInterface _inQueueTableInterface;
        private readonly IBlobStorage _blobStorage;

        private const int MAX_RETRIES = 2;
        public DemoAnalyzeReportProcessor(
            ILogger<DemoAnalyzeReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IProducer<MatchDatabaseInsertionInstruction> fanoutProducer,
            IInQueueTableInterface inQueueTableInterface,
            IBlobStorage blobStorage)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _fanoutProducer = fanoutProducer;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _inQueueTableInterface = inQueueTableInterface;
            _blobStorage = blobStorage;
        }


        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoAnalyzeReport model)
        {
            try
            {
                if (model.Success)
                {
                    ActOnAnalyzeSuccess(model);
                }
                else
                {
                    ActOnAnalyzeFailure(model.MatchId, (DemoAnalysisBlock) model.Block);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to update demo [ {model.MatchId} ] in database");
            }
        }

        /// <summary>
        /// Publish Redis Instructions, ensuring data is present.
        /// </summary>
        /// <param name="model"></param>
        private void PublishRedisInstruction(DemoAnalyzeReport model){
            if (String.IsNullOrEmpty(model.RedisKey))
            {
                throw new ArgumentException("RedisKey must be defined!");
            }
            var forwardModel = new MatchDatabaseInsertionInstruction
            {
                MatchId = model.MatchId,
                RedisKey = model.RedisKey,
                ExpiryDate = model.ExpiryDate,
            };
            _fanoutProducer.PublishMessage(forwardModel);
            _logger.LogInformation($"Demo [ {model.MatchId} ]. RedisInstruction sent.");
        }

        /// <summary>
        /// Act upon a successful analyzation of a Demo.
        /// </summary>
        /// <param name="response"></param>
        private void ActOnAnalyzeSuccess(DemoAnalyzeReport response)
        {
            Demo dbDemo = _demoTableInterface.GetDemoById(response.MatchId);
            InQueueDemo queueDemo = _inQueueTableInterface.GetDemoById(response.MatchId);

            _inQueueTableInterface.UpdateCurrentQueue(queueDemo, Queue.MatchWriter);
            PublishRedisInstruction(response);

        }

        /// <summary>
        /// Determine the cause of failure and act.
        /// </summary>
        /// <param name="matchId"></param>
        /// <param name="block"></param>
        private void ActOnAnalyzeFailure(long matchId, DemoAnalysisBlock block)
        {
            InQueueDemo inQueueDemo = _inQueueTableInterface.GetDemoById(matchId);
            Demo dbDemo = _demoTableInterface.GetDemoById(matchId);

            if(dbDemo.AnalysisSucceeded)
            {
                throw new ArgumentException(
                    $"Demo [ {matchId} ] has succeeded Analysis, Therefore ActOnFailure is the Incorrect usage.");
            }

            // If what is currently stored in DemoAnalysisBlock does not match the current failure
            // Reset the retry counter
            // If not, increment the counter.
            if (dbDemo.AnalysisBlockReason != block)
            {
                _inQueueTableInterface.ResetRetry(inQueueDemo);
            }
            else
            {
                _inQueueTableInterface.IncrementRetry(inQueueDemo);
            }

            // Store the Analyze state with the current failure
            _demoTableInterface.SetAnalyzeState(dbDemo, analysisFinishedSuccessfully: false, block);

            // If the amount of retries exceeds the maximum allowed - stop retrying this demo.
            // OR if the demo is a duplicate.
            if (inQueueDemo.RetryAttemptsOnCurrentFailure > MAX_RETRIES)
            {
                _blobStorage.DeleteBlobAsync(dbDemo.BlobUrl);
                _inQueueTableInterface.Remove(inQueueDemo);
                _demoTableInterface.RemoveDemo(dbDemo);
                _logger.LogInformation($"Demo [ {matchId} ]. Exceeded the maximum retry limit of [ {MAX_RETRIES} ].  Removed");
                return;
            }
            // If the demo is a duplicate.
            if (block == DemoAnalysisBlock.DemoFileWorker_Duplicate)
            {
                _blobStorage.DeleteBlobAsync(dbDemo.BlobUrl);
                _inQueueTableInterface.Remove(inQueueDemo);
                _demoTableInterface.RemoveDemo(dbDemo);
                _logger.LogInformation($"Demo [ {matchId} ]. Duplicate, determinted by the MD5Hash");
                return;
            }

            switch (block){
                case DemoAnalysisBlock.DemoFileWorker_BlobDownload:
                    // BlobDownload failed.
                    // This may be a temporary issue - Try again.
                    _logger.LogWarning($"Demo [ {matchId} ]. Failed to download blob.");
                    break;

                case DemoAnalysisBlock.DemoFileWorker_Unzip:
                    // Unzip failed, this could indicate that we do not support the file type, or the demo is
                    // corrupt - Delete the blob and mark this as failed.
                    _logger.LogWarning($"Demo [ {matchId} ]. Could not be unzipped");
                    break;

                case DemoAnalysisBlock.DemoFileWorker_HttpHashCheck:
                    // Contacting DemoCentral to confirm if the Demo was a Duplicate failed.
                    // This may be a temporary issue - Try again.
                    _logger.LogWarning($"Demo [ {matchId} ]. Failed to complete the Hash check for duplicate checking.");
                    break;

                case DemoAnalysisBlock.DemoFileWorker_Duplicate:
                    // Demo has been indentified as a Duplicate.
                    throw new InvalidOperationException("Duplicate handling should have already happened!");
                case DemoAnalysisBlock.DemoFileWorker_Analyze:
                    // DemoFileWorker failed on the Analyze step.
                    _logger.LogWarning($"Demo [ {matchId} ]. Analyze Failure");
                    break;

                case DemoAnalysisBlock.DemoFileWorker_Enrich:
                    // DemoFileWorker failed on the Enrich step.
                    _logger.LogWarning($"Demo [ {matchId} ]. Enrich Failure");
                    break;

                case DemoAnalysisBlock.DemoFileWorker_RedisStorage:
                    // DemoFileWorker failed to store the MatchDataSet in Redis,
                    // This may be a temporary issue - Try again.
                    _logger.LogWarning($"Demo [ {matchId} ]. RedisStorage Failure");
                    break;

                case DemoAnalysisBlock.Unknown:
                default:
                    _logger.LogCritical($"Demo [ {matchId} ]. Unknown Failure!");
                    break;

            }   
        }
    }
}