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
        //Routing documentation is here https://docs.microsoft.com/de-de/aspnet/core/fundamentals/routing?view=aspnetcore-3.0#route-template-reference


        [HttpGet("{matchId}")]
        // GET /api/exposed/queue/playermatches/1
        public int QueuePosition(long matchId)
        {
            return QueueTracker.GetQueuePosition(matchId);
        }

        [HttpGet("[action]/{playerId}")]
        // GET /api/exposed/queue/playermatches/1
        public int PlayerMatches(long playerId)
        {
            return QueueTracker.GetPlayerMatchesInQueue(playerId).Count;
        }
    }


}
