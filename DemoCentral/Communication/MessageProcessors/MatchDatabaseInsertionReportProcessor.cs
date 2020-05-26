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

        public MatchDatabaseInsertionReportProcessor(
            ILogger<MatchDatabaseInsertionReportProcessor> logger,
            IDemoTableInterface demoTableInterface,
            IInQueueTableInterface inQueueTableInterface)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
        }


        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(TaskCompletedReport model)
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
        private void UpdateDBFromResponse(TaskCompletedReport model)
        {
            long matchId = model.MatchId;
            var dbDemo = _demoTableInterface.GetDemoById(matchId);
            var queuedDemo = _inQueueTableInterface.GetDemoById(matchId);
            
            if (model.Success)
            {
                _demoTableInterface.SetAnalyzeState(
                    dbDemo,
                    true);

                _logger.LogInformation($"Demo [ {matchId} ]. MatchWriter stored the MatchData successfully.");
            }
            else
            {
                _demoTableInterface.SetAnalyzeState(
                    dbDemo,
                    false,
                    DemoAnalysisBlock.UnknownMatchWriter);

                _logger.LogError($"Demo [ {matchId} ]. MatchWriter failed to store the MatchData!");
            }

            _inQueueTableInterface.Remove(queuedDemo);
        }
    }
}