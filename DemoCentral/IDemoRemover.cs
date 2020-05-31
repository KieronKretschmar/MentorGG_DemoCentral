using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using DemoCentral.Enumerals;
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
        Task RemoveExpiredDemos(TimeSpan allowedTimeAfterExpiration);
    }

    public class DemoRemover : IDemoRemover
    {
        private readonly IDemoTableInterface _dBInterface;
        private readonly ILogger<DemoRemover> _logger;
        private readonly IProducer<DemoRemovalInstruction> _matchWriter;
        private readonly IMatchInfoGetter _matchInfoGetter;

        public DemoRemover(IDemoTableInterface dBInterface, ILogger<DemoRemover> logger, IProducer<DemoRemovalInstruction> matchWriter, IMatchInfoGetter matchInfoGetter)
        {
            _dBInterface = dBInterface;
            _logger = logger;
            _matchWriter = matchWriter;
            _matchInfoGetter = matchInfoGetter;
        }

        public DemoRemovalResult RemoveDemo(long matchId)
        {
            try
            {
                var demo = _dBInterface.GetDemoById(matchId);
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

            var instruction = new DemoRemovalInstruction
            {
                MatchId = matchId,
            };

            _matchWriter.PublishMessage(instruction);
            _logger.LogTrace($"Forwarded request of demo [ {matchId} ] to MatchWriter for removal from database");
            return DemoRemovalResult.Successful;
        }

        public async Task RemoveExpiredDemos(TimeSpan removalDelay)
        {
            _logger.LogInformation("Removing expired demos.");

            List<long> expiredDemos  = _dBInterface.GetExpiredDemosId();
            _logger.LogInformation($"Removing demos [ {string.Join(", ", expiredDemos)} ] ");

            foreach (var demo in expiredDemos)
            {
                DateTime removalDate = await _matchInfoGetter.CalculateDemoRemovalDateAsync(demo);
                var outdated = removalDate + removalDelay < DateTime.UtcNow;

                if (outdated)
                    RemoveDemo(demo);
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