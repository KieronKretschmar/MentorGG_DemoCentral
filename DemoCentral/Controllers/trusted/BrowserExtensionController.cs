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

namespace DemoCentral.Controllers.trusted
{
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/trusted")]
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
        /// <param name="jsonModel">json string of the data associated with the request</param>
        /// <response code="200"> request was processed and put into queue</response>
        /// <response code="400"> request could not be processed</response>
        [HttpPost("extensionupload")]
        public ActionResult InsertIntoGathererQueue(long uploaderId, string jsonModel)
        {
            _logger.LogInformation($"Received new browwser extension upload from {uploaderId}");
            
            try
            {
                var match = JsonConvert.DeserializeObject<JsonModel>(jsonModel);

                var model = new DemoInsertInstruction
                {
                    DownloadUrl = match.DownloadUrl,
                    UploaderId = uploaderId,
                    MatchDate = match.MatchDate,
                    Source = match.Source,
                    UploadType = UploadType.Extension
                };

                _producer.PublishMessage(model);
                _logger.LogInformation($"Put new download request from browser extension in queue \n url:{model.DownloadUrl}");
                return Ok();

            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to insert download request from {uploaderId} due to {e}");
                return BadRequest();
            }
        }


        public struct JsonModel
        {
            public DateTime MatchDate;
            public string DownloadUrl;
            public Source Source;
        }
    }
}