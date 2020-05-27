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
                var blockReason = DemoAnalysisBlock.MatchWriter_Unknown;
                _demoTableInterface.SetAnalyzeState(
                    dbDemo,
                    false,
                    blockReason);
                _demoTableInterface.RemoveDemo(dbDemo);

                _logger.LogError($"Demo [ {matchId} ]. MatchWriter failed to store the MatchData!");
            }
        }
    }
}