﻿using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Database.DatabaseClasses;
using DemoCentral;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using DemoCentral.Communication.RabbitConsumers;
using DemoCentral.Controllers.trusted;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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
            var mockILogger = new Mock<ILogger<MatchController>>();
            var mockDemoRemover = new Mock<IDemoRemover>();
            var testId = 123456789;

            var test = new MatchController(mockILogger.Object, mockDemoRemover.Object);
            var res = test.RemoveFromStorage(testId);

            mockDemoRemover.Verify(x => x.RemoveDemo(testId), Times.Once);
        }


        [TestMethod]
        public void RemoverChecksIfDemoExists()
        {
            var mockMatchWriter = new Mock<IProducer<DemoRemovalInstruction>>();
            var mockILogger = new Mock<ILogger<DemoRemover>>();
            var mockDBInterface = new Mock<IDemoTableInterface>();
            var mockDemoRemover = new Mock<IDemoRemover>();
            var mockMatchInfoGetter = new Mock<IMatchInfoGetter>();
            var testId = 123456789;
            mockDBInterface.Setup(x => x.GetDemoById(testId)).Throws<InvalidOperationException>();

            var test = new DemoRemover(mockDBInterface.Object, mockILogger.Object, mockMatchWriter.Object,mockMatchInfoGetter.Object);
            var res = test.RemoveDemo(testId);
            Assert.AreEqual(res, DemoRemover.DemoRemovalResult.NotFound);
        }

        //Currently ignored as mocking an `IRPCQueueConnection` is not possible
        //Using a correct connection just seems unclean and overkill
        [Ignore]
        [TestMethod]
        public async Task ReceivedSuccessfulMessageSetsFileStatusAndBlobStorageDeletedAsync()
        {
            var testId = 123456789;
            var mockILogger = new Mock<ILogger<MatchWriterRemovalReportConsumer>>();

            var services = new ServiceCollection();
            var mockRabbit = new MockRabbitConnection();
            var mockDbInterface = new Mock<IDemoTableInterface>();
            var mockBlobStorage = new Mock<IBlobStorage>();
            var mockResponse = new TaskCompletedReport(testId) { Success = true};

            services.AddTransient(o => mockDbInterface);
            services.AddTransient(o => mockBlobStorage);

            var test = new MatchWriterRemovalReportConsumer(services.BuildServiceProvider(), mockILogger.Object,mockRabbit);

            var res = await test.HandleMessageAsync(new RabbitMQ.Client.Events.BasicDeliverEventArgs(), mockResponse);
            Assert.IsTrue(res == RabbitCommunicationLib.Enums.ConsumedMessageHandling.Done);
            mockBlobStorage.Verify(x => x.DeleteBlobAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void TimedDemoRemovalCallsDemoRemover()
        {
            var testInterval = TimeSpan.FromMilliseconds(25);
            var testTimeSpan = TimeSpan.FromMilliseconds(10);
            var mockDemoRemover = new Mock<IDemoRemover>();
            var mockLogger = new Mock<ILogger<TimedDemoRemovalCaller>>();


            var test = new TimedDemoRemovalCaller(testInterval, testTimeSpan, mockDemoRemover.Object, mockLogger.Object);

            var cancellationToken = new CancellationToken();

            test.StartAsync(cancellationToken);

            Thread.Sleep(50);

            test.StopAsync(cancellationToken);

            mockDemoRemover.Verify(x => x.RemoveExpiredDemos(testTimeSpan), Times.AtLeastOnce);
        }

        [TestMethod]
        public void DemoRemoverRemovesFoundDemo()
        {
            var testId = 123456789;
            var testDemo = new Demo
            {
                MatchId = testId,
            };

            var mockDbInterface = new Mock<IDemoTableInterface>();

            mockDbInterface.Setup(x => x.GetDemoById(testId)).Returns(testDemo);
            var mockLogger = new Mock<ILogger<DemoRemover>>();
            var mockMatchWriter = new Mock<IProducer<DemoRemovalInstruction>>();
            var mockMatchInfoGetter = new Mock<IMatchInfoGetter>();


            var test = new DemoRemover(mockDbInterface.Object, mockLogger.Object, mockMatchWriter.Object, mockMatchInfoGetter.Object);

            var res = test.RemoveDemo(testId);

            Assert.AreEqual(DemoRemover.DemoRemovalResult.Successful, res);
            mockMatchWriter.Verify(x => x.PublishMessage(It.IsAny<DemoRemovalInstruction>(),null), Times.Once);
  
        }


        [TestMethod]
        public async Task DemoRemoverChecksRemovalDateAsync()
        {
            var testId = 123456789;
            var mockDbInterface = new Mock<IDemoTableInterface>();
            var mockLogger = new Mock<ILogger<DemoRemover>>();
            var mockMatchWriter = new Mock<IProducer<DemoRemovalInstruction>>();
            var mockMatchInfoGetter = new Mock<IMatchInfoGetter>();

            mockDbInterface.Setup(x => x.GetExpiredDemosId()).Returns(new List<long> { testId });
            mockDbInterface.Setup(x => x.GetDemoById(testId)).Throws<InvalidOperationException>();
            int hasCalled = 0;
            mockMatchInfoGetter.Setup(x => x.CalculateDemoRemovalDateAsync(It.IsAny<Demo>())).Callback(() => hasCalled += 1);
            mockMatchInfoGetter.Setup(x => x.CalculateDemoRemovalDateAsync(It.IsAny<long>())).Callback(() => hasCalled += 1);

            var test = new DemoRemover(mockDbInterface.Object, mockLogger.Object, mockMatchWriter.Object, mockMatchInfoGetter.Object);
            await test.RemoveExpiredDemos(TimeSpan.Zero);

            Assert.AreEqual(1, hasCalled);
        }

        #region BlobStorage related tests
        [TestMethod]
        [Ignore]
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
        #endregion

        #region CleanUp methods
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
        #endregion
    }
}
