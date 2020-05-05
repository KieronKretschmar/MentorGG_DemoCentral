using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoCentral.Communication.Rabbit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Controllers.trusted
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/trusted/maintenance")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IInQueueTableInterface _inQueueTableInterface;
        private readonly ILogger<MaintenanceController> _logger;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorker;

        public MaintenanceController(IDemoTableInterface demoTableInterface, IInQueueTableInterface inQueueTableInterface, ILogger<MaintenanceController> logger, IProducer<DemoAnalyzeInstruction> demoFileWorker)
        {
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
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
            var demosToReset = _demoTableInterface.GetUnfinishedDemos(minUploadDate);

            int resetCount = 0;
            foreach (var demo in demosToReset)
            {
                // Update Demo table
                var isReset = _demoTableInterface.ResetAnalysis(demo.MatchId);
                if (!isReset)
                {
                    _logger.LogError($"Could not reset analysis for demo [ {demo} ]");
                    continue;
                }

                // Update InQueueDemo table
                // Try to remove demo from queue if it's in it
                try
                {
                    _inQueueTableInterface.RemoveDemoFromQueue(demo.MatchId);
                }
                catch
                {

                }
                var inQueueDemo = _inQueueTableInterface.Add(demo.MatchId, demo.MatchDate, demo.Source, demo.UploaderId);
                _inQueueTableInterface.UpdateProcessStatus(inQueueDemo, Database.Enumerals.ProcessedBy.DemoFileWorker, true);

                // Publish message
                var message = _demoTableInterface.CreateAnalyzeInstructions(demo);
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