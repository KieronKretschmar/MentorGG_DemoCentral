using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoCentral;
using System;
using DataBase.DatabaseClasses;
using Moq;
using Microsoft.EntityFrameworkCore;
using RabbitTransfer.Enums;
using System.Linq;

namespace DemoCentralTests
{
    [TestClass]
    public class InQueueDBInterfaceTests
    {
        private DbContextOptions<DemoCentralContext> _test_config;

        public InQueueDBInterfaceTests()
        {
             _test_config = DCTestsDBHelper.test_config;
        }

        [TestMethod]
        public void AddCreatesANewDemoEntryWithSameMatchId()
        {
            long matchId = 1;
            DateTime matchDate = new DateTime();
            Source source = Source.ManualUpload;
            long uploaderId = 1234;

            using (var context = new DemoCentralContext(_test_config))
            {
                InQueueDBInterface testObject = new InQueueDBInterface(context);
                testObject.Add(matchId, matchDate, source, uploaderId);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                //Throws exception if entry not found or more than one are present
                InQueueDemo demo = context.InQueueDemo.Where(x => x.MatchId == matchId).Single();

                Assert.AreEqual(matchId, demo.MatchId);
                Assert.AreEqual(matchDate, demo.MatchDate);
                Assert.AreEqual(source, demo.Source);
                Assert.AreEqual(uploaderId, demo.UploaderId);
            }
        }

        [TestMethod]
        public void AddCreatesANewDemoEntryNotInAnyQueues()
        {
            long matchId = 1;
            DateTime matchDate = new DateTime();
            Source source = Source.ManualUpload;
            long uploaderId = 1234;

            using (var context = new DemoCentralContext(_test_config))
            {
                InQueueDBInterface testObject = new InQueueDBInterface(context);
                testObject.Add(matchId, matchDate, source, uploaderId);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                //Throws exception if entry not found or more than one are present
                InQueueDemo demo = context.InQueueDemo.Where(x => x.MatchId == matchId).Single();

                Assert.IsFalse(demo.SOQUEUE);
                Assert.IsFalse(demo.DFWQUEUE);
                Assert.IsFalse(demo.DDQUEUE);
            }
        }

        [TestCleanup]
        public void DropDataBase()
        {
            using (var context = new DemoCentralContext(_test_config))
                context.Database.EnsureDeleted();
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
