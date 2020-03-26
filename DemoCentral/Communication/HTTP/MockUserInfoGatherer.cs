using DemoCentral.Communication.HTTP;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using System.Threading.Tasks;

namespace DemoCentral
{
    internal class MockUserInfoGatherer : IUserInfoOperator
    {
        private readonly ILogger<MockUserInfoGatherer> _logger;

        public MockUserInfoGatherer(ILogger<MockUserInfoGatherer> logger)
        {
            _logger = logger;
        }

        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            _logger.LogWarning($"You are using the mock for UserInfoGatherer. Request made for user #{steamId}.");
            return Task.FromResult(AnalyzerQuality.Medium);
        }
    }
}