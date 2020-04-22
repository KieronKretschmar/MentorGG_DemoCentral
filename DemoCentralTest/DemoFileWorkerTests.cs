using System;
using System.Collections.Generic;
using System.Text;
using DataBase.DatabaseClasses;
using DataBase.Enumerals;
using DemoCentral;
using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentralTests
{
    [TestClass]
    public class DemoFileWorkerTests
    {
        public DemoFileWorkerTests()
        {

        }

        [TestMethod]
        public void PublishingMethodUpdatesQueueStatus()
        {

        }

        [TestMethod]
        public async System.Threading.Tasks.Task ReceivingZipFailedMessageUpdatesStatusAsync()
        {
            var testMatchId = 123456789;

            short timesCalled = 0; 

            var mockRPCConnection = new MockRPCQueueConnection();
            var mockQueueInterface = new Mock<IInQueueDBInterface>();
            var mockDbInterface = new Mock<IDemoCentralDBInterface>();
            var mockLogger = new Mock<ILogger<DemoFileWorker>>();
            var mockProducer = new Mock<IProducer<RedisLocalizationInstruction>>();

            mockDbInterface.Setup(x => x.SetFileWorkerStatus(It.IsAny<Demo>(), DemoFileWorkerStatus.UnzipFailed)).Callback(()=> timesCalled++);
            mockDbInterface.Setup(x => x.SetFileWorkerStatus(testMatchId, DemoFileWorkerStatus.UnzipFailed)).Callback(()=> timesCalled++);

            var services = new ServiceCollection();
            services.AddTransient<IInQueueDBInterface>(s => mockQueueInterface.Object);
            services.AddTransient<IDemoCentralDBInterface>(s => mockDbInterface.Object);
            services.AddTransient<ILogger<DemoFileWorker>>(s => mockLogger.Object);
            services.AddTransient<IProducer<RedisLocalizationInstruction>>(s => mockProducer.Object);


            var analyzeReport = new DemoAnalyzeReport
            {
                MatchId = testMatchId
            };



            var test = new DemoFileWorker(mockRPCConnection, services.BuildServiceProvider());
            await test.HandleMessageAsync(new RabbitMQ.Client.Events.BasicDeliverEventArgs(), analyzeReport);

            Assert.IsTrue(timesCalled == 1);
        }

        [TestMethod]
        public void ReceivingZipFailedMessageRemovesDemo()
        {

        }

        [TestMethod]
        public void ReceivingMessageWithDuplicatedHashRemovesDemo()
        {

        }


        [TestMethod]
        public void ReceivingSuccessfulMessageSetsFilePathAndZipStatus()
        {

        }

        [TestMethod]
        public void ReceivingSuccessfulMessageRemovesFromQueue()
        {

        }



        private static readonly string successfulResponseJSON =
            "{\"Success\": true,  \"Version\": \"testVersion\",  \"BlobDownloadFailed\": false,  \"Unzipped\": true,  \"DuplicateChecked\": true,\"IsDuplicate\": false,  \"UploadedToRedis\": true,  \"FramesPerSecond\": 1,  \"Hash\": \"testHash\"}";
    }
}
