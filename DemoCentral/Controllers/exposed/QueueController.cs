using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DemoCentral.Controllers.exposed
{
    [ApiController]
    [Route("api/exposed/[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly InQueueDBInterface _dbInterface;

        public QueueController(InQueueDBInterface dbInterface)
        {
            _dbInterface = dbInterface;
        }

        [HttpGet("{matchId}")]
        // GET /api/exposed/queue/playermatches/1
        public int QueuePosition(long matchId)
        {
            return _dbInterface.GetQueuePosition(matchId);
        }

        [HttpGet("[action]/{playerId}")]
        // GET /api/exposed/queue/playermatches/1
        public int NumberPlayerMatches(long playerId)
        {
            return _dbInterface.GetPlayerMatchesInQueue(playerId).Count;
        }
    }


}
