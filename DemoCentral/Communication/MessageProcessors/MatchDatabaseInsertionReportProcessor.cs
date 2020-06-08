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
    /// Handles reports regarding uploads received from MatchWriter.
    /// </summary>
    public class MatchDatabaseInsertionReportProcessor
    {
        private readonly ILogger<MatchDatabaseInsertionReportProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IInQueueTableInterface _inQueueTableInterface;
        private readonly IProducer<SituationExtractionInstruction> _situationOperatorProducer;
        private readonly IProducer<MatchDatabaseInsertionInstruction> _databaseInsertionProducer;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly IBlobStorage _blobStorage;

        public MatchDatabaseInsertionReportProcessor(
            ILogger<MatchDatabaseInsertionReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IInQueueTableInterface inQueueTableInterface,
            IProducer<SituationExtractionInstruction> situationOperatorProducer,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IProducer<MatchDatabaseInsertionInstruction> databaseInsertionProducer,
            IBlobStorage blobStorage)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
            _situationOperatorProducer = situationOperatorProducer;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _databaseInsertionProducer = databaseInsertionProducer;
            _blobStorage = blobStorage;
        }


        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(MatchDatabaseInsertionReport model)
        {
            try
            {
                UpdateDBFromResponse(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to update demo [ {model.MatchId} ] in database. Removed from Queue.");
                InQueueDemo queueDemo = _inQueueTableInterface.GetDemoById(model.MatchId);
                _inQueueTableInterface.Remove(queueDemo);
            }
        }
        private void UpdateDBFromResponse(MatchDatabaseInsertionReport model)
        {
            long matchId = model.MatchId;
            var dbDemo = _demoTableInterface.GetDemoById(matchId);
            var queuedDemo = _inQueueTableInterface.GetDemoById(matchId);

            _demoTableInterface.SetAnalyzeState(
                dbDemo,
                false,
                model.Block);

            if (model.Success)
            {
                _inQueueTableInterface.ResetRetry(queuedDemo);

                _logger.LogInformation($"Demo [ {matchId} ]. MatchWriter stored the MatchData successfully.");

                var instructions = dbDemo.ToSituationExtractionInstruction(); 
                _situationOperatorProducer.PublishMessage(instructions);
                _inQueueTableInterface.UpdateCurrentQueue(queuedDemo, Queue.SitutationOperator);
                _logger.LogInformation($"Sent demo [ {model.MatchId} ] to SituationOperator queue");
            }
            else
            {
                _logger.LogError($"Demo [ {matchId} ]. MatchWriter failed with DemoAnalysisBlock [ { model.Block} ].");

                // If what is currently stored in DemoAnalysisBlock does not match the current failure
                // Reset the retry counter
                // If not, increment the counter.
                int retryAttempts;
                if (dbDemo.AnalysisBlockReason != model.Block)
                {
                    _inQueueTableInterface.ResetRetry(queuedDemo);
                    retryAttempts = 0;
                }
                else
                {
                    retryAttempts = _inQueueTableInterface.IncrementRetry(queuedDemo);
                }

                _demoTableInterface.SetAnalyzeState(dbDemo, false, model.Block);

                int maxRetries;
                switch (model.Block)
                {
                    case DemoAnalysisBlock.MatchWriter_MatchDataSetUnavailable:
                        // Retry from DFW to make MatchDataSet available.
                        _demoFileWorkerProducer.PublishMessage(dbDemo.ToAnalyzeInstruction());
                        _inQueueTableInterface.UpdateCurrentQueue(queuedDemo, Queue.DemoFileWorker);
                        return;
                        
                    case DemoAnalysisBlock.MatchWriter_RedisConnectionFailed:
                    case DemoAnalysisBlock.MatchWriter_Timeout:
                        // Retry this step (MatchDatabaseInsertion) up to 3 times, then stop analysis.
                        maxRetries = 3;
                        if (retryAttempts >= maxRetries)
                        {
                            _blobStorage.DeleteBlobAsync(dbDemo.BlobUrl);
                            _inQueueTableInterface.Remove(queuedDemo);
                            return;
                        }
                        break;
                        
                    case DemoAnalysisBlock.MatchWriter_DatabaseUpload:
                    case DemoAnalysisBlock.MatchWriter_Unknown:
                        maxRetries = 1;
                        if (retryAttempts >= maxRetries)
                        {
                            _blobStorage.DeleteBlobAsync(dbDemo.BlobUrl);
                            _inQueueTableInterface.Remove(queuedDemo);
                            return;
                        }
                        break;

                    default:
                        _logger.LogWarning($"Demo [ {matchId} ]. MatchWriter failed with unhandled DemoAnalysisBlock [ { model.Block} ]! Removed");
                        _blobStorage.DeleteBlobAsync(dbDemo.BlobUrl);
                        _inQueueTableInterface.Remove(queuedDemo);
                        break;
                }

                _logger.LogInformation($"Re-sending Match [ {dbDemo.MatchId} ] to MatchWriter for a retry.");
                _databaseInsertionProducer.PublishMessage(new MatchDatabaseInsertionInstruction {MatchId = dbDemo.MatchId});
            }
        }
    }
}