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
            DateTime matchDate = new DateTime();
            Source source = Source.ManualUpload;
            long uploaderId = 1234;

            using (var context = new DemoCentralContext(_test_config))
            {
                InQueueTableInterface testObject = new InQueueTableInterface(context);
                testObject.Add(matchId, Queue.DemoDownloader);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                //Throws exception if entry not found or more than one are present
                InQueueDemo demo = GetDemoByMatchId(context, matchId);

                Assert.AreEqual(matchId, demo.MatchId);
                Assert.AreEqual(matchDate, demo.Demo.MatchDate);
            }
        }



        [TestMethod]
        public void GetPlayerMatchesInQueueReturnsMatches()
        {
            long playerId = 1234;
            List<InQueueDemo> matches;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueTableInterface(context);
                test.Add(1, Queue.DemoDownloader);
                test.Add(2, Queue.DemoDownloader);
                test.Add(3, Queue.DemoDownloader);

                matches = test.GetPlayerMatchesInQueue(playerId);
            }

            Assert.AreEqual(3, matches.Count);
            foreach (var match in matches)
                Assert.AreEqual(match.Demo.UploaderId, playerId);
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
        public void GetQueuePositionReturnsCorrectPosition()
        {
            long playerId1 = 1;
            long playerId2 = 2;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueTableInterface(context);

                test.Add(1, Queue.DemoDownloader);
                test.Add(2, Queue.DemoDownloader);
                test.Add(3, Queue.DemoDownloader);
                test.Add(4, Queue.DemoDownloader);

                for (int i = 1; i < 5; i++)
                    Assert.AreEqual(i - 1, test.GetQueuePosition(test.GetDemoById(i)));
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
