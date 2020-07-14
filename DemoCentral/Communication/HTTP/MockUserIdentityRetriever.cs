using DemoCentral.Communication.HTTP;
using DemoCentral.Enumerals;
using DemoCentral.Models;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DemoCentral.Communication.HTTP
{
    internal class MockUserIdentityRetriever : IUserIdentityRetriever
    {
        private readonly ILogger<MockUserIdentityRetriever> _logger;

        public MockUserIdentityRetriever(ILogger<MockUserIdentityRetriever> logger)
        {
            _logger = logger;
        }

        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            _logger.LogWarning($"UserInfoGatherer is mocked, returning Medium Quality. Request made for SteamId [ {steamId} ]");
            return Task.FromResult(AnalyzerQuality.Medium);
        }

        public Task<SubscriptionType> GetHighestUserSubscription(List<long> playerIds)
        {
            _logger.LogWarning($"UserInfoGatherer is mocked, returning free subscription. Request made for SteamIds [ {string.Join(", ",playerIds)} ]");
            return Task.FromResult(SubscriptionType.Free);
        }

        public Task<List<UserIdentity>> GetUserIdentitiesAsync(List<long> steamIds)
        {
            throw new NotImplementedException();
        }

        public Task<UserIdentity> GetUserIdentityAsync(long player)
        {
            var mockIdentity = new UserIdentity();
            return Task.FromResult(mockIdentity);
        }
    }
}

