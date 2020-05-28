using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.Helpers
{
    public interface IMatchRedis
    {
        Task DeleteMatchAsync(long matchId);
    }

    /// <summary>
    /// Communicates with the redis cache that stores MatchDataSets
    /// </summary>
    public class MatchRedis : IMatchRedis
    {
        private readonly ILogger<MatchRedis> _logger;
        private IDatabase cache;

        public MatchRedis(
            ILogger<MatchRedis> logger,
            IConnectionMultiplexer connectionMultiplexer)
        {
            _logger = logger;
            cache = connectionMultiplexer.GetDatabase();
        }

        public async Task DeleteMatchAsync(long matchId)
        {
            var key = matchId.ToString();
            _logger.LogDebug($"Attempting to delete key [ {key} ]");
            await cache.KeyDeleteAsync(key).ConfigureAwait(false);
            _logger.LogDebug($"Deleted key [ {key} ] from RedisCache");
        }
    }

    /// <summary>
    /// Mock implementation of <see cref="IMatchRedis"></see> that does nothing.
    /// </summary>
    public class MockRedis : IMatchRedis
    {
        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task DeleteMatchAsync(long matchId)
        {
            return Task.CompletedTask;
        }
    }
}
