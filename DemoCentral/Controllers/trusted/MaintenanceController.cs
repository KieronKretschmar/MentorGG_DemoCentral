using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.DatabaseClasses;
using DemoCentral.Communication.Rabbit;
using DemoCentral.Helpers;
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
        private readonly IProducer<DemoDownloadInstruction> _demoDownloader;

        public MaintenanceController(
            IDemoTableInterface demoTableInterface,
            IInQueueTableInterface inQueueTableInterface,
            ILogger<MaintenanceController> logger,
            IProducer<DemoAnalyzeInstruction> demoFileWorker,
            IProducer<DemoDownloadInstruction> demoDownloader)
        {
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
            _logger = logger;
            _demoFileWorker = demoFileWorker;
            _demoDownloader = demoDownloader;
        }

        /// <summary>
        /// Restarts failed Demos.
        /// </summary>
        /// <param name="minUploadDate">Minimum Upload Date to consider</param>
        /// <returns></returns>
        [HttpPost("restart-unfinished-demos")]
        public ActionResult RestartFailedDemos(DateTime minUploadDate)
        {
            var failedDemos = _demoTableInterface.GetFailedDemos(minUploadDate);

            List<long> toDemoDownloader = new List<long>();
            List<long> toDemoFileWorker = new List<long>();
            foreach (var demo in failedDemos)
            {
                // If the BlobUrl is empty, Download the Demo.
                if (string.IsNullOrEmpty(demo.BlobUrl))
                {
                    AddToDemoDownloaderQueue(demo);
                    toDemoDownloader.Add(demo.MatchId);
                }
                // Otherwise, We can start the analysis process from DemoFileWorker.
                else
                {
                    AddToDemoFileWorkerQueue(demo);
                    toDemoFileWorker.Add(demo.MatchId);
                }
            }

            _logger.LogInformation($"Restarted [ {failedDemos.Count} ] Demos.");
            _logger.LogInformation($"Sent [ {toDemoDownloader.Count} ] To DemoDownloader: [ {String.Join(",", toDemoDownloader)} ]");
            _logger.LogInformation($"Sent [ {toDemoFileWorker.Count} ] To DemoFileWorker: [ {String.Join(",", toDemoFileWorker)} ]");

            return Ok();
        }


        private void AddToDemoDownloaderQueue(Demo demo)
        {
            _demoDownloader.PublishMessage(demo.ToDownloadInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.DemoDownloader);
        }

        private void AddToDemoFileWorkerQueue(Demo demo)
        {
            _demoFileWorker.PublishMessage(demo.ToAnalyzeInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.DemoFileWorker);
        }
    }
}