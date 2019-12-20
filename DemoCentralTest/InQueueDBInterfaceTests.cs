using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoCentral;
using Moq;

namespace DemoCentralTests
{
    [TestClass]
    public class InQueueDBInterfaceTests
    {
        public InQueueDBInterfaceTests()
        {
        }

        [TestMethod]
        public void AddCreatesANewDemoEntryWithSameMatchId()
        {
        }

        [TestMethod]
        public void AddCreatesANewDemoEntryNotInAnyQueues()
        {
        }


        [TestMethod]
        public void UpdateQueueStatusSetsStatusOnKnownQueue()
        {
        }



        [TestMethod]
        public void UpdateQueueStatusFailsOnUnknownQueue()
        {
        }

        [TestMethod]
        public void UpdateQueueStatusRemovesDemoIfNotInAnyQueue()
        {
        }

        [TestMethod]
        public void GetPlayerMatchesInQueueReturnsMatches()
        {
        }


        [TestMethod]
        public void GetPlayerMatchesInQueueFailsWithUnknownPlayer()
        {
        }

        [TestMethod]
        public void TotalQueueLengthReturnsCorrectInt()
        {
        }

        [TestMethod]
        public void RemoveDemoFromQueueRemovesDemo()
        {
        }

        [TestMethod]
        public void RemoveDemoFromQueueFailsWithUnknownMatchId()
        {
        }

        [TestMethod]
        public void GetQueuePositionReturnsCorrectPosition()
        {
        }

        [TestMethod]
        public void IncrementRetryIncreasesRetryCount()
        {
        }
    }
}
