using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitTransfer.TransferModels;
using DemoCentral;
using Moq;
using Microsoft.EntityFrameworkCore;
using DataBase.DatabaseClasses;
using System.Linq;
using System;
using RabbitTransfer.Enums;
using DataBase.Enumerals;
using System.Collections.Generic;

namespace DemoCentralTests
{
    [TestClass]
    public class DemoCentralDBInterfaceTests
    {
        private DbContextOptions<DemoCentralContext> _test_config;
        private IInQueueDBInterface _mockInQueueDb;
        private Demo _standardDemo;

        public DemoCentralDBInterfaceTests()
        {
            _test_config = DCTestsDBHelper.test_config;
            _mockInQueueDb = new Mock<IInQueueDBInterface>().Object;

            _standardDemo = new Demo
            {
                DownloadUrl = "xyz",
                FileStatus = (byte) FileStatus.NEW,
                UploadDate = DateTime.UtcNow,
                UploadType = UploadType.Unknown,
                MatchDate = default(DateTime),
                Source = Source.Unknown,
                DemoAnalyzerVersion = "",
                UploaderId = 1234,
            };
        }


        [TestCleanup]
        public void DropDataBase()
        {
            using (var context = new DemoCentralContext(_test_config))
                context.Database.EnsureDeleted();
        }

        [TestMethod]
        public void TryCreateNewDemoEntryFromGathererCreatesAEntryForNewDemo()
        {
            long matchId;

            GathererTransferModel model = new GathererTransferModel
            {
                MatchDate = default(DateTime),
                DownloadUrl = "1234",
                Source = Source.Unknown,
                UploaderId = 1234,
                UploadType = UploadType.Unknown,
            };

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);

                test.TryCreateNewDemoEntryFromGatherer(model, out matchId);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                Demo demo = GetDemoByMatchId(matchId, context);
                Assert.IsNotNull(demo);

