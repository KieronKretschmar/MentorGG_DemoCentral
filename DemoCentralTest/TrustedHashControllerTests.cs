using DataBase.DatabaseClasses;
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

namespace DemoCentralTests
{
    [TestClass]
    public class TrustedHashControllerTests
    {
        private readonly DbContextOptions<DemoCentralContext> _test_config;
        private readonly ILogger<HashController> mockILogger;
        private readonly IDemoCentralDBInterface mockIDemoDBInterface;

        public TrustedHashControllerTests()
        {
            _test_config = DCTestsDBHelper.test_config;
            mockILogger = new Mock<ILogger<HashController>>().Object;
            mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>().Object;
        }

        [TestMethod]
        public void CreateHashReturnsHTTPConflictIfDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>();
            long matchId = 1;
            string hash = "test_hash";
            byte frames = 1;

            mockIDemoDBInterface.Setup(x => x.IsReanalysisRequired(hash, out matchId, frames)).Returns(true);
            ActionResult response;

            mockIDemoDBInterface.Object.IsReanalysisRequired(hash, out matchId);

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockILogger);
                response = test.CreateHash(matchId, frames, hash);
            }
            Assert.IsInstanceOfType(response, typeof(ConflictObjectResult));
        }


        [TestMethod]
        public void CreateHashReturnsHTTPOkIfNotDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>();
            long matchId = 1;
            byte frames = 1;
            mockIDemoDBInterface.Setup(x => x.IsReanalysisRequired("", out matchId, frames)).Returns(false);
            ActionResult response;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockILogger);
                response = test.CreateHash(matchId, frames,"");
            }
            Assert.IsInstanceOfType(response, typeof(OkResult));
        }


        [TestMethod]
        public void CreateHashSavesHashIfNotDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>();
            long matchId = 1;
            byte frames = 1;
            string hash = "test_hash";
            mockIDemoDBInterface.Setup(x => x.IsReanalysisRequired("", out matchId,frames)).Returns(false);
            ActionResult response;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockILogger);
                response = test.CreateHash(matchId,frames, hash);
            }
            mockIDemoDBInterface.Verify(x => x.SetHash(matchId, hash), Times.Once);
            Assert.IsInstanceOfType(response, typeof(OkResult));
        }
    }
}
