using Database.Enumerals;
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
    public interface IUserInfoOperator
    {
        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId);
    }

    public class UserInfoOperator : IUserInfoOperator
    {
        private string _http_USER_SUBSCRIPTION;
        private readonly ILogger<UserInfoOperator> _logger;
        private readonly HttpClient Client;


        public UserInfoOperator(string http_user_subscription, ILogger<UserInfoOperator> logger)
        {
            _http_USER_SUBSCRIPTION = http_user_subscription;
            _logger = logger;
            Client = new HttpClient();
        }

        /// <summary>
        /// Gets the analyzer quality associated with a users subscription plan
        /// </summary>
        /// <exception cref="HttpRequestException"></exception>
        /// <param name="steamId"></param>
        /// <returns></returns>
        public async Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            var queryString = $"{_http_USER_SUBSCRIPTION}?steamId={steamId}";
            var response = await Client.GetAsync(queryString);

            // throw exception if response is not succesful
            if (!response.IsSuccessStatusCode)
            {
                var msg = $"Getting user subscription plan failed for query [ {queryString} ]. Response: {response}";
                _logger.LogInformation(msg);

                return AnalyzerQuality.Low;
            }

            var userSubscriptionString = await response.Content.ReadAsStringAsync();

            var userSubscription = Enum.Parse<UserSubscription>(userSubscriptionString);

            switch (userSubscription)
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
