using System;
using System.Collections.Generic;
using System.Text;
using DemoCentral;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        public void ReceivingZipFailedMessageUpdatesStatus()
        {

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
