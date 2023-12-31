﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.DatabaseClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;

namespace DemoCentral.Controllers
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/public")]
    [ApiController]
    public class ManualUploadController : ControllerBase
    {
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly IInQueueTableInterface _inQueueTableInterface;
        private readonly ILogger<ManualUploadController> _logger;

        public ManualUploadController(
            IDemoTableInterface demoTableInterface,
            IInQueueTableInterface inQueueTableInterface,
            ILogger<ManualUploadController> logger)
        {
            _demoTableInterface = demoTableInterface;
            _inQueueTableInterface = inQueueTableInterface;
            _logger = logger;
        }


        [HttpGet("single/{steamId}/manual-upload")]
        public async Task<int> GetNumberOfManualUploadedMatches(long steamId)
        {
            _logger.LogInformation($"Received request for number of manual uploaded matches of player [ {steamId} ]");

            var numberSuccessfulPlayerMatches = _demoTableInterface.GetMatchesByUploader(
                steamId).Where(x => x.UploadType == UploadType.ManualUserUpload && x.AnalysisSucceeded).Count();

            var matchesInQueue = _inQueueTableInterface.GetPlayerMatchesInQueue(steamId).Select(x => x.Demo.MatchId);


            var numberOfManualUploadMatchesInQueue = 0;
            foreach (var matchId in matchesInQueue)
            {
                Demo demo = _demoTableInterface.GetDemoById(matchId);
                numberOfManualUploadMatchesInQueue += Convert.ToInt32(demo.UploadType == UploadType.ManualUserUpload);
            }

            return numberOfManualUploadMatchesInQueue + numberSuccessfulPlayerMatches;
        }
    }
}