                Assert.AreEqual(model.MatchDate, demo.MatchDate);
                Assert.AreEqual(model.UploadType, demo.UploadType);
                Assert.AreEqual(model.UploaderId, demo.UploaderId);
                Assert.AreEqual(model.DownloadUrl, demo.DownloadUrl);
                Assert.AreEqual(model.Source, demo.Source);
            }
        }

        [TestMethod]
        public void TryCreateNewDemoEntryFromGathererAddsDemoToQueueTable()
        {
            long matchId;
            Mock<IInQueueDBInterface> mockInQueueDB = new Mock<IInQueueDBInterface>();
            var mockedObject = mockInQueueDB.Object;
            var matchDate = default(DateTime);
            var downloadUrl = "xyz";
            var uploaderId = 1234;

            GathererTransferModel model = new GathererTransferModel
            {
                MatchDate = matchDate,
                DownloadUrl = downloadUrl,
                UploaderId = uploaderId,
                Source = Source.Unknown,
                UploadType = UploadType.Unknown,
            };

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, mockedObject);

                test.TryCreateNewDemoEntryFromGatherer(model, out matchId);
            }

            mockInQueueDB.Verify(mockedObject => mockedObject.Add(matchId, matchDate, Source.Unknown, uploaderId), Times.Once());
        }

        [TestMethod]
        public void TryCreateNewDemoEntryFromGathererReturnsFalseOnKnownDemo()
        {

            long matchId;
            bool success;

            Mock<IInQueueDBInterface> mockInQueueDB = new Mock<IInQueueDBInterface>();
            var mockedObject = mockInQueueDB.Object;
            var matchDate = default(DateTime);
            var downloadUrl = "xyz";
            var uploaderId = 1234;

            GathererTransferModel model = new GathererTransferModel
            {
                MatchDate = matchDate,
                DownloadUrl = downloadUrl,
                UploaderId = uploaderId,
                Source = Source.Unknown,
                UploadType = UploadType.Unknown,
            };

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, mockedObject);

                test.TryCreateNewDemoEntryFromGatherer(model, out matchId);
                success = test.TryCreateNewDemoEntryFromGatherer(model, out matchId);
            }

            Assert.IsFalse(success);
            Assert.AreEqual(-1, matchId);
        }

        [TestMethod]
        public void UpdateHashSetsNewHash()
        {
            long matchId;
            Demo demo = CopyDemo(_standardDemo);
            var new_hash = "new_hash";

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                matchId = demo.MatchId;
                test.UpdateHash(matchId, new_hash);
            }

            Assert.AreEqual(new_hash, demo.Md5hash);
        }

        [TestMethod]
        public void UpdateHashFailsWithUnknownMatch()
        {
            long unknown_matchId = -1;
            var new_hash = "new_hash";
            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);

                Assert.ThrowsException<InvalidOperationException>(() => test.UpdateHash(unknown_matchId, new_hash));
            }
        }


        [TestMethod]
        public void CreateDemoFileWorkerModelReturnFunctioningModel()
        {

            long matchId;
            DC2DFWModel assertModel;
            Demo demo = CopyDemo(_standardDemo);
            demo.FilePath = "abc";
            demo.Event = "TESTING";
            demo.Source = Source.ManualUpload;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                matchId = demo.MatchId;
                assertModel = test.CreateDemoFileWorkerModel(matchId);
            }

            Assert.AreEqual(demo.FilePath, assertModel.ZippedFilePath);
            Assert.AreEqual(demo.Event, assertModel.Event);
            Assert.AreEqual(demo.Source, assertModel.Source);
        }

        [TestMethod]
        public void GetRecentMatchesReturnsListOfValidMatches()
        {
            List<Demo> matches;
            Demo demo1 = CopyDemo(_standardDemo);
            Demo demo2 = CopyDemo(demo1);
            Demo demo3 = CopyDemo(demo1);
            long playerId = demo1.UploaderId;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo1, context);
                AddDemoToDB(demo2, context);
                AddDemoToDB(demo3, context);

                matches = test.GetRecentMatches(playerId, 3);
            }

            List<Demo> expected = new List<Demo> { demo1, demo2, demo3 };
            CollectionAssert.AllItemsAreUnique(matches);
            Assert.AreEqual(3, matches.Count);

            //Checks if the same elements are contained, regardless of the order
            CollectionAssert.AreEquivalent(expected, matches);
        }


        [TestMethod]
        public void GetRecentMatchesSkipsOffset()
        {
            List<Demo> matches;
            Demo demo1 = CopyDemo(_standardDemo);
            Demo demo2 = CopyDemo(demo1);
            Demo demo3 = CopyDemo(demo1);
            long playerId = demo1.UploaderId;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo1, context);
                AddDemoToDB(demo2, context);
                AddDemoToDB(demo3, context);

                matches = test.GetRecentMatches(playerId, 2, 1);
            }

            List<Demo> expected = new List<Demo> { demo2, demo3 };
            CollectionAssert.AllItemsAreUnique(matches);
            Assert.AreEqual(2, matches.Count);

            //Checks if the same elements are contained, regardless of the order
            CollectionAssert.AreEquivalent(expected, matches);
        }

        [TestMethod]
        public void GetRecentMatchesDoesNotFailIfRequestingMoreMatchesThanExist()
        {
            List<Demo> matches;
            Demo demo1 = CopyDemo(_standardDemo);
            Demo demo2 = CopyDemo(demo1);
            Demo demo3 = CopyDemo(demo1);
            long playerId = demo1.UploaderId;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo1, context);
                AddDemoToDB(demo2, context);
                AddDemoToDB(demo3, context);

                //implicitly asserts that no exception is thrown
                //if one is, the unit test fails 
                matches = test.GetRecentMatches(playerId, 2, 2);
            }

            List<Demo> expected = new List<Demo> { demo3 };
            Assert.AreEqual(1, matches.Count);

            //Checks if the same elements are contained, regardless of the order
            CollectionAssert.AreEquivalent(expected, matches);
        }


        [TestMethod]
        public void SetFileStatusZippedSetsCorrectStatus()
        {
            Demo demo = CopyDemo(_standardDemo);

            using (var context = new DemoCentralContext(_test_config))
            {
                DemoCentralDBInterface test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                test.SetFileStatusZipped(demo.MatchId, true);
            }

            Assert.IsTrue(demo.FileStatus == FileStatus.UNZIPPED);
        }

        [TestMethod]
        public void SetFileStatusDownloadedSetsCorrectStatus()
        {
            Demo demo = CopyDemo(_standardDemo);

            using (var context = new DemoCentralContext(_test_config))
            {
                DemoCentralDBInterface test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                test.SetFileStatusDownloaded(demo.MatchId, true);
            }

            Assert.IsTrue(demo.FileStatus == FileStatus.DOWNLOADED);
        }

        [TestMethod]
        public void AddFilePathSetsPath()
        {

            Demo demo = CopyDemo(_standardDemo);
            string test_path = "test_file_path";

            using (var context = new DemoCentralContext(_test_config))
            {
                DemoCentralDBInterface test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                test.AddFilePath(demo.MatchId, test_path);
            }

            Assert.AreEqual(test_path, demo.FilePath);
        }

        [TestMethod]
        public void RemoveDemoRemovesDemo()
        {

            Demo demo = CopyDemo(_standardDemo);

            using (var context = new DemoCentralContext(_test_config))
            {
                DemoCentralDBInterface test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);
                test.RemoveDemo(demo.MatchId);
            }

            using (var context = new DemoCentralContext(_test_config))
            {
                var deleted_demo = GetDemoByMatchId(demo.MatchId, context);
                Assert.IsNull(deleted_demo);
            }
        }

        [TestMethod]
        public void SetUploadStatusSetsCorrectStatus()
        {
            Demo demo = CopyDemo(_standardDemo);

            using (var context = new DemoCentralContext(_test_config))
            {
                DemoCentralDBInterface test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                test.SetUploadStatus(demo.MatchId, true);
            }

            Assert.IsTrue(demo.UploadStatus == UploadStatus.FINISHED);
        }

        [TestMethod]
        public void GetRecentMatchIdsReturnsListOfValidMatchIds()
        {
            List<long> matches;
            Demo demo1 = CopyDemo(_standardDemo);
            Demo demo2 = CopyDemo(demo1);
            Demo demo3 = CopyDemo(demo1);
            long playerId = demo1.UploaderId;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo1, context);
                AddDemoToDB(demo2, context);
                AddDemoToDB(demo3, context);

                matches = test.GetRecentMatchIds(playerId, 3);
            }

            List<long> expected = new List<Demo> { demo1, demo2, demo3 }.Select(x => x.MatchId).ToList();
            CollectionAssert.AllItemsAreUnique(matches);
            Assert.AreEqual(3, matches.Count);

            //Checks if the same elements are contained, regardless of the order
            CollectionAssert.AreEquivalent(expected, matches);
        }

        [TestMethod]
        public void GetRecentMatchIdsSkipsOffset()
        {
            List<long> matches;
            Demo demo1 = CopyDemo(_standardDemo);
            Demo demo2 = CopyDemo(demo1);
            Demo demo3 = CopyDemo(demo1);
            long playerId = demo1.UploaderId;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo1, context);
                AddDemoToDB(demo2, context);
                AddDemoToDB(demo3, context);

                matches = test.GetRecentMatchIds(playerId, 2, 1);
            }

            List<long> expected = new List<Demo> { demo2, demo3 }.Select(x => x.MatchId).ToList();
            CollectionAssert.AllItemsAreUnique(matches);
            Assert.AreEqual(2, matches.Count);

            //Checks if the same elements are contained, regardless of the order
            CollectionAssert.AreEquivalent(expected, matches);
        }

        [TestMethod]
        public void GetRecentMatchIdsDoesNotFailsIfRequestingMoreMatchesThanExist()
        {

            List<long> matches;
            Demo demo1 = CopyDemo(_standardDemo);
            Demo demo2 = CopyDemo(demo1);
            Demo demo3 = CopyDemo(demo1);
            long playerId = demo1.UploaderId;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo1, context);
                AddDemoToDB(demo2, context);
                AddDemoToDB(demo3, context);

                //implicitly asserts that no exception is thrown
                //if one is, the unit test fails 
                matches = test.GetRecentMatchIds(playerId, 2, 2);
            }

            List<long> expected = new List<Demo> { demo3 }.Select(x => x.MatchId).ToList();
            Assert.AreEqual(1, matches.Count);

            //Checks if the same elements are contained, regardless of the order
            CollectionAssert.AreEquivalent(expected, matches);
        }

        [TestMethod]
        public void SetDownloadRetryingAndGetDownloadPathSetsDownloadRetrying()
        {
            Demo demo = CopyDemo(_standardDemo);

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);
                test.SetDownloadRetryingAndGetDownloadPath(demo.MatchId);
            }

            Assert.AreEqual(demo.FileStatus, FileStatus.DOWNLOAD_RETRYING);
        }


        [TestMethod]
        public void SetDownloadRetryingAndGetDownloadPathReturnsCorrectPath()
        {
            Demo demo = CopyDemo(_standardDemo);
            demo.DownloadUrl = "test_download_url";
            string returnPath;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);
                returnPath = test.SetDownloadRetryingAndGetDownloadPath(demo.MatchId);
            }

            Assert.AreEqual(demo.DownloadUrl, returnPath);

        }

        [TestMethod]
        public void IsDuplicateHashOutputsTrueForDuplicate()
        {
            Demo demo = CopyDemo(_standardDemo);
            bool isDuplicate;

            string duplicate_hash = "test_hash_duplicate";

            demo.Md5hash = duplicate_hash;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                isDuplicate = test.IsDuplicateHash(duplicate_hash);
            }

            Assert.IsTrue(isDuplicate);
        }


        [TestMethod]
        public void IsDuplicateHashOutputsFalseForNonDuplicate()
        {
            Demo demo = CopyDemo(_standardDemo);
            bool isDuplicate;

            string first_hash = "test_hash_non_duplicate_1";
            string second_hash = "test_hash_non_duplicate_2";


            demo.Md5hash = first_hash;

            using (var context = new DemoCentralContext(_test_config))
            {
                var test = new DemoCentralDBInterface(context, _mockInQueueDb);
                AddDemoToDB(demo, context);

                isDuplicate = test.IsDuplicateHash(second_hash);
            }

            Assert.AreNotEqual(first_hash, second_hash);
            Assert.IsFalse(isDuplicate);
        }

        private Demo GetDemoByMatchId(long matchId, DemoCentralContext context)
        {
            return context.Demo.Where(x => x.MatchId == matchId).SingleOrDefault();
        }

        private void AddDemoToDB(Demo demo, DemoCentralContext context)
        {
            context.Demo.Add(demo);
            context.SaveChanges();
        }

        private Demo CopyDemo(Demo demo1)
        {
            return new Demo
            {
                MatchId = demo1.MatchId,
                DownloadUrl = demo1.DownloadUrl,
                FileStatus = demo1.FileStatus,
                UploadDate = demo1.UploadDate,
                UploadType = demo1.UploadType,
                MatchDate = demo1.MatchDate,
                Source = demo1.Source,
                DemoAnalyzerVersion = demo1.DemoAnalyzerVersion,
                UploaderId = demo1.UploaderId,
            };
        }
    }
}


