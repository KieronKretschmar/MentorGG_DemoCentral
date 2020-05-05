using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoCentral.Communication.Rabbit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Controllers.trusted
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/trusted")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        private readonly ILogger<MatchController> _logger;
        private readonly IMatchWriter _matchWriter;
        private readonly IDemoTableInterface _demoTableInterface;

        public MatchController(ILogger<MatchController> logger, IMatchWriter matchWriter, IDemoTableInterface demoTableInterface)
        {
            _logger = logger;
            _matchWriter = matchWriter;
            _demoTableInterface = demoTableInterface;
        }

        /// <summary>
        /// Remove a demo from the match database and blob Storage
        /// </summary>
        /// <param name="matchId">match to remove</param>
        [HttpPost("match/{matchId}/remove-from-storage")]
        public ActionResult RemoveFromStorage(long matchId)
        {
            _logger.LogInformation($"Received request for removal from storage of match [ {matchId} ]");

            try
            {
                var demo = _demoTableInterface.GetDemoById(matchId);
                if (demo.FileStatus != DataBase.Enumerals.FileStatus.InBlobStorage)
                    throw new ArgumentException($"Demo [ {matchId} ] is not in blob storage, Removal request cancelled");
            }
            catch (Exception e) when ( e is ArgumentException)
            {
                _logger.LogInformation(e, $"Demo [ {matchId} ] was not removed from blob storage");
                return BadRequest();
            }
            catch(Exception e) when (e is InvalidOperationException)
            {
                _logger.LogInformation(e, $"Demo [ {matchId} ] does not exist, Removal request cancelled");
                return NotFound();
            }

            var instruction = new DemoRemovalInstruction
            {
                MatchId = matchId,
            };

            _matchWriter.PublishMessage(instruction);
            _logger.LogTrace($"Forwarded request of demo [ {matchId} ] to MatchWriter for removal from database");
            return Ok();
        }
    }
}