using Database.DatabaseClasses;
using DemoCentral.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Moq;
using System.Text;
using Microsoft.Extensions.Logging;
using DemoCentral;
using Microsoft.AspNetCore.Mvc;
using RabbitCommunicationLib.Enums;

namespace DemoCentralTests
{
    [TestClass]
    public class TrustedHashControllerTests
    {
        private readonly DbContextOptions<DemoCentralContext> _test_config;
        private readonly ILogger<HashController> mockILogger;
        private readonly IDemoTableInterface mockIDemoDBInterface;

        public TrustedHashControllerTests()
        {
            _test_config = DCTestsDBHelper.test_config;
            mockILogger = new Mock<ILogger<HashController>>().Object;
            mockIDemoDBInterface = new Mock<IDemoTableInterface>().Object;
        }

        [TestMethod]
        public void CreateHashReturnsHTTPConflictIfDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoTableInterface>();
            long matchId = 1;
            string hash = "test_hash";
            AnalyzerQuality quality = AnalyzerQuality.High;

            mockIDemoDBInterface.Setup(x => x.IsReanalysisRequired(hash, out matchId, quality)).Returns(false);
            ActionResult response;

            mockIDemoDBInterface.Object.IsReanalysisRequired(hash, out matchId, quality);

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockILogger);
                response = test.CreateHash(matchId, quality, hash);
            }
            Assert.IsInstanceOfType(response, typeof(ConflictObjectResult));
        }


        [TestMethod]
        public void CreateHashReturnsHTTPOkIfNotDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoTableInterface>();
            long matchId = 1;
            AnalyzerQuality quality = AnalyzerQuality.Low;
            mockIDemoDBInterface.Setup(x => x.IsReanalysisRequired("", out matchId, quality)).Returns(true);
            ActionResult response;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockILogger);
                response = test.CreateHash(matchId, quality,"");
            }
            Assert.IsInstanceOfType(response, typeof(OkResult));
        }


        [TestMethod]
        public void CreateHashSavesHashIfNotDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoTableInterface>();
            long matchId = 1;
            AnalyzerQuality quality = AnalyzerQuality.Low;
            string hash = "test_hash";
            mockIDemoDBInterface.Setup(x => x.IsReanalysisRequired(hash, out matchId,quality)).Returns(true);
            ActionResult response;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockILogger);
                response = test.CreateHash(matchId,quality, hash);
            }
            mockIDemoDBInterface.Verify(x => x.SetHash(matchId, hash), Times.Once);
            Assert.IsInstanceOfType(response, typeof(OkResult));
        }
    }
}
