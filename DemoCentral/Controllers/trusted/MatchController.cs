using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoCentral.Communication.Rabbit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Controllers.trusted
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/trusted")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        private readonly ILogger<MatchController> _logger;
        private readonly IDemoRemover _demoRemover;

        public MatchController(ILogger<MatchController> logger, IDemoRemover demoRemover)
        {
            _logger = logger;
            _demoRemover = demoRemover;
        }

        /// <summary>
        /// Remove a demo from the match database and blob Storage
        /// </summary>
        /// <param name="matchId">match to remove</param>
        [HttpPost("match/{matchId}/remove-from-storage")]
        public ActionResult RemoveFromStorage(long matchId)
        {
            _logger.LogInformation($"Received request for removal from storage of match [ {matchId} ]");

            var removalResult = _demoRemover.RemoveDemo(matchId);

            switch (removalResult)
            {
                case DemoRemover.DemoRemovalResult.Successful:
                    return Ok();
                case DemoRemover.DemoRemovalResult.NotInStorage:
                    return BadRequest();
                case DemoRemover.DemoRemovalResult.NotFound:
                    return NotFound();
            }

            //If you get here, there has to be an internal error,
            //as the above enum should catch all possible outcomes
            return StatusCode(500);
        }
    }
}