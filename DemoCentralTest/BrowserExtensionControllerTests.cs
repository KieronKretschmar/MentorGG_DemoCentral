using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using DemoCentral.Controllers;
using static DemoCentral.Controllers.BrowserExtensionController;

namespace DemoCentralTests
{
    [TestClass]
    public class BrowserExtensionControllerTests
    {
        private readonly ILogger<BrowserExtensionController> _mockILogger;
        private const string browserExtensionJson = "{\"matches\":[{\"time\": \"0001-01-01T00:00:00\",\"url\": \"https://demos-asia-southeast1.faceit-cdn.net/csgo/76d083e1-9808-48e4-aaf0-9d1d49343b28.dem.gz\",\"steamId\": 123456789,\"source\": 1}]}";



public BrowserExtensionControllerTests()
        {
            _mockILogger = new Mock<ILogger<BrowserExtensionController>>().Object;
        }

        [TestMethod]
        public void CallsProducer()
        {

            var testProducer = new Mock<IProducer<DemoInsertInstruction>>();
            var test = new BrowserExtensionController(testProducer.Object, _mockILogger);

            var result  = test.InsertIntoGathererQueue(browserExtensionJson);

            Assert.IsTrue(result is OkResult);
            testProducer.Verify(x => x.PublishMessage(It.IsAny<DemoInsertInstruction>(),null),Times.Once);
        }
    }
}
