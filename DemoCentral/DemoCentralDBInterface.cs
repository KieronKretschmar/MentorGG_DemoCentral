using DataBase.DatabaseClasses;
using RabbitTransfer.Enums;
using RabbitTransfer.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DataBase.Enumerals;

namespace DemoCentral
{
    public interface IDemoCentralDBInterface
    {
        void AddFilePath(long matchId, string zippedFilePath);
        DC2DFWModel CreateDemoFileWorkerModel(long matchId);
        List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0);
        List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0);
        bool IsDuplicateHash(string hash);
        string SetDownloadRetryingAndGetDownloadPath(long matchId);
        void SetFileStatusDownloaded(long matchId, bool success);
        void SetFileStatusZipped(long matchId, bool success);
        void SetUploadStatus(long matchId, bool success);
        bool TryCreateNewDemoEntryFromGatherer(long matchId, GathererTransferModel model);
        void UpdateHash(long matchId, string hash);
    }

    public class DemoCentralDBInterface : IDemoCentralDBInterface
    {
        private readonly DemoCentralContext _context;
        private readonly IInQueueDBInterface _inQueueDBInterface;

        public DemoCentralDBInterface(DemoCentralContext context, IInQueueDBInterface inQueueDBInterface)
        {
            _context = context;
            _inQueueDBInterface = inQueueDBInterface;
        }

        public void UpdateHash(long matchId, string hash)
        {
            var demo = GetDemoById(matchId);
            demo.Md5hash = hash;
            _context.SaveChanges();
        }

        public DC2DFWModel CreateDemoFileWorkerModel(long matchId)
        {
            var demo = GetDemoById(matchId);

            var model = new DC2DFWModel
            {
                Event = demo.Event,
                Source = (Source) demo.Source,
                MatchDate = demo.MatchDate,
                ZippedFilePath = demo.FilePath
            };

            return model;
        }

        public List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0)
        {
            List<long> recentMatchesId = _context.Demo.Where(x => x.UploaderId == playerId).Select(x => x.MatchId).Take(recentMatches + offset).ToList();
            recentMatchesId.RemoveRange(0, offset);

            return recentMatchesId;
        }

        public void SetFileStatusZipped(long matchId, bool success)
        {
            var demo = GetDemoById(matchId);
            demo.FileStatus = success ? (byte) FileStatus.UNZIPPED : (byte) FileStatus.UNZIPFAILED;
            _context.SaveChanges();
        }

        public void SetFileStatusDownloaded(long matchId, bool success)
        {
            var demo = GetDemoById(matchId);
            demo.FileStatus = success ? (byte) FileStatus.DOWNLOADED : (byte) FileStatus.DOWNLOADFAILED;
            _context.SaveChanges();
        }

        public void AddFilePath(long matchId, string zippedFilePath)
        {
            var demo = GetDemoById(matchId);
            demo.FilePath = zippedFilePath;
            _context.SaveChanges();
        }

        internal void RemoveDemo(long matchId)
        {
            var demo = GetDemoById(matchId);
            _context.Demo.Remove(demo);
            _context.SaveChanges();
        }

        public void SetUploadStatus(long matchId, bool success)
        {
            var demo = GetDemoById(matchId);
            demo.UploadStatus = success ? (byte) UploadStatus.FINISHED : (byte) UploadStatus.FAILED;
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
            var demo = GetDemoById(matchId);

            demo.FileStatus = (byte) FileStatus.DOWNLOAD_RETRYING;
            string downloadUrl = demo.DownloadUrl;
            _context.SaveChanges();

            return downloadUrl;
        }

        public bool IsDuplicateHash(string hash)
        {
            var demo = _context.Demo.Where(x => x.Md5hash == hash).SingleOrDefault();

            return !(demo == null);
        }


        public bool TryCreateNewDemoEntryFromGatherer(long matchId, GathererTransferModel model)
        {
            //checkdownloadurl
            var demo = _context.Demo.Where(x => x.DownloadUrl.Equals(model.DownloadUrl)).SingleOrDefault();
            if (demo != null)
                return false;

            _context.Demo.Add(new Demo
            {
                DownloadUrl = model.DownloadUrl,
                FileStatus = (byte) FileStatus.NEW,
                UploadDate = DateTime.UtcNow,
                UploadType = (byte) model.UploadType,
                MatchDate = model.MatchDate,
                Source = (byte) model.Source,
                DemoAnalyzerVersion = "",
                UploaderId = model.UploaderId,
            });

            _context.SaveChanges();

            _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);

            return true;
        }

        private Demo GetDemoById(long matchId)
        {
            return _context.Demo.Where(x => x.MatchId == matchId).Single();
        }
    }

}

