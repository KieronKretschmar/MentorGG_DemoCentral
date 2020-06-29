using Database.DatabaseClasses;
using DemoCentral.Enumerals;
using DemoCentral.Helpers.SubscriptionConfig;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DemoCentral.Communication.HTTP
{
    public interface IMatchInfoGetter
    {
        Task<List<long>> GetParticipatingPlayersAsync(long matchId);
        Task<SubscriptionType> GetMaxUserSubscriptionInMatchAsync(long matchId);
    }

    public class MatchInfoGetter : IMatchInfoGetter
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private readonly IDemoTableInterface _dBInterface;
        private readonly ILogger<MatchInfoGetter> _logger;

        public MatchInfoGetter(
            IHttpClientFactory clientFactory,
            IUserIdentityRetriever userIdentityRetriever,
            IDemoTableInterface dBInterface,
            ILogger<MatchInfoGetter> logger)
        {
            _clientFactory = clientFactory;
            _userIdentityRetriever = userIdentityRetriever;
            _dBInterface = dBInterface;
            _logger = logger;
        }

        public async Task<List<long>> GetParticipatingPlayersAsync(long matchId)
        {
            _logger.LogInformation($"Requesting participating players for match [ {matchId} ]");

            var response = await _clientFactory.CreateClient("match-retriever").GetAsync($"v1/public/match/{matchId}/players");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    $"Getting participating for match [ {matchId} ] failed. Response: [ {response} ]. Returning empty list");
                return new List<long>();
            }

            var res = JsonConvert.DeserializeObject<PlayerInMatchModel>(await response.Content.ReadAsStringAsync());

            _logger.LogInformation($"Participating players in match [ {matchId} ] are [ {string.Join(",", res.Players)} ]");
            return res.Players;
        }

        public async Task<SubscriptionType> GetMaxUserSubscriptionInMatchAsync(long matchId)
        {
            var players = await GetParticipatingPlayersAsync(matchId);

            _logger.LogInformation($"Requesting highest subscription for match [ {matchId} ]");

            var maxUserSubscription = await _userIdentityRetriever.GetHighestUserSubscription(players);

            _logger.LogInformation($"Highest subscription for match [ {matchId} ] is [ {Enum.GetName(typeof(SubscriptionType), maxUserSubscription)} ]");
            return maxUserSubscription;
        }

        public class PlayerInMatchModel
        {
            public List<long> Players { get; set; }
        }

    }
}
