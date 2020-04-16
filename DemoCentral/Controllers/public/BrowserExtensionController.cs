using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.Producer;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Controllers
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/public")]
    [ApiController]
    public class BrowserExtensionController : ControllerBase
    {
        private readonly ILogger<BrowserExtensionController> _logger;
        private readonly IProducer<DemoInsertInstruction> _producer;

        public BrowserExtensionController(IProducer<DemoInsertInstruction> producer, ILogger<BrowserExtensionController> logger)
        {
            _logger = logger;
            _producer = producer;
        }


        /// <summary>
        /// Convert a browser extension request into a DemoInstertInstruction and put it in the corresponding queue
        /// </summary>
        /// <param name="uploaderId">steam id of the uploader</param>
        /// <param name="data">json string of the data associated with the request</param>
        /// <response code="200"> request was processed and put into queue</response>
        /// <response code="400"> request could not be processed</response>
        [HttpPost("extensionupload-valve")]
        public ActionResult InsertIntoGathererQueue(string data)
        {
            var matches = JsonConvert.DeserializeObject<JsonMatches>(data);

            _logger.LogInformation($"Received {matches.Matches.Length} new matches via browser extension, UploaderId [ {matches.Matches[0].UploaderId} ]");
            bool atLeastOneMatchFailed = false;

            foreach (var match in matches.Matches)
            {
                try
                {
                    var uploaderId = match.UploaderId;
                    var model = new DemoInsertInstruction
                    {
                        DownloadUrl = match.DownloadUrl,
                        UploaderId = uploaderId,
                        MatchDate = match.MatchDate,
                        Source = Source.Valve,
                        UploadType = UploadType.Extension
                    };

                    _producer.PublishMessage(model);
                    _logger.LogInformation($"Put new download request from browser extension in queue \n url:{model.DownloadUrl}");

                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Failed to insert download request from {match.DownloadUrl}");
                    atLeastOneMatchFailed = true;
                }
            }
            if (atLeastOneMatchFailed)
                return BadRequest();
            else
                return Ok();
        }

        public partial class JsonMatches
        {
            [JsonProperty("matches")]
            public Match[] Matches { get; set; }
        }

        public partial class Match
        {
            [JsonProperty("time")]
            public DateTime MatchDate { get; set; }

            [JsonProperty("url")]
            public string DownloadUrl { get; set; }

            [JsonProperty("steamId")]
            public long UploaderId { get; set; }
        }
    }
}