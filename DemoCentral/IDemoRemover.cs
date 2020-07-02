using Database.DatabaseClasses;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using DemoCentral.Enumerals;
using DemoCentral.Helpers.SubscriptionConfig;
using DemoCentral.Models;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DemoCentral
{
    public interface IDemoRemover
    {
        void SendRemovalInstructions(long matchId);
        Task RemoveExpiredDemos(TimeSpan allowance);
    }

    public class DemoRemover : IDemoRemover
    {
        private readonly IDemoTableInterface _demoTable;
        private readonly ILogger<DemoRemover> _logger;
        private readonly IProducer<DemoRemovalInstruction> _matchWriterProducer;
        private readonly ISubscriptionConfigProvider _subscriptionConfigProvider;
        private readonly IMatchInfoGetter _matchInfoGetter;
        private readonly IUserIdentityRetriever _userIdentityRetriever;

        public DemoRemover(
            IDemoTableInterface demoTable,
            ILogger<DemoRemover> logger,
            IProducer<DemoRemovalInstruction> matchWriterProducer,
            ISubscriptionConfigProvider subscriptionConfigProvider,
            IMatchInfoGetter matchInfoGetter,
            IUserIdentityRetriever userIdentityRetriever)
        {
            _demoTable = demoTable;
            _logger = logger;
            _matchWriterProducer = matchWriterProducer;
            _subscriptionConfigProvider = subscriptionConfigProvider;
            _matchInfoGetter = matchInfoGetter;
            _userIdentityRetriever = userIdentityRetriever;
        }

        public void SendRemovalInstructions(long matchId)
        {
            var instruction = new DemoRemovalInstruction
            {
                MatchId = matchId,
            };

            _matchWriterProducer.PublishMessage(instruction);
            _logger.LogTrace($"Forwarded request of demo [ {matchId} ] to MatchWriter for removal from database");
        }

        public async Task RemoveExpiredDemos(TimeSpan allowance)
        {
            _logger.LogInformation($"Seeking Demos to remove with TimeSpan Allowance: [ {allowance} ]");

            List<Demo> demosToRemove = _demoTable.GetDemosForRemoval(allowance);

            if(demosToRemove.Count == 0)
            {
                _logger.LogInformation("Found no Demos to remove");
                return;
            }


            foreach (var demo in demosToRemove)
            {
                _logger.LogInformation($"Evaluating Demo [ {demo.MatchId} ] ");

                List<long> steamIds = await _matchInfoGetter.GetParticipatingPlayersAsync(demo.MatchId);

                int accessPeriodDays;
                if(steamIds.Count > 0)
                {
                    accessPeriodDays = await CalculateMaximumAccessPeriodAsync(steamIds);
                }
                else
                {
                    // If there were no players, this indicates the Match was not found my MR.
                    accessPeriodDays = 0;
                }

                // Calculate Expiry Date
                DateTime expiryDate;
                if(accessPeriodDays >= 0)
                {
                    expiryDate = demo.MatchDate + TimeSpan.FromDays(accessPeriodDays);
                }
                else
                {
                    // We do this to check again in future.
                    expiryDate = DateTime.UtcNow + TimeSpan.FromDays(14);
                }

                _demoTable.SetExpiryDate(demo, expiryDate);

                // If the ExpiryDate has passed the current time, remove the Demo.
                if (expiryDate < DateTime.UtcNow)
                {   
                    _logger.LogInformation($"Sending Removal Instructions for Demo [ {demo.MatchId} ] ");
                    SendRemovalInstructions(demo.MatchId);
                }

            }
        }

        /// <summary>
        /// Iterate over each non-bot player, obtaining their Subscription information and determine the maximum MatchAccessDuration;
        /// </summary>
        /// <param name="matchId"></param>
        /// <returns></returns>
        private async Task<int> CalculateMaximumAccessPeriodAsync(List<long> steamIds)
        {
            // Get Maximum Access Period in Days
            int maximumAccessPeriodDays = 0;
            foreach (long steamId in steamIds)
            {
                // Skip Bots
                if (steamId < 0)
                {
                    continue;
                }

                var userIdentity = await _userIdentityRetriever.GetUserIdentityAsync(steamId);
                var userSettings = _subscriptionConfigProvider.Config.SettingsFromSubscriptionType(userIdentity.SubscriptionType);

                var currentAccessDuration = userSettings.MatchAccessDurationInDays;

                if (currentAccessDuration == -1)
                {
                    maximumAccessPeriodDays = currentAccessDuration;
                    break;
                }

                if(currentAccessDuration > maximumAccessPeriodDays)
                {
                    maximumAccessPeriodDays = currentAccessDuration;
                }
            }

            if(maximumAccessPeriodDays == 0)
            {
                throw new ArgumentException($"Failed to calculate MaximumAccessPeriod!");
            }
            else
            {
                return (int)maximumAccessPeriodDays;
            }
        }
    }
}