
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitCommunicationLib.Enums;
using System.Net.Http;
using System.Threading.Tasks;
using DemoCentral.Models;
using DemoCentral.Enumerals;
using System.Collections.Generic;
using System;
using System.Net;
using System.Linq;

namespace DemoCentral.Communication.HTTP
{
    public interface IUserIdentityRetriever
    {
        Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId);
        Task<UserIdentity> GetUserIdentityAsync(long steamId);

        Task<List<UserIdentity>> GetUserIdentitiesAsync(List<long> steamIds);

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

            if(userIdentity == null)
            {
                return AnalyzerQuality.Low;
            }

            switch (userIdentity.SubscriptionType)
            {
                case SubscriptionType.Free:
                case SubscriptionType.Influencer:
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
            
            string content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.NotFound && content.Contains($"User [ {steamId} ] not found"))
            {
                _logger.LogInformation($"User [ {steamId} ] not found in MentorInterface.");
                return null;
            }
            else
            {
                response.EnsureSuccessStatusCode();
            }

            var reponseContent = await response.Content.ReadAsStringAsync();
            var userIdentity = JsonConvert.DeserializeObject<UserIdentity>(reponseContent);

            return userIdentity;
        }

        public async Task<List<UserIdentity>> GetUserIdentitiesAsync(List<long> steamIds)
        {
            var idsString = string.Join(",", steamIds);
            var response = await _clientFactory.CreateClient("mentor-interface").GetAsync($"/identity/multiple/{idsString}");

            string content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.NotFound && content.Contains($"Users not found"))
            {
                _logger.LogInformation($"No Users found in MentorInterface.");
                return new List<UserIdentity>();
            }
            else
            {
                response.EnsureSuccessStatusCode();
            }

            var reponseContent = await response.Content.ReadAsStringAsync();
            var userIdentities = JsonConvert.DeserializeObject<List<UserIdentity>>(reponseContent);

            return userIdentities;

        }
    }

}
