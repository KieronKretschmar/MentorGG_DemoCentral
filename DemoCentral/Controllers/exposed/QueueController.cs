using System;
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

        public QueueController(InQueueDBInterface dbInterface)
        {
            _dbInterface = dbInterface;
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
            try
            {
                return new ActionResult<int>(_dbInterface.GetQueuePosition(matchId));
            }
            catch (InvalidOperationException)
            {
                return new BadRequestResult();
            }
        }

        /// <summary>
        /// Get the number of enqueued matches for a certain player 
        /// </summary>
        /// <param name="playerId">steamid of the certain player</param>
        /// <returns>either int or BadRequest if the demo could not be found</returns>
        [HttpGet("[action]/{playerId}")]
        // GET /api/exposed/queue/numberplayermatches/1
        public ActionResult<int> NumberPlayerMatches(long playerId)
        {
            try
            {
                return new ActionResult<int>(_dbInterface.GetPlayerMatchesInQueue(playerId).Count);
            }
            catch (InvalidOperationException)
            {
                return new BadRequestResult();
            }
        }
    }


}
