using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoCentral;
using Moq;

namespace DemoCentralTests
{
    [TestClass]
    public class DemoDownloaderRPCCientTests
    {
        public DemoDownloaderRPCCientTests()
        {
        }

        [TestMethod]
        public void SendingMessageUpdatesFileStatus()
        {
        }


        [TestMethod]
        public void ReceivedMessageSetsFileStatus()
        {
        }


        [TestMethod]
        public void ReceivedSuccessfulMessageAddsFilePath()
        {
        }


        [TestMethod]
        public void ReceivedSuccessfulMessageRemovesFromQueue()
        {
        }


        [TestMethod]
        public void ReceivedSuccessfulMessageForwardToDemoFileWorker()
        {
        }

        [TestMethod]
        public void ReceivedFailedMessageIncrementsRetries()
        {
        }


        [TestMethod]
        public void ReceivedFailedMessageSetsDownloadRetrying()
        {
        }


        [TestMethod]
        public void ReceivedFailedMessageResendsMessage()
        {
        }


        [TestMethod]
        public void ReceivedFailedMessageWithExcessiveRetriesGetsRemovedFromQueue()
        {
        }


        [TestMethod]
        public void ReceivedFailedMessageWithExcessiveRetriesSetsStatusFailed()
        {
        }
    }
}
