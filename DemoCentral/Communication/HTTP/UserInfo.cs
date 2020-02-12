using Database.Enumerals;
using DemoCentral.Enumerals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DemoCentral.Communication.HTTP
{
    public interface IUserInfo
    {
        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId);
    }

    public class UserInfo : IUserInfo
    {
        private string _http_USER_SUBSCRIPTION;
        private readonly HttpClient Client;


        public UserInfo(string http_user_subscription)
        {
            _http_USER_SUBSCRIPTION = http_user_subscription;
        }

        /// <summary>
        /// Gets users from SteamUserOperator.
        /// </summary>
        /// <exception cref="HttpRequestException"></exception>
        /// <param name="steamIds"></param>
        /// <returns></returns>
        public async Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            var queryString = $"{_http_USER_SUBSCRIPTION}?steamId={steamId}";
            var response = await Client.GetAsync(queryString);

            // throw exception if response is not succesful
            if (!response.IsSuccessStatusCode)
            {
                var msg = $"Getting user subscription plan failed for query [ {queryString} ]. Response: {response}";
                throw new HttpRequestException(msg);
            }
            response.EnsureSuccessStatusCode();

            var userSubscriptionString = await response.Content.ReadAsStringAsync();

            var userSubscription = Enum.Parse<UserSubscription>(userSubscriptionString);
            var quality = AnalyzerQuality.Low;

            switch (userSubscription)   
            {
                case UserSubscription.Free:
                    quality = AnalyzerQuality.Low;
                    break;
                case UserSubscription.Premium:
                    quality = AnalyzerQuality.Medium;
                    break;
                case UserSubscription.Ultimate:
                    quality = AnalyzerQuality.High;
                    break;
            }
            return quality;
        }
    }
}
