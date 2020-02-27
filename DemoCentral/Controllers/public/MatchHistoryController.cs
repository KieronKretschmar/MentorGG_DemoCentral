using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;
using Microsoft.Extensions.Logging;


namespace DemoCentral.Controllers
{
    /// <summary>
    /// Handle requests for the match history with information stored in democentral. This mostly consists of demo meta-data.
    /// </summary>
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

        /// <summary>
        /// Get the failed matches for a uploader id
        /// </summary>
        /// <response code="200"> the request was processed. Keep in mind that this can still be an empty list</response>
        /// <param name="steamId">id of the uploader</param>
        /// <param name="recentMatches">the number of matches to search through</param>
        /// <param name="offset">the number of matches to skip from the beginning</param>
        /// <example> GET v1/public/single/1777771112451/failedmatches?recentMatches=10&amp;offset=2 </example>
        [HttpGet("single/{steamId}/failedmatches")]
        public MatchHistoryModel GetFailedMatchHistory(long steamId, int recentMatches, int offset)
        {
            _logger.LogInformation($"Received request for player#{steamId} to get {recentMatches} failed matches, offset {offset}");
            return MatchHistoryModel.FromRecentFailedMatches(steamId, recentMatches, offset, _dbInterface);
        }
    }
}
