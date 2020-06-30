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
    }

    public class MatchInfoGetter : IMatchInfoGetter
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IDemoTableInterface _dBInterface;
        private readonly ILogger<MatchInfoGetter> _logger;

        public MatchInfoGetter(
            IHttpClientFactory clientFactory,
            IDemoTableInterface dBInterface,
            ILogger<MatchInfoGetter> logger)
        {
            _clientFactory = clientFactory;;
            _dBInterface = dBInterface;
            _logger = logger;
        }

        public async Task<List<long>> GetParticipatingPlayersAsync(long matchId)
        {
            _logger.LogInformation($"Requesting participating players for match [ {matchId} ]");

            var response = await _clientFactory.CreateClient("match-retriever").GetAsync($"v1/public/match/{matchId}/players");
            response.EnsureSuccessStatusCode();

            var res = JsonConvert.DeserializeObject<PlayerInMatchModel>(await response.Content.ReadAsStringAsync());

            _logger.LogInformation($"Participating players in match [ {matchId} ] are [ {string.Join(",", res.Players)} ]");
            return res.Players;
        }

        public class PlayerInMatchModel
        {
            public List<long> Players { get; set; }
        }

    }
}
