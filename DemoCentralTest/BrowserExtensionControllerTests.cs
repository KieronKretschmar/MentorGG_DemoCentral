using DemoCentral.Controllers.trusted;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace DemoCentralTests
{
    [TestClass]
    public class BrowserExtensionControllerTests
    {
        private readonly ILogger<BrowserExtensionController> _mockILogger;
        private const string browserExtensionJson = "{\" MatchDate \": \"0001-01-01T00:00:00\",\"DownloadUrl\": null,\"Source\": \"valve\"}";


        public BrowserExtensionControllerTests()
        {
            _mockILogger = new Mock<ILogger<BrowserExtensionController>>().Object;
        }

        [TestMethod]
        public void CallsProducer()
        {
            long testUploaderId = 123456789;

            var testProducer = new Mock<IProducer<DemoInsertInstruction>>();
            var test = new BrowserExtensionController(testProducer.Object, _mockILogger);

            var result  = test.InsertIntoGathererQueue(testUploaderId, browserExtensionJson);

            Assert.IsTrue(result is OkResult);
            testProducer.Verify(x => x.PublishMessage(It.IsAny<DemoInsertInstruction>(),null),Times.Once);
        }
    }
}
