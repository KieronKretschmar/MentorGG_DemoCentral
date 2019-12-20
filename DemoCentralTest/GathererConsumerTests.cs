using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoCentral;
using Moq;

namespace DemoCentralTests
{
    [TestClass]
    public class GathererConsumerTests
    {
        public GathererConsumerTests()
        {
        }

        [TestMethod]
        public void ConfirmHandleMethodIsCalled()
        {
        }


        [TestMethod]
        public void CreateModelMethodIsCalled()
        {
        }



        [TestMethod]
        public void NewDownloadUrlSendsMessageToDemoDownloader()
        {
        }


        [Ignore("DB not yet implemented")]
        [TestMethod]
        public void KnownUrlGetsSavedToUploadedDB()
        {
        }

        [Ignore("DB not yet implemented")]
        [TestMethod]
        public void KnownUrlGetsUploaderIdAdded()
        {
        }
    }
}
