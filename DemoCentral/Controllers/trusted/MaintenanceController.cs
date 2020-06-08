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
using RabbitCommunicationLib.Enums;
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
        private readonly IProducer<SituationExtractionInstruction> _situationOperator;

        public MaintenanceController(
            IDemoTableInterface demoTableInterface,
            IInQueueTableInterface inQueueTableInterface,
            ILogger<MaintenanceController> logger,
            IProducer<DemoAnalyzeInstruction> demoFileWorker,
            IProducer<DemoDownloadInstruction> demoDownloader,
            IProducer<SituationExtractionInstruction> situationOperator
            )
        {
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
            _logger = logger;
            _demoFileWorker = demoFileWorker;
            _demoDownloader = demoDownloader;
            _situationOperator = situationOperator;
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
                    TryAddToDemoDownloaderQueue(demo);
                    toDemoDownloader.Add(demo.MatchId);
                }
                // Otherwise, We can start the analysis process from DemoFileWorker.
                else
                {
                    TryAddToDemoFileWorkerQueue(demo);
                    toDemoFileWorker.Add(demo.MatchId);
                }
            }

            _logger.LogInformation($"Restarted [ {failedDemos.Count} ] Demos.");
            _logger.LogInformation($"Sent [ {toDemoDownloader.Count} ] To DemoDownloader: [ {String.Join(",", toDemoDownloader)} ]");
            _logger.LogInformation($"Sent [ {toDemoFileWorker.Count} ] To DemoFileWorker: [ {String.Join(",", toDemoFileWorker)} ]");

            return Ok();
        }

        /// <summary>
        /// Restarts analysis for the specified demos starting at the specified queue.
        /// If a match is not ready to be inserted to the given queue because the previous step wasn't completed, it is skipped.
        /// </summary>
        /// <param name="matchIds"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        [HttpPost("requeue-demos")]
        public ActionResult RequeueDemos([ModelBinder(typeof(CsvModelBinder))] List<long> matchIds, Queue queue)
        {
            List<long> requeuedDemos = new List<long>();
            foreach (var matchId in matchIds)
            {
                var demo = _demoTableInterface.GetDemoById(matchId);

                switch (queue)
                {
                    case Queue.DemoDownloader:
                        if (TryAddToDemoDownloaderQueue(demo))
                        {
                            requeuedDemos.Add(demo.MatchId);
                        }
                        break;

                    case Queue.DemoFileWorker:
                        if (TryAddToDemoFileWorkerQueue(demo))
                        {
                            requeuedDemos.Add(demo.MatchId);
                        }
                        break;

                    case Queue.MatchWriter:
                        return BadRequest("Can not requeue demo at MatchWriter, as it depends on redis insertion from DemoFileWorker. Please choose DemoFileWorker instead.");

                    case Queue.SitutationOperator:
                        if (TryAddToSituationOperatorQueue(demo))
                        {
                            requeuedDemos.Add(demo.MatchId);
                        }
                        break;

                    default:
                        throw new NotImplementedException($"Requeue is not implemented for queue [ {queue} ] is not implemented.");
                }
            }

            var msg = $"Requeued [ {requeuedDemos.Count} ] Demos to queue [ {queue} ]: [ {String.Join(",", requeuedDemos)} ]";
            _logger.LogInformation(msg);
            return Ok(msg);
        }

        private bool TryAddToDemoDownloaderQueue(Demo demo)
        {
            _demoDownloader.PublishMessage(demo.ToDownloadInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.DemoDownloader);
            return true;
        }

        private bool TryAddToDemoFileWorkerQueue(Demo demo)
        {
            // Abort if blob is not available
            if (string.IsNullOrEmpty(demo.BlobUrl))
            {
                return false;
            }

            _demoFileWorker.PublishMessage(demo.ToAnalyzeInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.DemoFileWorker);
            return true;
        }

        private bool TryAddToSituationOperatorQueue(Demo demo)
        {
            // Abort if match does not seem to be in MatchDb
            var isInMatchDb = demo.AnalysisSucceeded || (int)demo.AnalysisBlockReason >= (int)DemoAnalysisBlock.SituationOperator_Unknown;
            if (!isInMatchDb)
            {
                return false;
            }


            _situationOperator.PublishMessage(demo.ToSituationExtractionInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.SitutationOperator);
            return true;
        }
    }
}