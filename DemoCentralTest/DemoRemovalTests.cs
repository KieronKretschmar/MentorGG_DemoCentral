using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DataBase.DatabaseClasses;
using DataBase.Enumerals;
using DemoCentral;
using DemoCentral.Communication.Rabbit;
using DemoCentral.Controllers.trusted;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DemoCentralTests
{
    [TestClass]
    public class DemoRemovalTests
    {
        private readonly DbContextOptions<DemoCentralContext> _test_config;
        private readonly string _test_blobContainer;
        private readonly string _test_blobConnectionString;

        public DemoRemovalTests()
        {
            _test_config = DCTestsDBHelper.test_config;
            _test_blobContainer = "test-container";
            _test_blobConnectionString = "UseDevelopmentStorage=true";
        }

        [TestMethod]
        public void ControllerForwardsToMatchWriter()
        {
            var mockMatchWriter = new Mock<IMatchWriter>();
            var mockILogger = new Mock<ILogger<MatchController>>();
            var mockDBInterface = new Mock<IDemoDBInterface>();
            var testId = 123456789;
            mockDBInterface.Setup(x => x.GetDemoById(testId)).Returns(new Demo
            {
                BlobUrl = "test_url",
                FileStatus = FileStatus.InBlobStorage,
                MatchId = testId,
            });

            var test = new MatchController(mockILogger.Object, mockMatchWriter.Object, mockDBInterface.Object);
            var res = test.RemoveFromStorage(testId);

            mockMatchWriter.Verify(x => x.PublishMessage(It.IsAny<DemoRemovalInstruction>()), Times.Once);
            Assert.IsInstanceOfType(res, typeof(OkResult));
        }


        [TestMethod]
        public void ControllerChecksIfDemoExists()
        {

            var mockMatchWriter = new Mock<IMatchWriter>();
            var mockILogger = new Mock<ILogger<MatchController>>();
            var mockDBInterface = new Mock<IDemoDBInterface>();
            var testId = 123456789;
            mockDBInterface.Setup(x => x.GetDemoById(testId)).Throws<InvalidOperationException>();

            var test = new MatchController(mockILogger.Object, mockMatchWriter.Object, mockDBInterface.Object);
            var res = test.RemoveFromStorage(testId);
            Assert.IsInstanceOfType(res, typeof(BadRequestResult));
        }

        //Currently ignored as mocking an `IRPCQueueConnection` is not possible
        //Using a correct connection just seems unclean and overkill
        [Ignore]
        [TestMethod]
        public async Task ReceivedSuccessfulMessageSetsFileStatusAndBlobStorageDeletedAsync()
        {
            var testId = 123456789;
            var mockILogger = new Mock<ILogger<MatchWriter>>();

            //Can not mock rpc queue connection this way
            var mockRpcConnection = new Mock<IRPCQueueConnections>();
            var mockDbInterface = new Mock<IDemoDBInterface>();
            var mockBlobStorage = new Mock<IBlobStorage>();
            var mockResponse = new TaskCompletedReport
            {
                MatchId = testId,
                Success = true,
            };

            var test = new MatchWriter(mockRpcConnection.Object, mockDbInterface.Object, mockBlobStorage.Object, mockILogger.Object);

            var res = await test.HandleMessageAsync(new RabbitMQ.Client.Events.BasicDeliverEventArgs(), mockResponse);
            Assert.IsTrue(res == RabbitCommunicationLib.Enums.ConsumedMessageHandling.Done);
            mockDbInterface.Verify(x => x.SetFileStatus(It.IsAny<Demo>(), FileStatus.Removed), Times.Once);
            mockBlobStorage.Verify(x => x.DeleteBlobAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task BlobStorageDeleteWorksAsync()
        {
            try
            {
                var mockLogger = new Mock<ILogger<BlobStorage>>();
                var test = new BlobStorage(_test_blobConnectionString, mockLogger.Object);

                //Upload blob
                var client = new BlobContainerClient(_test_blobConnectionString, _test_blobContainer);
                await client.CreateAsync(PublicAccessType.Blob);
                var blobClient = client.GetBlobClient("DeletionTest");
                await blobClient.UploadAsync(Stream.Null);

                string blobUrl = blobClient.Uri.AbsoluteUri;

                await test.DeleteBlobAsync(blobUrl);

                Assert.IsFalse(await blobClient.ExistsAsync());
            }
            finally
            {
                //Not calling this in the CleanUp method as it is only needed for blob storage
                //and forces the Storage Emulator to be running.
                //Maybe creating an Attribute like [BlobStorageTest] which checks that the storage emulator is running
                //and calls for the cleanup of the test  container is a better solution
                DeleteBlobstorageTestContainer();
            }
        }


        [TestCleanup]
        public void Cleanup()
        {
            using (var context = new DemoCentralContext(_test_config))
            {
                context.Database.EnsureDeleted();
            }
        }

        public void DeleteBlobstorageTestContainer()
        {
            var client = new BlobServiceClient(_test_blobConnectionString);
            client.DeleteBlobContainer(_test_blobContainer);
        }
    }
}
