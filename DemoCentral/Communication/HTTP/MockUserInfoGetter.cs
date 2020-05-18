﻿using DemoCentral.Communication.HTTP;
using DemoCentral.Models;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using System;
using System.Threading.Tasks;

namespace DemoCentral.Communication.HTTP
{
    internal class MockUserInfoGetter : IUserIdentityRetriever
    {
        private readonly ILogger<MockUserInfoGetter> _logger;

        public MockUserInfoGetter(ILogger<MockUserInfoGetter> logger)
        {
            _logger = logger;
        }

        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            _logger.LogWarning($"UserInfoGatherer is mocked, returning Medium Quality. Request made for SteamId [ {steamId} ]");
            return Task.FromResult(AnalyzerQuality.Medium);
        }

        public Task<UserIdentity> GetUserIdentityAsync(long player)
        {
            var mockIdentity = new UserIdentity();
            return Task.FromResult(mockIdentity);
        }
    }
}

