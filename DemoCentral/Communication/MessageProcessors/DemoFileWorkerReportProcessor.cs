using System;
using System.Threading.Tasks;
using Database.DatabaseClasses;
using Database.Enumerals;
using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Communication.MessageProcessors
{
    /// <summary>
    /// Handles reports regarding analysis received from DemoFileWorker.
    /// </summary>
    public class DemoFileWorkerReportProcessor
    {
        private readonly ILogger<DemoFileWorkerReportProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly IProducer<RedisLocalizationInstruction> _fanoutProducer;
        private IInQueueTableInterface _inQueueTableInterface;
        private readonly IBlobStorage _blobStorage;

        private const int MAX_RETRIES = 2;
        public DemoFileWorkerReportProcessor(
            ILogger<DemoFileWorkerReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IProducer<RedisLocalizationInstruction> fanoutProducer,
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
                    ActOnAnalyzeFailure(model.MatchId, model.Failure);
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
            var forwardModel = new RedisLocalizationInstruction
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
            var matchId = response.MatchId;
            InQueueDemo inQueueDemo = _inQueueTableInterface.GetDemoById(matchId);
            Demo dbDemo = _demoTableInterface.GetDemoById(matchId);

            _demoTableInterface.SetAnalyzeState(dbDemo, success: true);
            _demoTableInterface.SetFrames(dbDemo, response.FramesPerSecond);

            _inQueueTableInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.DemoFileWorker, false);

            PublishRedisInstruction(response);
            _inQueueTableInterface.RemoveDemoFromQueue(inQueueDemo);

        }

        /// <summary>
        /// Determine the cause of failure and act.
        /// </summary>
        /// <param name="matchId"></param>
        /// <param name="failure"></param>
        private void ActOnAnalyzeFailure(long matchId, DemoAnalyzeFailure failure)
        {
            InQueueDemo inQueueDemo = _inQueueTableInterface.GetDemoById(matchId);
            Demo dbDemo = _demoTableInterface.GetDemoById(matchId);

            if(dbDemo.DemoFileWorkerStatus != GenericStatus.Failure)
            {
                throw new ArgumentException(
                    $"Demo [ {matchId} ] does not have status: `Failure` Incorrect usage.");
            }

            // If what is currently stored in DemoAnalyzeFailure does not match the current failure
            // Reset the retry counter
            // If not, increment the counter.
            if (dbDemo.DemoAnalyzeFailure != failure)
            {
                _inQueueTableInterface.ResetRetry(inQueueDemo);
            }
            else
            {
                _inQueueTableInterface.IncrementRetry(inQueueDemo);
            }

            // Store the Analyze state with the current failure
            _demoTableInterface.SetAnalyzeState(dbDemo, success: false, failure);

            // If the amount of retries exceeds the maximum allowed - stop retrying this demo.
            // OR if the demo is a duplicate.
            if (inQueueDemo.RetryAttemptsOnCurrentFailure > MAX_RETRIES || )
            {
                _blobStorage.DeleteBlobAsync(dbDemo.BlobUrl);
                _inQueueTableInterface.RemoveDemoFromQueue(inQueueDemo);
                _demoTableInterface.RemoveDemo(dbDemo);
                _logger.LogInformation($"Demo [ {matchId} ]. Exceeded the maximum retry limit of [ {MAX_RETRIES} ]");
                return;
            }
            // If the demo is a duplicate.
            if (failure == DemoAnalyzeFailure.Duplicate)
            {
                _blobStorage.DeleteBlobAsync(dbDemo.BlobUrl);
                _inQueueTableInterface.RemoveDemoFromQueue(inQueueDemo);
                _demoTableInterface.RemoveDemo(dbDemo);
                _logger.LogInformation($"Demo [ {matchId} ]. Duplicate, determinted by the MD5Hash");
                return;
            }

            switch (failure){
                case DemoAnalyzeFailure.BlobDownload:
                    // BlobDownload failed.
                    // This may be a temporary issue - Try again.
                    _logger.LogWarning($"Demo [ {matchId} ]. Failed to download blob.");
                    break;

                case DemoAnalyzeFailure.Unzip:
                    // Unzip failed, this could indicate that we do not support the file type, or the demo is
                    // corrupt - Delete the blob and mark this as failed.
                    _logger.LogWarning($"Demo [ {matchId} ]. Could not be unzipped");
                    break;

                case DemoAnalyzeFailure.HttpHashCheck:
                    // Contacting DemoCentral to confirm if the Demo was a Duplicate failed.
                    // This may be a temporary issue - Try again.
                    _logger.LogWarning($"Demo [ {matchId} ]. Failed to complete the Hash check for duplicate checking.");
                    break;

                case DemoAnalyzeFailure.Duplicate:
                    // Demo has been indentified as a Duplicate.
                    throw new InvalidOperationException("Duplicate handling should have already happened!");
                case DemoAnalyzeFailure.Analyze:
                    // DemoFileWorker failed on the Analyze step.
                    _logger.LogWarning($"Demo [ {matchId} ]. Analyze Failure");
                    break;

                case DemoAnalyzeFailure.Enrich:
                    // DemoFileWorker failed on the Enrich step.
                    _logger.LogWarning($"Demo [ {matchId} ]. Enrich Failure");
                    break;

                case DemoAnalyzeFailure.RedisStorage:
                    // DemoFileWorker failed to store the MatchDataSet in Redis,
                    // This may be a temporary issue - Try again.
                    _logger.LogWarning($"Demo [ {matchId} ]. RedisStorage Failure");
                    break;

                case DemoAnalyzeFailure.Unknown:
                default:
                    _logger.LogCritical($"Demo [ {matchId} ]. Unknown Failure!");
                    break;

            }   
        }
    }
}