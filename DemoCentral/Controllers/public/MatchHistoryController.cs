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
        private IDemoTableInterface _demoTableInterface;
        private readonly ILogger<MatchHistoryController> _logger;

        public MatchHistoryController(IDemoTableInterface demoTableInterface, ILogger<MatchHistoryController> logger)
        {
            _demoTableInterface = demoTableInterface;
            _logger = logger;
        }

        /// <summary>
        /// Get the failed matches for a uploader id
        /// </summary>
        /// <response code="200"> the request was processed. Keep in mind that this can still be an empty list</response>
        /// <param name="steamId">id of the uploader</param>
        /// <param name="count">the number of matches to search through</param>
        /// <param name="offset">the number of matches to skip from the beginning</param>
        /// <example> GET v1/public/single/1777771112451/failedmatches?recentMatches=10&amp;offset=2 </example>
        [HttpGet("single/{steamId}/failedmatches")]
        public MatchHistoryModel GetFailedMatchHistory(long steamId, int count, int offset)
        {
            return MatchHistoryModel.FromRecentFailedMatches(steamId, count, offset, _demoTableInterface);
        }
    }
}
