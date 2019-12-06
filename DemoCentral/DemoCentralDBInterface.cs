using DemoCentral.DatabaseClasses;
using DemoCentral.Enumerals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DemoCentral
{
    public class DemoCentralDBInterface
    {
        public static List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0)
        {
            List<long> recentMatchesId;
            using (var context = new democentralContext())
            {
                var res = context.Demo.Where(x => x.UploaderId == playerId).Take(recentMatches + offset).ToList();

                res.RemoveRange(0, offset);
                recentMatchesId = res.Select(x => x.MatchId).ToList();
            }

            return recentMatchesId;
        }

        public static void AddFilePath(long matchId, string zippedFilePath)
        {
            using (var context = new democentralContext())
            {
                context.Demo.Where(x => x.MatchId == matchId).Single().FilePath = zippedFilePath;
                context.SaveChanges();
            }
        }

        internal static void RemoveDemo(long matchId)
        {
            throw new NotImplementedException();
        }

        public static void UpdateUploadStatus(long matchId, bool success)
        {
            using (var context = new democentralContext())
            {
                context.Demo.Where(x => x.MatchId == matchId).Single().UploadStatus = success ? (byte)UploadStatus.FINISHED : (byte)UploadStatus.FAILED;
                context.SaveChanges();
            }
        }

        public static void UpdateDownloadStatus(long matchId, bool success)
        {
            using (var context = new democentralContext())
            {
                context.Demo.Where(x => x.MatchId == matchId).Single().FileStatus = success ? (byte)FileStatus.DOWNLOADED : (byte)FileStatus.DOWNLOADFAILED;
                context.SaveChanges();
            }
        }

        public static List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0)
        {
            List<Demo> recentMatchesId;
            using (var context = new democentralContext())
            {
                var res = context.Demo.Where(x => x.UploaderId == playerId).Take(recentMatches + offset).ToList();
                res.RemoveRange(0, offset);

                recentMatchesId = res;
            }

            return recentMatchesId;
        }

        public static string SetDownloadRetryingAndGetDownloadPath(long matchId)
        {
            string downloadUrl;

            using (var context = new democentralContext())
            {
                var demo = context.Demo.Where(x => x.MatchId == matchId).Single();

                demo.FileStatus = (byte)FileStatus.RETRYING;
                downloadUrl = demo.DownloadUrl;
                context.SaveChanges();
            }


            return downloadUrl;
        }

        public static bool CreateNewDemoEntryFromGatherer(GathererTransferModel model)
        {
            using (var context = new democentralContext())
            {
                //checkdownloadurl
                var demo = context.Demo.Where(x => x.DownloadUrl.Equals(model.DownloadUrl)).SingleOrDefault();
                if (demo != null) return false;

                context.Demo.Add(new Demo
                {
                    DownloadUrl = model.DownloadUrl,
                    FileStatus = (byte)FileStatus.NEW,
                    UploadDate = DateTime.Now,
                    UploadType = model.UploadType,
                    MatchDate = model.MatchDate,
                    Source = model.Source,
                    DemoAnalyzerVersion = "",
                    UploaderId = model.UploaderId,
                });

                Console.WriteLine("New DemoEntry created");
                context.SaveChanges();

                QueueTracker.Add(model.matchId, model.MatchDate, model.Source, model.UploaderId);

                return true;
            }
        }
    }

}

