﻿using DataBase.DatabaseClasses;
using DemoCentral.Enumerals;
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
        Task<DateTime> CalculateDemoRemovalDateAsync(Demo demo);
        Task<DateTime> CalculateDemoRemovalDateAsync(long matchId);
    }

    public class MatchInfoGetter : IMatchInfoGetter
    {
        private readonly HttpClient _client;
        private readonly IUserIdentityRetriever _userIdentityRetriever;
        private readonly IDemoCentralDBInterface _dBInterface;
        private readonly ILogger<MatchInfoGetter> _logger;

        public MatchInfoGetter(IHttpClientFactory clientFactory, IUserIdentityRetriever userIdentityRetriever, IDemoCentralDBInterface dBInterface, ILogger<MatchInfoGetter> logger)
        {
            _client = clientFactory.CreateClient("match-retriever");
            _userIdentityRetriever = userIdentityRetriever;
            _dBInterface = dBInterface;
            _logger = logger;
        }

        public async Task<List<long>> GetParticipatingPlayersAsync(long matchId)
        {
            _logger.LogInformation($"Requesting participating players for match [ {matchId} ]");
            var players = new List<long>();

            var response = await _client.GetAsync($"match/{matchId}/players");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    $"Getting participating for match [ {matchId} ] failed. Response: [ {response} ]. Returning empty list");
                return new List<long>();
            }

            var res = JsonConvert.DeserializeObject<PlayerInMatchModel>(await response.Content.ReadAsStringAsync());
            players = res.Players;

            _logger.LogInformation($"Participating players in match [ {matchId} ] are [ {string.Join(",", players)} ]");
            return players;
        }

        public async Task<SubscriptionType> GetMaxUserSubscriptionInMatchAsync(long matchId)
        {
            var players = await GetParticipatingPlayersAsync(matchId);

            _logger.LogInformation($"Requesting highest subscription for match [ {matchId} ]");

            var maxUserSubscription = await _userIdentityRetriever.GetHighestUserSubscription(players);

            _logger.LogInformation($"Highest subscription for match [ {matchId} ] is [ {Enum.GetName(typeof(SubscriptionType), maxUserSubscription)} ]");
            return maxUserSubscription;
        }

        public async Task<DateTime> CalculateDemoRemovalDateAsync(Demo demo)
        {
            var subscription = await GetMaxUserSubscriptionInMatchAsync(demo.MatchId);
            var storageTime = StorageDurationBySubscription.Durations[subscription];
            var removalDate = demo.MatchDate + storageTime;
            return removalDate;
        }

        public async Task<DateTime> CalculateDemoRemovalDateAsync(long matchId)
        {
            //TODO Move this to a seperate class
            //Ideally this should not be here, but in a seperate class, as the match Info getter in the http directory
            //should not have database access
            var demo = _dBInterface.GetDemoById(matchId);
            return await CalculateDemoRemovalDateAsync(demo);
        }



        public class PlayerInMatchModel
        {
            public List<long> Players { get; set; }
        }

    }
}
