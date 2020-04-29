using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoCentral.Communication.Rabbit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DemoCentral.Controllers.trusted
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/trusted/maintenance")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly IDemoDBInterface _dbInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly ILogger<MaintenanceController> _logger;
        private readonly IDemoFileWorker _demoFileWorker;

        public MaintenanceController(IDemoDBInterface dbInterface, IInQueueDBInterface inQueueDBInterface, ILogger<MaintenanceController> logger, IDemoFileWorker demoFileWorker)
        {
            _dbInterface = dbInterface;
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
            _demoFileWorker = demoFileWorker;
        }

        /// <summary>
        /// Resets and triggers re-analysis for all unfinished demos that are in blobStorage and were uploaded after minUploadDate.
        /// </summary>
        /// <param name="minUploadDate"></param>
        /// <returns></returns>
        [HttpPost("restart-unfinished-demos")]
        public ActionResult RestartUnfinishedDemos(DateTime minUploadDate)
        {
            var demosToReset = _dbInterface.GetUnfinishedDemos(minUploadDate);

            int resetCount = 0;
            foreach (var demo in demosToReset)
            {
                // Update Demo table
                var isReset = _dbInterface.ResetAnalysis(demo.MatchId);
                if (!isReset)
                {
                    _logger.LogError($"Could not reset analysis for demo [ {demo} ]");
                    continue;
                }

                // Update InQueueDemo table
                // Try to remove demo from queue if it's in it
                try
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(demo.MatchId);
                }
                catch
                {

                }
                var inQueueDemo = _inQueueDBInterface.Add(demo.MatchId, demo.MatchDate, demo.Source, demo.UploaderId);
                _inQueueDBInterface.UpdateProcessStatus(inQueueDemo, Database.Enumerals.ProcessedBy.DemoFileWorker, true);

                // Publish message
                var message = _dbInterface.CreateAnalyzeInstructions(demo);
                _demoFileWorker.PublishMessage(message);

                _logger.LogInformation($"Reset analysis for [ {demo.MatchId} ]");

                resetCount++;
            }

            var msg = $"Triggered reanalysis for [ {resetCount} ] of [ {demosToReset.Count} ] selected matches uploaded after [ {minUploadDate} ].";
            _logger.LogInformation(msg);
            return Content(msg);
        }
    }
}