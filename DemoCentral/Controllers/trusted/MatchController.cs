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

        public MatchController(ILogger<MatchController> logger, IMatchWriter matchWriter)
        {
            _logger = logger;
            _matchWriter = matchWriter;
        }

        /// <summary>
        /// Remove a demo from the match database
        /// </summary>
        /// <param name="matchId">match to remove</param>
        [HttpPost("match/{matchId}/remove-from-storage")]
        public void RemoveFromStorage(long matchId)
        {
            _logger.LogInformation($"Received request for removal from storage of match [ {matchId} ]");

            var instruction = new DemoRemovalInstruction
            {
                MatchId = matchId,
            };

            _matchWriter.PublishMessage(instruction);
        }
    }
}