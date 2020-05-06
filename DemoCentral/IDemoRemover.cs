using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;

namespace DemoCentral
{
    public interface IDemoRemover
    {
        DemoRemover.DemoRemovalResult RemoveDemo(long matchId);
        void RemoveExpiredDemos(TimeSpan allowedTimeAfterExpiration);
    }

    public class DemoRemover : IDemoRemover
    {
        private readonly IDemoCentralDBInterface _dBInterface;
        private readonly ILogger<DemoRemover> _logger;
        private readonly IMatchWriter _matchWriter;

        public DemoRemover(IDemoCentralDBInterface dBInterface, ILogger<DemoRemover> logger, IMatchWriter matchWriter)
        {
            _dBInterface = dBInterface;
            _logger = logger;
            _matchWriter = matchWriter;
        }

        public DemoRemovalResult RemoveDemo(long matchId)
        {
            try
            {
                var demo = _dBInterface.GetDemoById(matchId);
                if (demo.FileStatus != DataBase.Enumerals.FileStatus.InBlobStorage)
                    throw new ArgumentException($"Demo [ {matchId} ] is not in blob storage, Removal request cancelled");
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

        public void RemoveExpiredDemos(TimeSpan allowedTimeAfterExpiration)
        {
            _logger.LogInformation("Removing expired demos.");

            List<long> expiredDemos  = _dBInterface.GetExpiredDemosId(allowedTimeAfterExpiration);
            _logger.LogInformation($"Removing demos [ {string.Join(", ", expiredDemos)} ] ");

            foreach (var demo in expiredDemos)
                RemoveDemo(demo);
        }


        public enum DemoRemovalResult
        {
            Successful = 200,
            NotInStorage = 400,
            NotFound = 404,
        }
    }
}