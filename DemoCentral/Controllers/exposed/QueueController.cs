﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Net;

namespace DemoCentral.Controllers.exposed
{
    [ApiController]
    [Route("api/exposed/[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly InQueueDBInterface _dbInterface;
        private readonly ILogger<QueueController> _logger;

        public QueueController(InQueueDBInterface dbInterface, ILogger<QueueController> logger)
        {
            _dbInterface = dbInterface;
            _logger = logger;
        }

        /// <summary>
        /// Get the position in queue for a certain demo 
        /// </summary>
        /// <param name="matchId">id of the certain demo</param>
        /// <returns>either int or BadRequest if the demo could not be found</returns>
        [HttpGet("{matchId}")]
        // GET /api/exposed/queue/playermatches/1
        public ActionResult<int> QueuePosition(long matchId)
        {
            _logger.LogInformation("Received request for queue position of Demo#{matchId}");
            try
            {
                return new ActionResult<int>(_dbInterface.GetQueuePosition(matchId));
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Demo#{matchId} not in queue");
                return new BadRequestResult();
            }
        }

        /// <summary>
        /// Get the number of enqueued matches for a certain player 
        /// </summary>
        /// <param name="playerId">steamid of the certain player</param>
        [HttpGet("[action]/{playerId}")]
        // GET /api/exposed/queue/numberplayermatches/1
        public ActionResult<int> NumberPlayerMatches(long playerId)
        {
            _logger.LogInformation("Received request for matches of player#{playerId}");
            try
            {
                return new ActionResult<int>(_dbInterface.GetPlayerMatchesInQueue(playerId).Count);
            }
            catch
            {
                _logger.LogError("Invalid player#{playerId}");
                return new BadRequestResult();
            }
        }
    }


}
