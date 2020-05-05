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
    /// Handles reports regarding uploads received from MatchWriter.
    /// </summary>
    public class MatchWriterUploadReportProcessor
    {
        private readonly ILogger<MatchWriterUploadReportProcessor> _logger;
        private readonly IDemoTableInterface _demoTableInterface;

        public MatchWriterUploadReportProcessor(
            ILogger<MatchWriterUploadReportProcessor> logger,
            IDemoTableInterface demoTableInterface)
        {
            _logger = logger;
            _demoTableInterface = demoTableInterface;
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
            _demoTableInterface.SetUploadStatus(dbDemo, model.Success);

            if (model.Success)
            {
                _demoTableInterface.SetDatabaseVersion(dbDemo, model.Version);
            }

            string log = model.Success ? "was uploaded" : "failed upload";
            _logger.LogInformation($"Demo [ {matchId} ] {log}.");
        }
    }
}