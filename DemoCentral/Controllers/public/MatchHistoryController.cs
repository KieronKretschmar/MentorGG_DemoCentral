using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;
using Microsoft.Extensions.Logging;

namespace DemoCentral.Controllers
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/public")]
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

        [HttpGet("history/{steamId}")]
        //GET v1/public/history/{steamId}?recentMatches=YYYY&offset=0
        public MatchHistoryModel GetMatchHistory(long uploaderId, int recentMatches, int offset)
        {
            _logger.LogInformation($"Received request for player#{uploaderId} to get {recentMatches} failed matches, offset {offset}");
            return MatchHistoryModel.FromRecentFailedMatches(uploaderId, recentMatches, offset, _dbInterface);
        }
    }
}
