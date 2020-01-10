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
        //GET api/exposed/matchhistory?playerId=XXXX&recentMatches=YYYY&offset = 0
        public MatchHistoryModel GetMatchHistory(long uploaderId, int recentMatches, int offset)
        {
            _logger.LogInformation($"Received request for player#{uploaderId} to get {recentMatches} last matches, offset {offset}");
            return MatchHistoryModel.FromRecentMatches(uploaderId, recentMatches, offset, _dbInterface);
        }
    }
}