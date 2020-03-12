﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Net;

namespace DemoCentral.Controllers
{
    /// <summary>
    /// Handles requests for the queue status. All of this requests are GET-only.
    /// </summary>
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/public")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly IInQueueDBInterface _dbInterface;
        private readonly ILogger<QueueController> _logger;

        public QueueController(IInQueueDBInterface dbInterface, ILogger<QueueController> logger)
        {
            _dbInterface = dbInterface;
            _logger = logger;
        }

        /// <summary>
        /// Get the position in queue for a certain demo 
        /// </summary>
        /// <param name="matchId">id of the certain demo</param>
        /// <response code="200">the position the demo is at</response> 
        /// <response code="404">the demo was not in queue</response> 
        /// <returns>either int or 404 if the demo could not be found</returns>
        /// <example>GET /v1/public/match/1234551112/queueposition</example>
        [HttpGet("match/{matchId}/queueposition")]
        public ActionResult<int> QueuePosition(long matchId)
        {
            _logger.LogInformation($"Received request for queue position of Demo#{matchId}");
            try
            {
                return _dbInterface.GetQueuePosition(matchId);
            }
            catch (InvalidOperationException)
            {
                string error = $"Demo#{matchId} not in queue";

                _logger.LogInformation(error);
                return NotFound(error);
            }
        }

        /// <summary>
        /// Get the number of enqueued matches for a certain player 
        /// </summary>
        /// <response code="200">number of matches in queue for a certain player</response>
        /// <param name="steamId">steamid of the certain player</param>
        /// <example>GET /v1/public/single/11231331131/matchesinqueue</example>
        [HttpGet("single/{steamId}/matchesinqueue")]
        public ActionResult<int> NumberPlayerMatches(long steamId)
        {
            _logger.LogInformation($"Received request for matches of player#{steamId}");
            return _dbInterface.GetPlayerMatchesInQueue(steamId).Count;
        }
    }


}