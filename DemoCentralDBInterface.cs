using DemoCentral.DatabaseClasses;
using DemoCentral.Enumerals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DemoCentral
{
    public interface IDemoCentralDBInterface
    {
        void AddFilePath(long matchId, string zippedFilePath);
        bool CreateNewDemoEntryFromGatherer(GathererTransferModel model);
        List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0);
        List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0);
        string SetDownloadRetryingAndGetDownloadPath(long matchId);
        void UpdateDownloadStatus(long matchId, bool success);
        void UpdateUploadStatus(long matchId, bool success);
    }

    public class DemoCentralDBInterface : IDemoCentralDBInterface
    {
        private readonly DemoCentralContext _context;
        private readonly InQueueDBInterface _inQueueDBInterface;

        public DemoCentralDBInterface(DemoCentralContext context, InQueueDBInterface inQueueDBInterface)
        {
            _context = context;
            _inQueueDBInterface = inQueueDBInterface;
        }

        public void UpdateHash(long matchId, string hash)
        {
            var demo = _context.Demo.Where(x => x.MatchId == matchId).Single();
            demo.Md5hash = hash;
            _context.SaveChanges();
        }

        public List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0)
        {
            List<long> recentMatchesId;
            using (var context = new DemoCentralContext())
            {
                var res = context.Demo.Where(x => x.UploaderId == playerId).Take(recentMatches + offset).ToList();

                res.RemoveRange(0, offset);
                recentMatchesId = res.Select(x => x.MatchId).ToList();
            }

            return recentMatchesId;
        }

        public void AddFilePath(long matchId, string zippedFilePath)
        {
            _context.Demo.Where(x => x.MatchId == matchId).Single().FilePath = zippedFilePath;
            _context.SaveChanges();

        }

        internal void RemoveDemo(long matchId)
        {
            throw new NotImplementedException();
        }

        public void UpdateUploadStatus(long matchId, bool success)
        {

            _context.Demo.Where(x => x.MatchId == matchId).Single().UploadStatus = success ? (byte) UploadStatus.FINISHED : (byte) UploadStatus.FAILED;
            _context.SaveChanges();

        }

        public void UpdateDownloadStatus(long matchId, bool success)
        {
            _context.Demo.Where(x => x.MatchId == matchId).Single().FileStatus = success ? (byte) FileStatus.DOWNLOADED : (byte) FileStatus.DOWNLOADFAILED;
            _context.SaveChanges();

        }

        public List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0)
        {
            var recentMatchesId = _context.Demo.Where(x => x.UploaderId == playerId).Take(recentMatches + offset).ToList();
            recentMatchesId.RemoveRange(0, offset);

            return recentMatchesId;
        }

        public string SetDownloadRetryingAndGetDownloadPath(long matchId)
        {
            string downloadUrl;

            var demo = _context.Demo.Where(x => x.MatchId == matchId).Single();

            demo.FileStatus = (byte) FileStatus.RETRYING;
            downloadUrl = demo.DownloadUrl;
            _context.SaveChanges();



            return downloadUrl;
        }

        public bool IsDuplicateHash(string hash)
        {
            var demo = _context.Demo.Where(x => x.Md5hash == hash).SingleOrDefault();

            return !(demo == null);
        }


        public bool CreateNewDemoEntryFromGatherer(GathererTransferModel model)
        {
            //checkdownloadurl
            var demo = _context.Demo.Where(x => x.DownloadUrl.Equals(model.DownloadUrl)).SingleOrDefault();
            if (demo != null)
                return false;

            _context.Demo.Add(new Demo
            {
                DownloadUrl = model.DownloadUrl,
                FileStatus = (byte) FileStatus.NEW,
                UploadDate = DateTime.Now,
                UploadType = model.UploadType,
                MatchDate = model.MatchDate,
                Source = model.Source,
                DemoAnalyzerVersion = "",
                UploaderId = model.UploaderId,
            });

            _context.SaveChanges();

            _inQueueDBInterface.Add(model.matchId, model.MatchDate, model.Source, model.UploaderId);

            return true;

        }
    }

}

