using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoCentral;
using System;
using DataBase.DatabaseClasses;
using Moq;
using Microsoft.EntityFrameworkCore;
using RabbitCommunicationLib.Enums;
using System.Linq;
using Database.Enumerals;
using System.Collections.Generic;

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
                InQueueDBInterface testObject = new InQueueDBInterface(context);
                testObject.Add(matchId, matchDate, source, uploaderId);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                //Throws exception if entry not found or more than one are present
                InQueueDemo demo = GetDemoByMatchId(context, matchId);

                Assert.AreEqual(matchId, demo.MatchId);
                Assert.AreEqual(matchDate, demo.MatchDate);
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
                InQueueDemo demo = GetDemoByMatchId(context, matchId);

                Assert.IsFalse(demo.SOQUEUE);
                Assert.IsFalse(demo.DFWQUEUE);
                Assert.IsFalse(demo.DDQUEUE);
            }
        }


        [TestMethod]
        public void UpdateQueueStatusSetsStatusOnKnownQueue()
        {
            long matchId = 1;
            using (var context = new DemoCentralContext(_test_config))
            {
                InQueueDBInterface test = new InQueueDBInterface(context);
                test.Add(matchId, new DateTime(), Source.Faceit, 1234);

                test.UpdateProcessStatus(matchId, ProcessedBy.DemoDownloader, true);
                test.UpdateProcessStatus(matchId, ProcessedBy.DemoFileWorker, true);
                test.UpdateProcessStatus(matchId, ProcessedBy.SituationsOperator, true);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                InQueueDemo demo = GetDemoByMatchId(context, matchId);

                Assert.IsTrue(demo.DDQUEUE);
                Assert.IsTrue(demo.SOQUEUE);
                Assert.IsTrue(demo.DFWQUEUE);
            }
        }

        [TestMethod]
        public void GetPlayerMatchesInQueueReturnsMatches()
        {
            long playerId = 1234;
            List<InQueueDemo> matches;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueDBInterface(context);
                test.Add(1, default(DateTime), Source.Unknown, playerId);
                test.Add(2, default(DateTime), Source.Unknown, playerId);
                test.Add(3, default(DateTime), Source.Unknown, playerId);

                matches = test.GetPlayerMatchesInQueue(playerId);
            }

            Assert.AreEqual(3, matches.Count);
            foreach (var match in matches)
                Assert.AreEqual(match.UploaderId, playerId);
        }


        [TestMethod]
        public void GetPlayerMatchesInQueueReturnsEmptyListWithUnknownPlayer()
        {
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueDBInterface(context);
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
                var test = new InQueueDBInterface(context);

                int queueLength = test.GetTotalQueueLength();
                Assert.AreEqual(0, queueLength);

                test.Add(1, default(DateTime), Source.Unknown, playerId);
                test.Add(2, default(DateTime), Source.Unknown, playerId);
                test.Add(3, default(DateTime), Source.Unknown, playerId);

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
                var test = new InQueueDBInterface(context);
                test.Add(matchId, default(DateTime), Source.Unknown, 1234);
                test.RemoveDemoFromQueue(matchId);
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
                var test = new InQueueDBInterface(context);
                Assert.ThrowsException<InvalidOperationException>(() => test.RemoveDemoFromQueue(matchId));
            }
        }

        [TestMethod]
        public void GetQueuePositionReturnsCorrectPosition()
        {
            long playerId1 = 1;
            long playerId2 = 2;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueDBInterface(context);

                test.Add(1, default(DateTime), Source.Unknown, playerId1);
                test.Add(2, default(DateTime), Source.Unknown, playerId1);
                test.Add(3, default(DateTime), Source.Unknown, playerId2);
                test.Add(4, default(DateTime), Source.Unknown, playerId2);

                for (int i = 1; i < 5; i++)
                    Assert.AreEqual(i - 1, test.GetQueuePosition(i));
            }
        }

        [TestMethod]
        public void IncrementRetryIncreasesRetryCount()
        {
            long matchId = 1;
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new InQueueDBInterface(context);
                test.Add(matchId, default(DateTime), Source.Unknown, 1234);

                for (int retry = 0; retry < 3; retry++)
                {
                    Assert.AreEqual(retry, test.IncrementRetry(matchId));
                }
            }
        }

        private InQueueDemo GetDemoByMatchId(DemoCentralContext context, long matchId)
        {
            return context.InQueueDemo.Where(x => x.MatchId == matchId).SingleOrDefault();
        }
    }
}
