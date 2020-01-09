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
        //https://localhost:44368/api/trusted/Hash/CreateHash?matchId=XXXX&hash=YYYYY
        public ActionResult CreateHash(long matchId, string hash)
        {
            bool duplicateHash = _dbInterface.IsDuplicateHash(hash);
            if (duplicateHash)
            {
                _logger.LogError($"Demo#{matchId} was duplicate via MD5Hash");
                return Conflict();
            }
            else
            {
                _logger.LogInformation($"Demo#{matchId} is unique");
                _dbInterface.UpdateHash(matchId, hash);

                return Ok();
            }
        }
    }


}