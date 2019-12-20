using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoCentral;
using Moq;

namespace DemoCentralTests
{
    [TestClass]
    public class DemoCentralDBInterfaceTests
    {
        public DemoCentralDBInterfaceTests()
        {
        }

        [TestMethod]
        public void UpdateHashSetsNewHash()
        {
        }

        [TestMethod]
        public void UpdateHashFailsWithUnknownMatch()
        {
        }


        [TestMethod]
        public void CreateDemoFileWorkerModelReturnFunctioningModel()
        {
        }



        [TestMethod]
        public void GetRecentMatchesReturnsListOfValidMatches()
        {
        }

        [TestMethod]
        public void GetRecentMatchesSkipsOffset()
        {
        }

        [TestMethod]
        public void GetRecentMatchesDoesNotFailsIfRequestingMoreMatchesThanExist()
        {
        }


        [TestMethod]
        public void SetFileStatusZippedSetsCorrectStatus()
        {
        }

        [TestMethod]
        public void SetFileStatusDownloadedSetsCorrectStatus()
        {
        }

        [TestMethod]
        public void AddFilePathSetsPath()
        {
        }

        [TestMethod]
        public void RemoveDemoRemovesDemo()
        {
        }

        [TestMethod]
        public void SetUploadStatusSetsCorrectStatus()
        {
        }

        [TestMethod]
        public void GetRecentMatchIdsReturnsListOfValidMatchIds()
        {
        }

        [TestMethod]
        public void GetRecentMatchIdsSkipsOffset()
        {
        }

        [TestMethod]
        public void GetRecentMatchIdsDoesNotFailsIfRequestingMoreMatchesThanExist()
        {
        }

        [TestMethod]
        public void SetDownloadRetryingAndGetDownloadPathSetsDownloadRetrying()
        {
        }


        [TestMethod]
        public void SetDownloadRetryingAndGetDownloadPathReturnsCorrectPath()
        {
        }

        [TestMethod]
        public void IsDuplicateHashOutputsCorrectBool()
        {
        }

        [TestMethod]
        public void TryCreateNewDemoEntryFromGathererCreatesAEntryForNewDemo()
        {
        }

        [TestMethod]
        public void TryCreateNewDemoEntryFromGathererAddsDemoToQueueTable()
        {
        }

        [TestMethod]
        public void TryCreateNewDemoEntryFromGathererReturnsFalseOnKnownDemo()
        {
        }

    }
}
