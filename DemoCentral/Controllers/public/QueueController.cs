using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Net;
using DemoCentral.Models;

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
            try
            {
                return _dbInterface.GetQueuePosition(matchId);
            }
            catch (InvalidOperationException)
            {
                string error = $"Demo [ {matchId} ] not in queue";

                return NotFound(error);
            }
        }

        /// <summary>
        /// Get information about matches the player has in queue, e.g. the total number of enqueued matches for a certain player.
        /// </summary>
        /// <response code="200">number of matches in queue for a certain player</response>
        /// <param name="steamId">steamid of the certain player</param>
        /// <example>GET /v1/public/single/11231331131/matchesinqueue</example>
        [HttpGet("single/{steamId}/matchesinqueue")]
        public ActionResult<QueueStatusModel> QueueStatus(long steamId)
        {
            var model = new QueueStatusModel();

            // assign matchids, starting with the match inserted first
            model.MatchIds = _dbInterface.GetPlayerMatchesInQueue(steamId).OrderBy(x=>x.InsertDate).Select(x => x.MatchId).ToList();
            model.FirstDemoPosition = model.MatchIds.Count > 0 ? _dbInterface.GetQueuePosition(model.MatchIds.First()) : -1;
            model.TotalQueueLength = _dbInterface.GetTotalQueueLength();
            return model;
        }
    }
}
