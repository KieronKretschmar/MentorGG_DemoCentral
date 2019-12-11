using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;

namespace DemoCentral.Controllers.exposed
{
    [Route("api/exposed/[controller]")]
    [ApiController]
    public class MatchHistoryController : ControllerBase
    {
        [HttpGet]
        public MatchHistoryModel GetMatchHistory(long playerId, int recentMatches, int offset)
        {
            return MatchHistoryModel.FromRecentMatches(playerId, recentMatches, offset);
        }
    }
}