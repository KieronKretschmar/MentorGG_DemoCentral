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
        private IDemoCentralDBInterface _dbInterface;

        public MatchHistoryController(IDemoCentralDBInterface dbInterface)
        {
            _dbInterface = dbInterface;
        }

        [HttpGet]
        public MatchHistoryModel GetMatchHistory(long playerId, int recentMatches, int offset)
        {
            return MatchHistoryModel.FromRecentMatches(playerId, recentMatches, offset, _dbInterface);
        }
    }
}