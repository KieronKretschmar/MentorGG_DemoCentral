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
    /// Handles reports regarding extraction received from SituationOperator.
    /// </summary>
    public class SituationExtractionReportProcessor
    {
        private readonly ILogger<SituationExtractionReportProcessor> _logger;
        private readonly IBlobStorage _blobStorage;
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IInQueueTableInterface _inQueueTableInterface;
        private readonly IMatchRedis _matchRedis;

        public SituationExtractionReportProcessor(
            ILogger<SituationExtractionReportProcessor> logger,
            IBlobStorage blobStorage,
            IDemoTableInterface demoTableInterface,
            IInQueueTableInterface inQueueTableInterface,
            IMatchRedis matchRedis
            )
        {
            _logger = logger;
            _blobStorage = blobStorage;
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
            _matchRedis = matchRedis;
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
            _demoTableInterface.SetAnalyzeState(demo, false, DemoAnalysisBlock.SituationOperator_Unknown);
        }


        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(SituationExtractionReport model)
        {
            try
            {
                await UpdateDBFromResponse(model);
            }
            catch (Exception e)
            {
                ActOnUnknownFailure(e, model.MatchId);
                throw;
            }
        }

        private async Task UpdateDBFromResponse(SituationExtractionReport model)
        {
            var matchId = model.MatchId;
            _logger.LogInformation($"Received report for demo [ {matchId} ] situation extraction - success : [ {model.Success} ] ");
            var demo = _demoTableInterface.GetDemoById(matchId);

            if (model.Success)
            {
                _demoTableInterface.SetAnalyzeState(
                    demo,
                    true);

                await _matchRedis.DeleteMatchAsync(matchId);

                _inQueueTableInterface.TryRemove(matchId);
                _logger.LogInformation($"Demo [ {matchId} ]. Analysis finished successfully.");

            }
            else
            {
                if (model.Block == null)
                {
                    throw new ArgumentException("Cannot Act on Analyze Failure if DemoAnalysisBlock is null!");
                }
                
                _demoTableInterface.SetAnalyzeState(
                    demo,
                    false,
                    model.Block);

                _inQueueTableInterface.TryRemove(matchId);
                _logger.LogError($"Demo [ {matchId} ]. Analysis failed.");
            }
        }
    }
}