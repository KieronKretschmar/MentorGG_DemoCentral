using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoCentral.Models;
using DemoCentral.DatabaseClasses;
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

        [HttpGet]
        public IActionResult GetHash(long matchId, string hash)
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