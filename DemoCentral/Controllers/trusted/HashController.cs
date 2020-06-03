using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;
using Database.DatabaseClasses;
using System.Net;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;

namespace DemoCentral.Controllers
{
    /// <summary>
    /// Handles duplicate checks via MD5 hash
    /// </summary>
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/trusted")]
    [ApiController]
    public class HashController : ControllerBase
    {
        private readonly IDemoTableInterface _demoTableInterface;
        private readonly ILogger<HashController> _logger;

        public HashController(IDemoTableInterface demoTableInterface, ILogger<HashController> logger)
        {
            _demoTableInterface = demoTableInterface;
            _logger = logger;
        }

        /// <summary>
        /// Check if the hash is already in the database, create if not
        /// </summary>
        /// <param name="matchId">id of the match to potentially create</param>
        /// <param name="requestedQuality">quality for which the Demo is being analysed in</param>
        /// <param name="hash">hash to check</param>
        /// <response code="200">The analysis of the demo was requested and the provided hash has been set.</response>
        /// <response code="409">the request demo is a duplicate</response>
        /// <returns>Conflict(409) or Ok(200) if re-analysis is required or not</returns>
        /// <example>POST v1/trusted/match/123456789/duplicatecheck?requestedQuality=1&amp;hash=mdHash123451a</example>
        [HttpPost("match/{matchId}/duplicatecheck")]
        public ActionResult CreateHash(long matchId,AnalyzerQuality requestedQuality, string hash)
        {
            bool analysisRequired = _demoTableInterface.IsAnalysisRequired(
                hash,
                out long? duplicateMatchId,
                requestedQuality);

            if (!analysisRequired)
            {
                string error = $"Demo [ {matchId} ] was duplicate of Demo [ {duplicateMatchId} ] via MD5Hash";
                _logger.LogInformation(error);
                return Conflict(error);
            }
            else
            {
                _logger.LogInformation($"Demo [ {matchId} ] is unique");
                
                var demo = _demoTableInterface.GetDemoById(matchId);
                _demoTableInterface.SetHash(demo, hash);

                return Ok();
            }
        }
    }
}
