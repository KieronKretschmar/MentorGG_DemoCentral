using DataBase.DatabaseClasses;
using RabbitTransfer.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DataBase.Enumerals;
using Microsoft.Extensions.Logging;
using Database.Enumerals;
using DemoCentral.Enumerals;

namespace DemoCentral
{
    /// <summary>
    /// Interface for the Demo table of the database
    /// </summary>
    public interface IDemoCentralDBInterface
    {
        void SetFilePath(long matchId, string zippedFilePath);
        DC2DFWModel CreateDemoFileWorkerModel(long matchId);
        /// <summary>
        /// Returns the player matches in queue , empty list if none found
        /// </summary>
        List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0);
        List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0);
        bool IsDuplicateHash(string hash, out long matchId, byte frames = 1);
        void RemoveDemo(long matchId);
        string SetDownloadRetryingAndGetDownloadPath(long matchId);
        void SetFileStatus(long matchId, FileStatus status);
        void SetUploadStatus(long matchId, bool success);
        void SetDatabaseVersion(long matchId, string databaseVersion);

        /// <summary>
        /// try to create a new entry in the demo table. Returns false and the matchId of the match, if the downloadUrl is already known. If not, the task is forwarded to downloader and true is returned.
        /// </summary>
        /// <param name="matchId">Return either a new matchId or the one of the found demo if the download url is known</param>
        /// <returns>true, if downloadUrl is unique</returns>
        bool TryCreateNewDemoEntryFromGatherer(GathererTransferModel model, AnalyzerQuality currentQuality, out long matchId);
        void SetHash(long matchId, string hash);
        void SetFrames(long matchId, int framesPerSecond);
    }

    /// <summary>
    /// Basic implementation of the <see cref="IDemoCentralDBInterface"/>
    /// </summary>
    public class DemoCentralDBInterface : IDemoCentralDBInterface
    {
        private readonly DemoCentralContext _context;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly ILogger<DemoCentralDBInterface> _logger;

        public DemoCentralDBInterface(DemoCentralContext context, IInQueueDBInterface inQueueDBInterface, ILogger<DemoCentralDBInterface> logger)
        {
            _context = context;
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }

        public void SetHash(long matchId, string hash)
        {
            Demo demo = null;

            try
            {
                demo = GetDemoById(matchId);
            }
            catch (InvalidOperationException e)
            {
                string critical = $"Requested hash update for non-existing demo#{matchId} \n " +
                    $"One should have been created by DemoCentral on first receiving the demo from the Gatherer";
                _logger.LogCritical(critical);
                throw new InvalidOperationException(critical, e);
            }

            demo.Md5hash = hash;
            _context.SaveChanges();
        }

        public DC2DFWModel CreateDemoFileWorkerModel(long matchId)
        {
            var demo = GetDemoById(matchId);

            var model = new DC2DFWModel
            {
                Event = demo.Event,
                Source = demo.Source,
                MatchDate = demo.MatchDate,
                ZippedFilePath = demo.FilePath,
                FramesPerSecond = demo.Frames,
            };

            return model;
        }

        public List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0)
        {
            List<long> recentMatchesId = _context.Demo.Where(x => x.UploaderId == playerId).Select(x => x.MatchId).Take(recentMatches + offset).ToList();
            recentMatchesId.RemoveRange(0, offset);

            return recentMatchesId;
        }

        public void SetFileStatus(long matchId, FileStatus status)
        {
            var demo = GetDemoById(matchId);
            demo.FileStatus = status;
            _context.SaveChanges();
        }

        public void SetFilePath(long matchId, string zippedFilePath)
        {
            var demo = GetDemoById(matchId);
            demo.FilePath = zippedFilePath;
            _context.SaveChanges();
        }

        public void RemoveDemo(long matchId)
        {
            var demo = GetDemoById(matchId);
            _context.Demo.Remove(demo);
            _context.SaveChanges();
        }

        public void SetUploadStatus(long matchId, bool success)
        {
            var demo = GetDemoById(matchId);
            demo.UploadStatus = success ? UploadStatus.FINISHED : UploadStatus.FAILED;
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

            demo.FileStatus = FileStatus.DOWNLOAD_RETRYING;
            string downloadUrl = demo.DownloadUrl;
            _context.SaveChanges();

            return downloadUrl;
        }

        /// <summary>
        /// Checks if a hash is already in the database, \n
        /// if so the out parameter is the matchId of the original demo, else -1
        /// </summary>
        /// <param name="matchId">id of the original match or -1 if hash is unique</param>
        public bool IsDuplicateHash(string hash, out long matchId, byte frames = 1)
        {
            var demo = _context.Demo.Where(x => x.Md5hash.Equals(hash)).SingleOrDefault();

            matchId = demo == null ? -1 : demo.MatchId;

            return !(demo == null) && demo.Frames > frames;
        }


        public bool TryCreateNewDemoEntryFromGatherer(GathererTransferModel model, AnalyzerQuality currentQuality, out long matchId)
        {
            //checkdownloadurl
            var demo = _context.Demo.Where(x => x.DownloadUrl.Equals(model.DownloadUrl)).SingleOrDefault();
            if (demo != null)
            {
                matchId = demo.MatchId;
                //Check whether a new entry has to be created as the new entry
                //would have a higher analyzer quality than the old one
                if (!(currentQuality > demo.Quality))
                    return false;

                demo.Quality = currentQuality;
                demo.Frames = FramesPerSubscription.Frames[currentQuality];
                _context.SaveChanges();

                _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);
                return true;
            }

            demo = Demo.FromGatherTransferModel(model);
            demo.Quality = currentQuality;
            demo.Frames = FramesPerSubscription.Frames[currentQuality];

            _context.Demo.Add(demo);

            _context.SaveChanges();

            matchId = demo.MatchId;
            _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);

            return true;
        }

        private Demo GetDemoById(long matchId)
        {
            return _context.Demo.Single(x => x.MatchId == matchId);
        }

        public void SetDatabaseVersion(long matchId, string databaseVersion)
        {
            var demo = GetDemoById(matchId);
            demo.DatabaseVersion = databaseVersion;
            _context.SaveChanges();
        }

        public void SetFrames(long matchId, int framesPerSecond)
        {
            Demo demo = GetDemoById(matchId);

            demo.Frames = (byte) framesPerSecond;
            _context.SaveChanges();
        }
    }

}

