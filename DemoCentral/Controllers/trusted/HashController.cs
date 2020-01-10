using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;
using DataBase.DatabaseClasses;
using System.Net;
using Microsoft.Extensions.Logging;

namespace DemoCentral.Controllers.trusted
{
    [Route("api/trusted/[controller]")]
    [ApiController]
    public class HashController : ControllerBase
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly ILogger<HashController> _logger;

        public HashController(IDemoCentralDBInterface dbInterface, ILogger<HashController> logger)
        {
            _dbInterface = dbInterface;
            _logger = logger;
        }

        /// <summary>
        /// Check if the hash is already in the database, create if not
        /// </summary>
        /// <param name="matchId">id of the match to potentially create</param>
        /// <param name="hash">hash to check</param>
        /// <returns>Conflict or Ok if hash is known or not</returns>
        [HttpPost("[action]")]
        //POST api/trusted/Hash/CreateHash?matchId=XXXX&hash=YYYYY
        public ActionResult CreateHash(long matchId, string hash)
        {
            bool duplicateHash = _dbInterface.IsDuplicateHash(hash, out long possibleMatchId);
            try
            {

                if (duplicateHash)
                {
                    string error = $"Demo#{matchId} was duplicate of Demo#{possibleMatchId} via MD5Hash";
                    _logger.LogWarning(error);
                    return Conflict(error);
                }
                else
                {
                    _logger.LogInformation($"Demo#{matchId} is unique");
                    _dbInterface.SetHash(matchId, hash);

                    return Ok();
                }
            }
            catch (InvalidOperationException)
            {
                string critical = $"Requested hash update for non-existing demo#{matchId} \n " +
                    $"One should have been created by DemoCentral on first receiving the demo from the Gatherer \n" +
                    "THIS CASE SHOULD NEVER HAPPEN IN PRODUCTION";
                _logger.LogCritical(critical);
                throw new InvalidOperationException(critical);
            }
        }

    }
}
