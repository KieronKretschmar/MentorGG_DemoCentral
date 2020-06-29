using Database.DatabaseClasses;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using DemoCentral.Enumerals;
using DemoCentral.Helpers.SubscriptionConfig;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DemoCentral
{
    public interface IDemoRemover
    {
        DemoRemover.DemoRemovalResult RemoveDemo(long matchId);
        Task RemoveExpiredDemos(TimeSpan allowance);
    }

    public class DemoRemover : IDemoRemover
    {
        private readonly IDemoTableInterface _demoTable;
        private readonly ILogger<DemoRemover> _logger;
        private readonly IProducer<DemoRemovalInstruction> _matchWriterProducer;
        private readonly ISubscriptionConfigProvider _subscriptionConfigProvider;
        private readonly IMatchInfoGetter _matchInfoGetter;

        public DemoRemover(
            IDemoTableInterface demoTable,
            ILogger<DemoRemover> logger,
            IProducer<DemoRemovalInstruction> matchWriterProducer,
            ISubscriptionConfigProvider subscriptionConfigProvider,
            IMatchInfoGetter matchInfoGetter)
        {
            _demoTable = demoTable;
            _logger = logger;
            _matchWriterProducer = matchWriterProducer;
            _subscriptionConfigProvider = subscriptionConfigProvider;
            _matchInfoGetter = matchInfoGetter;
        }

        public DemoRemovalResult RemoveDemo(long matchId)
        {
            Demo demo;
            try
            {
                demo = _demoTable.GetDemoById(matchId);
            }
            catch (Exception e) when (e is ArgumentException)
            {
                _logger.LogInformation(e, $"Demo [ {matchId} ] was not removed from blob storage");
                return DemoRemovalResult.NotInStorage;
            }
            catch (Exception e) when (e is InvalidOperationException)
            {
                _logger.LogInformation(e, $"Demo [ {matchId} ] does not exist, Removal request cancelled");
                return DemoRemovalResult.NotFound;
            }
            
            return RemoveDemo(demo);
        }

        public DemoRemovalResult RemoveDemo(Demo demo)
        {
            var instruction = new DemoRemovalInstruction
            {
                MatchId = demo.MatchId,
            };

            _matchWriterProducer.PublishMessage(instruction);
            _logger.LogTrace($"Forwarded request of demo [ {demo.MatchId} ] to MatchWriter for removal from database");
            return DemoRemovalResult.Successful;
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
                var subscription = await _matchInfoGetter.GetMaxUserSubscriptionInMatchAsync(demo.MatchId);
                
                var storageTime = TimeSpan.FromDays(
                    _subscriptionConfigProvider.Config.SettingsFromSubscriptionType(subscription).MatchAccessDurationInDays);

                var expiryDate = demo.MatchDate + storageTime;
                _demoTable.SetExpiryDate(demo, expiryDate);

                if (expiryDate < DateTime.UtcNow)
                {   
                    RemoveDemo(demo);
                }
            }
        }



        public enum DemoRemovalResult
        {
            Successful = 200,
            NotInStorage = 400,
            NotFound = 404,
        }
    }
}