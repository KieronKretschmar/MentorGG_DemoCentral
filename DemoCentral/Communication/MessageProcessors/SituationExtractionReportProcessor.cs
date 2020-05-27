using System;
using System.Threading.Tasks;
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
                _logger.LogError(e, $"Failed to update demo [ {model.MatchId} ] in database");
            }
        }

        private async Task UpdateDBFromResponse(SituationExtractionReport model)
        {
            var matchId = model.MatchId;
            _logger.LogInformation($"Received report for demo [ {matchId} ] situation extraction - success : [ {model.Success} ] ");
            var demo = _demoTableInterface.GetDemoById(matchId);
            var queuedDemo = _inQueueTableInterface.GetDemoById(matchId);

            if (model.Success)
            {
                _demoTableInterface.SetAnalyzeState(
                    demo,
                    true,
                    null);

                await _matchRedis.DeleteMatch(matchId);

                _logger.LogInformation($"Demo [ {matchId} ]. Analysis finished successfully.");
            }
            else
            {
                _demoTableInterface.SetAnalyzeState(
                    demo,
                    false,
                    model.Block);

                _logger.LogError($"Demo [ {matchId} ]. Analysis failed.");
            }

            _inQueueTableInterface.Remove(queuedDemo);
        }
    }
}