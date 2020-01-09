using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;
using Microsoft.Extensions.Logging;

namespace DemoCentral.Controllers.exposed
{
    [Route("api/exposed/[controller]")]
    [ApiController]
    public class MatchHistoryController : ControllerBase
    {
        private IDemoCentralDBInterface _dbInterface;
        private readonly ILogger<MatchHistoryController> _logger;

        public MatchHistoryController(IDemoCentralDBInterface dbInterface, ILogger<MatchHistoryController> logger)
        {
            _dbInterface = dbInterface;
            _logger = logger;
        }

        [HttpGet]
        public MatchHistoryModel GetMatchHistory(long playerId, int recentMatches, int offset)
        {
            _logger.LogInformation($"Received request for player#{playerId} to get {recentMatches} last matches, offset {offset}");
            return MatchHistoryModel.FromRecentMatches(playerId, recentMatches, offset, _dbInterface);
        }
    }
}