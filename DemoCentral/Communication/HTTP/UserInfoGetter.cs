
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitCommunicationLib.Enums;
using System.Net.Http;
using System.Threading.Tasks;
using DemoCentral.Models;
using DemoCentral.Enumerals;
using System.Collections.Generic;
using System;

namespace DemoCentral.Communication.HTTP
{
    public interface IUserIdentityRetriever
    {
        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId);
        public Task<UserIdentity> GetUserIdentityAsync(long player);
        public Task<SubscriptionType> GetHighestUserSubscription(List<long> playerIds);
    }

    /// <summary>
    /// Responsible for retreiving UserIdentity Information.
    /// </summary>
    public class UserIdentityRetriever : IUserIdentityRetriever
    {
        private readonly ILogger<UserIdentityRetriever> _logger;
        private readonly IHttpClientFactory _clientFactory;


        public UserIdentityRetriever(
            IHttpClientFactory clientFactory,
            ILogger<UserIdentityRetriever> logger)
        {
            _logger = logger;
            _clientFactory = clientFactory;
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
            var userIdentity = await GetUserIdentityAsync(steamId);

            switch (userIdentity.SubscriptionType)
            {
                case SubscriptionType.Free:
                    return AnalyzerQuality.Low;
                case SubscriptionType.Premium:
                    return AnalyzerQuality.Medium;
                case SubscriptionType.Ultimate:
                    return AnalyzerQuality.High;
                default:
                    _logger.LogWarning($"Defaulting to AnalyzerQuality.Low for unknown UserSubsription of user [ {steamId} ]");
                    return AnalyzerQuality.Low;
            }
        }


        public async Task<UserIdentity> GetUserIdentityAsync(long steamId)
        {
            var response = await _clientFactory.CreateClient("mentor-interface").GetAsync($"/identity/{steamId}");
            response.EnsureSuccessStatusCode();

            var reponseContent = await response.Content.ReadAsStringAsync();
            var userIdentity = JsonConvert.DeserializeObject<UserIdentity>(reponseContent);

            return userIdentity;
        }

        public async Task<SubscriptionType> GetHighestUserSubscription(List<long> steamIds)
        {
            var maxSubscription = SubscriptionType.Free;
            _logger.LogInformation($"Requesting highest subscription from players [ {string.Join(",", steamIds)} ]");

            foreach (var steamId in steamIds)
            {
                // Skip Bots
                if(steamId < 0)
                {
                    continue;
                }
                var identity = await GetUserIdentityAsync(steamId);
                maxSubscription = identity.SubscriptionType > maxSubscription ? identity.SubscriptionType : maxSubscription;
            }

            _logger.LogInformation($"Highest subscription from players [ {string.Join(",", steamIds)} ] is [ {Enum.GetName(typeof(SubscriptionType), maxSubscription)} ]");

            return maxSubscription;
        }
    }

}
