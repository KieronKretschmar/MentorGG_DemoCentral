using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataBase.DatabaseClasses;
using DataBase.Enumerals;
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
        private readonly IDemoCentralDBInterface _dBInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly ILogger<ManualUploadController> _logger;

        public ManualUploadController(IDemoCentralDBInterface dBInterface, IInQueueDBInterface inQueueDBInterface, ILogger<ManualUploadController> logger)
        {
            _dBInterface = dBInterface ?? throw new ArgumentNullException(nameof(dBInterface));
            _inQueueDBInterface = inQueueDBInterface ?? throw new ArgumentNullException(nameof(inQueueDBInterface));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        [HttpGet("single/{steamId}/manual-upload")]
        public async Task<int> GetNumberOfManualUploadedMatches(long steamId)
        {
            _logger.LogInformation($"Recceived request for number of manual uploaded matches of player#{steamId}");

            var numberSuccessfulPlayerMatches = _dBInterface.GetMatchesByUploader(steamId).Where(x => x.UploadType == UploadType.ManualUserUpload && x.UploadStatus == UploadStatus.Finished).Count();

            var matchesInQueue = _inQueueDBInterface.GetPlayerMatchesInQueue(steamId).Select(x => x.MatchId);


            var numberOfManualUploadMatchesInQueue = 0;
            foreach (var matchId in matchesInQueue)
            {
                Demo demo = _dBInterface.GetDemoById(matchId);
                numberOfManualUploadMatchesInQueue += Convert.ToInt32(demo.UploadType == UploadType.ManualUserUpload);
            }

            return numberOfManualUploadMatchesInQueue + numberSuccessfulPlayerMatches;
        }
    }
}