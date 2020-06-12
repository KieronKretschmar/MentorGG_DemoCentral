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
                    if (TryAddToDemoDownloaderQueue(demo))
                    {
                        toDemoDownloader.Add(demo.MatchId);
                    }
                }
                // Otherwise, We can start the analysis process from DemoFileWorker.
                else
                {
                    if (TryAddToDemoFileWorkerQueue(demo))
                    {
                        toDemoFileWorker.Add(demo.MatchId);
                    }
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
        [HttpPost("requeue-demos/by-matchids")]
        public ActionResult RequeueDemos([ModelBinder(typeof(CsvModelBinder))] List<long> matchIds, Queue queue)
        {
            List<long> requeuedDemos = new List<long>();
            foreach (var matchId in matchIds)
            {
                var demo = _demoTableInterface.GetDemoById(matchId);
                if (RequeueDemo(demo, queue))
                {
                    requeuedDemos.Add(demo.MatchId);
                }
            }

            var msg = $"Requeued [ {requeuedDemos.Count} ] Demos to queue [ {queue} ]: [ {String.Join(",", requeuedDemos)} ]";
            _logger.LogInformation(msg);
            return Ok(msg);
        }

        /// <summary>
        /// Restarts analysis for the specified demos starting at the specified queue.
        /// If a match is not ready to be inserted to the given queue because the previous step wasn't completed, it is skipped.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="minMatchId"></param>
        /// <param name="maxMatchId"></param>
        /// <param name="minUploadDate"></param>
        /// <param name="maxUploadDate"></param>
        /// <returns></returns>
        [HttpPost("requeue-demos/by-conditions")]
        public ActionResult RequeueDemos(Queue queue, int? minMatchId = null, int? maxMatchId = null, DateTime? minUploadDate = null, DateTime? maxUploadDate = null)
        {
            if(minMatchId == null && minUploadDate == null)
            {
                return BadRequest("As a safety measure, either minMatchId or minUploadDate must be specified. This is for safety.");
            }

            var matchIds = _demoTableInterface.GetDemos(minMatchId, maxMatchId, minUploadDate, maxUploadDate)
                .Select(x=>x.MatchId)
                .ToList();

            List<long> requeuedDemos = new List<long>();
            foreach (var matchId in matchIds)
            {
                var demo = _demoTableInterface.GetDemoById(matchId);
                if (RequeueDemo(demo, queue))
                {
                    requeuedDemos.Add(demo.MatchId);
                }
            }

            var msg = $"Requeued [ {requeuedDemos.Count} ] Demos to queue [ {queue} ]: [ {String.Join(",", requeuedDemos)} ]";
            _logger.LogInformation(msg);
            return Ok(msg);
        }

        /// <summary>
        /// Attempts to insert demo to the given queue and update database accordingly.
        /// </summary>
        /// <param name="demo"></param>
        /// <param name="queue"></param>
        /// <returns>Whether queueing was succesful.</returns>
        private bool RequeueDemo(Demo demo, Queue queue)
        {
            switch (queue)
            {
                case Queue.DemoDownloader:
                    return TryAddToDemoDownloaderQueue(demo);

                case Queue.DemoFileWorker:
                    return TryAddToDemoFileWorkerQueue(demo);

                case Queue.SituationOperator:
                    return TryAddToSituationOperatorQueue(demo);

                default:
                    throw new NotImplementedException($"Requeue is not implemented for queue [ {queue} ] is not implemented.");
            }

        }

        /// <summary>
        /// Attempts to insert demo to DemoDownloader and update database accordingly.
        /// </summary>
        /// <param name="demo"></param>
        /// <returns>Whether queueing was succesful.</returns>
        private bool TryAddToDemoDownloaderQueue(Demo demo)
        {
            _demoDownloader.PublishMessage(demo.ToDownloadInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.DemoDownloader);
            return true;
        }

        /// <summary>
        /// Attempts to insert demo to DemoFileWorker and update database accordingly.
        /// </summary>
        /// <param name="demo"></param>
        /// <returns>Whether queueing was succesful.</returns>
        private bool TryAddToDemoFileWorkerQueue(Demo demo)
        {
            // Abort if blob is not available
            if (string.IsNullOrEmpty(demo.BlobUrl))
            {
                _logger.LogInformation($"Requeuing demo [ {demo.MatchId} ] to DemoFileWorker was not possible as demo is not in BlobStorage.");
                return false;
            }

            _demoFileWorker.PublishMessage(demo.ToAnalyzeInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.DemoFileWorker);
            return true;
        }

        /// <summary>
        /// Attempts to insert demo to ituationOperator and update database accordingly.
        /// </summary>
        /// <param name="demo"></param>
        /// <returns>Whether queueing was succesful.</returns>
        private bool TryAddToSituationOperatorQueue(Demo demo)
        {
            // Abort if match does not seem to be in MatchDb
            var isInMatchDb = demo.AnalysisSucceeded || (int)demo.AnalysisBlockReason >= (int)DemoAnalysisBlock.SituationOperator_Unknown;
            if (!isInMatchDb)
            {
                _logger.LogInformation($"Requeuing demo [ {demo.MatchId} ] to SituationOperator was not possible as match is not in MatchDb.");
                return false;
            }

            _situationOperator.PublishMessage(demo.ToSituationExtractionInstruction());
            _inQueueTableInterface.Add(demo.MatchId, Queue.SituationOperator);
            return true;
        }
    }
}