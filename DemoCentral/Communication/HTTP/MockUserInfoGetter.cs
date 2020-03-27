using DemoCentral.Communication.HTTP;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using System.Threading.Tasks;

namespace DemoCentral.Communication.HTTP
{
    internal class MockUserInfoGetter : IUserInfoGetter
    {
        private readonly ILogger<MockUserInfoGetter> _logger;

        public MockUserInfoGetter(ILogger<MockUserInfoGetter> logger)
        {
            _logger = logger;
        }

        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            _logger.LogWarning($"You are using the mock for UserInfoGatherer. Request made for SteamId [ {steamId} ]");
            return Task.FromResult(AnalyzerQuality.Medium);
        }
    }
}

