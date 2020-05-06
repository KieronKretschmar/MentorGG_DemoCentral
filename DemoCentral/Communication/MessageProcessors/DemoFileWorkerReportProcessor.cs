using System;
using System.Threading.Tasks;
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
    /// Handles reports regarding analysis received from DemoFileWorker.
    /// </summary>
    public class DemoFileWorkerReportProcessor
    {
        private readonly ILogger<DemoFileWorkerReportProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly IProducer<RedisLocalizationInstruction> _fanoutProducer;
        private IInQueueTableInterface _inQueueTableInterface;

        public DemoFileWorkerReportProcessor(
            ILogger<DemoFileWorkerReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IProducer<RedisLocalizationInstruction> fanoutProducer,
            IInQueueTableInterface inQueueTableInterface)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _fanoutProducer = fanoutProducer;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _inQueueTableInterface = inQueueTableInterface;
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
                UpdateDBEntryFromFileWorkerResponse(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to update demo [ {model.MatchId} ] in database");
            }
        }



        private void UpdateDBEntryFromFileWorkerResponse(DemoAnalyzeReport response)
        {
            var matchId = response.MatchId;

            var inQueueDemo = _inQueueTableInterface.GetDemoById(matchId);
            var dbDemo = _demoTableInterface.GetDemoById(matchId);

            if (response.Success)
            {
                //Successfully handled in demo fileworker
                _demoTableInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.Finished);
                _demoTableInterface.SetFrames(dbDemo, response.FramesPerSecond);

                _inQueueTableInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.DemoFileWorker, false);

                var forwardModel = new RedisLocalizationInstruction
                {
                    MatchId = response.MatchId,
                    RedisKey = response.RedisKey,
                    ExpiryDate = response.ExpiryDate,
                };
                _fanoutProducer.PublishMessage(forwardModel);

                _inQueueTableInterface.RemoveDemoIfNotInAnyQueue(inQueueDemo);
                _logger.LogInformation($"Demo [ {matchId} ] was sent to fanout");
                return;
            }
            
            // The analysis has failed, act on the cause.
            switch (response.Failure){
                case DemoAnalyzeFailure.BlobDownload:
                    // BlobDownload failed.
                    // This may be a temporary issue - Try again.
                    _inQueueTableInterface.IncrementRetry(inQueueDemo);
                    _demoTableInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.)
                    break;

                case DemoAnalyzeFailure.Unzip:
                    // Unzip failed, this could indicate that we do not support the file type, or the demo is
                    // corrupt - Delete the blob and mark this as failed.
                    _inQueueTableInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoTableInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.UnzipFailed);
                    _logger.LogWarning($"Demo [ {matchId} ] could not be unzipped");
                    return;

                case DemoAnalyzeFailure.HttpHashCheck:
                    // Contacting DemoCentral to confirm if the Demo was a Duplicate failed.
                    // This may be a temporary issue - Try again.
                    _inQueueTableInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoTableInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.DuplicateCheckFailed);
                    _logger.LogWarning($"Demo [ {matchId} ] was not duplicate checked");
                    return;

                case DemoAnalyzeFailure.Duplicate:
                    // Demo has been indentified as a Duplicate.
                    _inQueueTableInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoTableInterface.RemoveDemo(dbDemo);

                    _logger.LogInformation($"Demo [ {matchId} ] is duplicate via MD5Hash");
                    return;

                case DemoAnalyzeFailure.Analyze:
                    // DemoFileWorker failed on the Analyze step.
                    _inQueueTableInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoTableInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.AnalyzerFailed);
                    _logger.LogWarning($"Demo [ {matchId} ] failed at DemoAnalyzer.");
                    return;

                case DemoAnalyzeFailure.Enrich:
                    // DemoFileWorker failed on the Enrich step.
                    break;

                case DemoAnalyzeFailure.RedisStorage:
                    // DemoFileWorker failed to store the MatchDataSet in Redis,
                    // This may be a temporary issue - Try again.
                    break;

                case DemoAnalyzeFailure.Unknown:
                default:
                    break;

            }            
        }
    }
}