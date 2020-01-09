using DataBase.DatabaseClasses;
using DemoCentral.Controllers.trusted;
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
        private readonly ILogger<HashController> mockIlogger;
        private readonly IDemoCentralDBInterface mockIDemoDBInterface;

        public TrustedHashControllerTests()
        {
            _test_config = DCTestsDBHelper.test_config;
            mockIlogger = new Mock<ILogger<HashController>>().Object;
            mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>().Object;
        }

        [TestMethod]
        public void GetHashReturnsHTTPConflictIfDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>();
            mockIDemoDBInterface.Setup(x => x.IsDuplicateHash("")).Returns(true);
            ActionResult response;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockIlogger);
                response = test.CreateHash(1, "");
            }
            Assert.IsInstanceOfType(response, typeof(ConflictObjectResult));
        }


        [TestMethod]
        public void GetHashReturnsHTTPOkIfNotDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>();
            mockIDemoDBInterface.Setup(x => x.IsDuplicateHash("")).Returns(false);
            ActionResult response;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockIlogger);
                response = test.CreateHash(1, "");
            }
            Assert.IsInstanceOfType(response, typeof(OkResult));
        }


        [TestMethod]
        public void GetHashSavesHashIfNotDuplicated()
        {
            var mockIDemoDBInterface = new Mock<IDemoCentralDBInterface>();
            mockIDemoDBInterface.Setup(x => x.IsDuplicateHash("")).Returns(false);
            string hash = "test_hash";
            long matchId = 1;
            ActionResult response;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new HashController(mockIDemoDBInterface.Object, mockIlogger);
                response = test.CreateHash(matchId, hash);
            }
            mockIDemoDBInterface.Verify(x => x.SetHash(matchId, hash), Times.Once);
            Assert.IsInstanceOfType(response, typeof(OkResult));
        }
    }
}
