﻿using Database.Enumerals;
using DemoCentral.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DemoCentral.Communication.HTTP
{
    public interface IUserInfoGetter
    {
        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId);
    }

    public class UserInfoGetter : IUserInfoGetter
    {
        private string _http_USER_SUBSCRIPTION;
        private readonly ILogger<UserInfoGetter> _logger;
        private readonly HttpClient Client;


        public UserInfoGetter(string http_user_subscription, ILogger<UserInfoGetter> logger)
        {
            _http_USER_SUBSCRIPTION = http_user_subscription;
            _logger = logger;
            Client = new HttpClient();
        }

        /// <summary>
        /// Gets the analyzer quality associated with a users subscription plan
        /// </summary>
        /// <remarks>defaults to low if user could not be queried</remarks>
        /// <exception cref="HttpRequestException"></exception>
        /// <param name="steamId"></param>
        /// <returns></returns>
        public async Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            var queryString = $"{_http_USER_SUBSCRIPTION}?steamId={steamId}";
            var response = await Client.GetAsync(queryString);

            if (!response.IsSuccessStatusCode)
            {
                var msg = $"Getting user subscription plan failed for query [ {queryString} ]. Response: {response}";
                _logger.LogInformation(msg);

                return AnalyzerQuality.Low;
            }

            var userSubscriptionString = await response.Content.ReadAsStringAsync();

            var success = Enum.TryParse(userSubscriptionString, out UserSubscription subscription);

            if (!success)
                return AnalyzerQuality.Low;

            switch (subscription)
            {
                case UserSubscription.Free:
                    return AnalyzerQuality.Low;
                case UserSubscription.Premium:
                    return AnalyzerQuality.Medium;
                case UserSubscription.Ultimate:
                    return AnalyzerQuality.High;
                default:
                    return AnalyzerQuality.Low;
            }
        }
    }
}
