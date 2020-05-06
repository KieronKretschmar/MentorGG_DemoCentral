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

        public MatchDownloadController(IDemoTableInterface demoTableInterface, Communication.Rabbit.IBlobStorage blobStorage)
        {
            _demoTableInterface = demoTableInterface;
        }
        
        [HttpGet("match/{matchId}/download-url")]
        public ActionResult<string> GetDownloadUrl(long matchId)
        {
            Database.DatabaseClasses.Demo demo;
            try
            {
                demo = _demoTableInterface.GetDemoById(matchId);
                if (!String.IsNullOrEmpty(demo.BlobUrl))
                {
                    return demo.BlobUrl;
                }
                return NotFound();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
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