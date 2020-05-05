using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoCentral.Helpers;
using DemoCentral.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoCentral.Controllers
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/public")]
    [ApiController]
    public class MatchDownloadController : ControllerBase
    {
        private readonly IDemoTableInterface _demoTableInterface;

        public MatchDownloadController(IDemoTableInterface demoTableInterface)
        {
            _demoTableInterface = demoTableInterface;
        }

        [HttpGet("matches/{matchIds}/download-urls")]
        public DownloadUrlModel GetDownloadUrlsForMatches([ModelBinder(typeof(CsvModelBinder))] List<long> matchIds) 
        {
            var res = new DownloadUrlModel();
            var listOfDemos = new List<DemoUrlPair>();
            foreach (var matchId in matchIds)
            {
                var demoUrlPair = new DemoUrlPair();
                var demo = _demoTableInterface.GetDemoById(matchId);
                
                demoUrlPair.MatchId = matchId;
                demoUrlPair.DownloadUrl = demo.BlobUrl;
                listOfDemos.Add(demoUrlPair);
            }

            res.demos = listOfDemos;
            return res;
        }
    }
}