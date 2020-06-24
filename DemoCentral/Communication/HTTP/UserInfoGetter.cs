
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
        private readonly HttpClient Client;


        public UserIdentityRetriever(IHttpClientFactory httpClientFactory, ILogger<UserIdentityRetriever> logger)
        {
            _logger = logger;
            Client = httpClientFactory.CreateClient("mentor-interface");
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
            var response = await Client.GetAsync($"/identity/{steamId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    $"Getting UserIdentity for SteamId [ {steamId} ]. Response: [ {response} ]. Returning AnalyzerQuality.Low");
            }

            var reponseContent = await response.Content.ReadAsStringAsync();
            var userIdentity = JsonConvert.DeserializeObject<UserIdentity>(reponseContent);

            return userIdentity;
        }

        public async Task<SubscriptionType> GetHighestUserSubscription(List<long> playerIds)
        {
            var maxSubscription = SubscriptionType.Free;
            _logger.LogInformation($"Requesting highest subscription from players [ {string.Join(",", playerIds)} ]");

            foreach (var player in playerIds)
            {
                var identity = await GetUserIdentityAsync(player);
                maxSubscription = identity.SubscriptionType > maxSubscription ? identity.SubscriptionType : maxSubscription;
            }

            _logger.LogInformation($"Highest subscription from players [ {string.Join(",", playerIds)} ] is [ {Enum.GetName(typeof(SubscriptionType), maxSubscription)} ]");

            return maxSubscription;
        }
    }

}
