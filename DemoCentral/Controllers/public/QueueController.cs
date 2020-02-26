using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Net;

namespace DemoCentral.Controllers
{
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
        /// <returns>either int or BadRequest if the demo could not be found</returns>
        [HttpGet("match/{matchId}/queueposition")]
        //GET /v1/public/match/1234551112/queueposition
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

                _logger.LogWarning(error);
                return NotFound(error);
            }
        }

        /// <summary>
        /// Get the number of enqueued matches for a certain player 
        /// </summary>
        /// <param name="playerId">steamid of the certain player</param>
        [HttpGet("single/{playerId}/matchesinqueue")]
        // GET /v1/public/single/11231331131/matchesinqueue
        public ActionResult<int> NumberPlayerMatches(long playerId)
        {
            _logger.LogInformation($"Received request for matches of player#{playerId}");
            return _dbInterface.GetPlayerMatchesInQueue(playerId).Count;
        }
    }


}
