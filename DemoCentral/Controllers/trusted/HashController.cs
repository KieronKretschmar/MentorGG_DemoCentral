using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;
using DataBase.DatabaseClasses;
using System.Net;

namespace DemoCentral.Controllers.trusted
{
    [Route("api/trusted/[controller]")]
    [ApiController]
    public class HashController : ControllerBase
    {
        private readonly DemoCentralDBInterface _dbInterface;

        public HashController(DemoCentralDBInterface dbInterface)
        {
            _dbInterface = dbInterface;
        }

        /// <summary>
        /// Check if the hash is already in the database, create if not
        /// </summary>
        /// <param name="matchId">id of the match to potentially create</param>
        /// <param name="hash">hash to check</param>
        /// <returns>Conflict or Ok if hash is known or not</returns>
        [HttpPost]
        public ActionResult CreateHash(long matchId, string hash)
        {
            bool duplicateHash = _dbInterface.IsDuplicateHash(hash);
            if (duplicateHash)
            {
                return Conflict();
            }
            else
            {
                _dbInterface.UpdateHash(matchId, hash);

                return Ok();
            }

        }
    }


}