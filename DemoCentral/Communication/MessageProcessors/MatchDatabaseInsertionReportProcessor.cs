using System;
using System.Threading.Tasks;
using Database.DatabaseClasses;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
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

        public MatchDatabaseInsertionReportProcessor(
            ILogger<MatchDatabaseInsertionReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IInQueueTableInterface inQueueTableInterface,
            IProducer<SituationExtractionInstruction> situationOperatorProducer)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
            _situationOperatorProducer = situationOperatorProducer;
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
                _logger.LogError(e, $"Failed to update demo [ {model.MatchId} ] in database");
            }
        }
        private void UpdateDBFromResponse(MatchDatabaseInsertionReport model)
        {
            long matchId = model.MatchId;
            var dbDemo = _demoTableInterface.GetDemoById(matchId);
            var queuedDemo = _inQueueTableInterface.GetDemoById(matchId);
            
            if (model.Success)
            {
                _inQueueTableInterface.UpdateCurrentQueue(queuedDemo, Queue.SitutationOperator);

                _logger.LogInformation($"Demo [ {matchId} ]. MatchWriter stored the MatchData successfully.");

                var instructions = new SituationExtractionInstruction 
                {
                    MatchId = model.MatchId,
                    // TODO: Find way to access ExpiryDate and RedisKey. Insert in database?
                    //ExpiryDate = null,
                    //RedisKey = null,
                };
                _situationOperatorProducer.PublishMessage(instructions);
                _logger.LogInformation($"Sent demo [ {model.MatchId} ] to SituationOperator queue");
            }
            else
            {
                _logger.LogError($"Demo [ {matchId} ]. MatchWriter failed with DemoAnalysisBlock [ { model.Block} ].");

                switch (model.Block)
                {
                    case DemoAnalysisBlock.MatchWriter_MatchDataSetUnavailable:
                        // TODO: Retry analysis starting at DFW or even DemoDownloader
                    case DemoAnalysisBlock.MatchWriter_RedisConnectionFailed:
                    case DemoAnalysisBlock.MatchWriter_Timeout:
                        // TODO: Retry this step (MatchDatabaseInsertion) up to 3 times, then stop analysis.

                    case DemoAnalysisBlock.MatchWriter_DatabaseUpload:
                        // TODO: Retry this step (MatchDatabaseInsertion) up to 1 times, then stop analysis.

                    case DemoAnalysisBlock.MatchWriter_Unknown:
                        // TODO: Retry this step (MatchDatabaseInsertion) up to 1 times, then stop analysis.

                    default:
                        _logger.LogWarning($"Demo [ {matchId} ]. MatchWriter failed with unhandled DemoAnalysisBlock [ { model.Block} ]!");

                        // Stop analysis
                        _demoTableInterface.SetAnalyzeState(
                            dbDemo,
                            false,
                            model.Block);
                        _demoTableInterface.RemoveDemo(dbDemo);
                        break;
                }
            }
        }
    }
}