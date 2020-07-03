using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoCentral;
using System;
using Database.DatabaseClasses;
using Moq;
using Microsoft.EntityFrameworkCore;
using RabbitCommunicationLib.Enums;
using System.Linq;
using System.Collections.Generic;

namespace DemoCentralTests
{
    [TestClass]
    public class inQueueTableInterfaceTests
    {
        private DbContextOptions<DemoCentralContext> _test_config;

        public inQueueTableInterfaceTests()
        {
            _test_config = DCTestsDBHelper.test_config;
        }

        [TestCleanup]
        public void DropDataBase()
        {
            using (var context = new DemoCentralContext(_test_config))
                context.Database.EnsureDeleted();
        }

        [TestMethod]
        public void AddCreatesANewDemoEntryWithSameMatchId()
        {
            long matchId = 1;

            using (var context = new DemoCentralContext(_test_config))
            {
                InQueueTableInterface testObject = new InQueueTableInterface(context);
                testObject.Add(matchId, Queue.DemoDownloader);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                InQueueDemo demo = GetDemoByMatchId(context, matchId);

                Assert.AreEqual(matchId, demo.MatchId);
            }
        }


        [TestMethod]
        public void GetPlayerMatchesInQueueReturnsEmptyListWithUnknownPlayer()
        {
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueTableInterface(context);
                var matches = test.GetPlayerMatchesInQueue(1234);

                Assert.AreEqual(0, matches.Count);
            }
        }

        [TestMethod]
        public void TotalQueueLengthReturnsCorrectInt()
        {
            long playerId = 1234;
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueTableInterface(context);

                int queueLength = test.GetTotalQueueLength();
                Assert.AreEqual(0, queueLength);

                test.Add(1, Queue.DemoDownloader);
                test.Add(2, Queue.DemoDownloader);
                test.Add(3, Queue.DemoDownloader);

                queueLength = test.GetTotalQueueLength();
                Assert.AreEqual(3, queueLength);
            }
        }

        [TestMethod]
        public void RemoveDemoFromQueueRemovesDemo()
        {
            long matchId = 1;
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueTableInterface(context);
                test.Add(matchId, Queue.DemoDownloader);
                test.Remove(test.GetDemoById(matchId));
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                var demo = context.InQueueDemo.Where(x => x.MatchId == matchId).SingleOrDefault();
                Assert.IsNull(demo);
            }
        }

        [TestMethod]
        public void RemoveDemoFromQueueFailsWithUnknownMatchId()
        {
            long matchId = 1;
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueTableInterface(context);
                Assert.ThrowsException<InvalidOperationException>(() => test.UpdateCurrentQueue(test.GetDemoById(matchId), Queue.DemoDownloader));
            }
        }

        [TestMethod]
        public void IncrementRetryIncreasesRetryCount()
        {
            long matchId = 1;
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueTableInterface(context);
                test.Add(matchId, Queue.DemoDownloader);

                for (int retry = 0; retry < 3; retry++)
                {
                    Assert.AreEqual(retry, test.IncrementRetry(test.GetDemoById(matchId)));
                }
            }
        }

        private InQueueDemo GetDemoByMatchId(DemoCentralContext context, long matchId)
        {
            return context.InQueueDemo.Where(x => x.MatchId == matchId).SingleOrDefault();
        }
    }
}
