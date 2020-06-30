using System;
using System.Threading.Tasks;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Communication.MessageProcessors
{
    /// <summary>
    /// Handles reports regarding removal received from MatchWriter.
    /// </summary>
    public class MatchWriterRemovalReportProcessor
    {
        private readonly ILogger<MatchWriterRemovalReportProcessor> _logger;
        private readonly IBlobStorage _blobStorage;
        private readonly IDemoTableInterface _demoTableInterface;

        public MatchWriterRemovalReportProcessor(
            ILogger<MatchWriterRemovalReportProcessor> logger,
            IBlobStorage blobStorage,
            IDemoTableInterface demoTableInterface)
        {
            _logger = logger;
            _blobStorage = blobStorage;
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
                await UpdateDBFromResponse(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to update demo [ {model.MatchId} ] in database");
            }
        }
        private async Task UpdateDBFromResponse(TaskCompletedReport model)
        {
            var matchId = model.MatchId;
            _logger.LogInformation($"Received report for demo [ {matchId} ] storage removal - success : [ {model.Success} ] ");
            var demo = _demoTableInterface.GetDemoById(matchId);

            if (model.Success)
            {
                try
                {
                    await _blobStorage.DeleteBlobAsync(demo.BlobUrl);
                    _demoTableInterface.SetBlobUrl(demo, null);
                }
                catch  (Exception e)
                {
                    _logger.LogError(e, "Blob failed to be removed.");
                }

                _demoTableInterface.SetMatchDataRemoved(demo);


            }
            else
            {
                _logger.LogWarning($"Match [ {matchId} ] failed to be removed. Check the correctness of the remaining data.");
            }
        }
    }
}