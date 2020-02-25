using DataBase.DatabaseClasses;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DataBase.Enumerals;
using Microsoft.Extensions.Logging;
using Database.Enumerals;
using DemoCentral.Enumerals;
using RabbitCommunicationLib.Enums;

namespace DemoCentral
{
    /// <summary>
    /// Interface for the Demo table of the database
    /// </summary>
    public interface IDemoCentralDBInterface
    {
        void SetFilePath(long matchId, string zippedFilePath);
        DemoAnalyzeInstructions CreateAnalyzeInstructions(long matchId);
        /// <summary>
        /// Returns the player matches in queue , empty list if none found
        /// </summary>
        List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0);
        List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0);
        bool ReAnalysisRequired(string hash, out long matchId, byte framesPerSecond = 1);
        void RemoveDemo(long matchId);
        string SetDownloadRetryingAndGetDownloadPath(long matchId);
        void SetFileStatus(long matchId, FileStatus status);
        void SetUploadStatus(long matchId, bool success);
        void SetDatabaseVersion(long matchId, string databaseVersion);

        /// <summary>
        /// try to create a new entry in the demo table. Returns false and the matchId of the match, if the downloadUrl is already known, return true otherwise
        /// </summary>
        /// <param name="matchId">Return either a new matchId or the one of the found demo if the download url is known</param>
        /// <returns>true, if downloadUrl is unique</returns>
        bool TryCreateNewDemoEntryFromGatherer(DemoEntryInstructions model, AnalyzerQuality requestedQuality, out long matchId);
        void SetHash(long matchId, string hash);
        void SetFrames(long matchId, int framesPerSecond);
        void SetDemoFileWorkerStatus(long matchId, bool v);
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

        public DemoAnalyzeInstructions CreateAnalyzeInstructions(long matchId)
        {
            var demo = GetDemoById(matchId);

            var model = new DemoAnalyzeInstructions
            {
                Source = demo.Source,
                MatchDate = demo.MatchDate,
                BlobURI = demo.FilePath,
                FramesPerSecond = demo.FramesPerSecond,
                Quality = demo.Quality,
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
        /// Checks if a hash is already in the database, and analyzed with more frames than the requested amount \n
        /// if so the out parameter is the matchId of the original demo, else -1
        /// </summary>
        /// <param name="matchId">id of the original match or -1 if hash is unique</param>
        public bool ReAnalysisRequired(string hash, out long matchId, byte framesPerSecond = 1)
        {
            var demo = _context.Demo.Where(x => x.Md5hash.Equals(hash)).SingleOrDefault();

            matchId = demo == null ? -1 : demo.MatchId;

            return !(demo == null) && demo.FramesPerSecond > framesPerSecond;
        }


        public bool TryCreateNewDemoEntryFromGatherer(DemoEntryInstructions model, AnalyzerQuality requestedQuality, out long matchId)
        {
            //checkdownloadurl
            var demo = _context.Demo.Where(x => x.DownloadUrl.Equals(model.DownloadUrl)).SingleOrDefault();
            if (demo != null)
            {
                matchId = demo.MatchId;
                //Check whether a new entry has to be created as the new entry
                //would have a higher analyzer quality than the old one
                if (!(requestedQuality > demo.Quality))
                    return false;

                demo.Quality = requestedQuality;
                demo.FramesPerSecond = FramesPerQuality.Frames[requestedQuality];
                _context.SaveChanges();

                return true;
            }

            demo = Demo.FromGatherTransferModel(model);
            demo.Quality = requestedQuality;
            demo.FramesPerSecond = FramesPerQuality.Frames[requestedQuality];

            _context.Demo.Add(demo);

            _context.SaveChanges();

            matchId = demo.MatchId;

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

            demo.FramesPerSecond = (byte) framesPerSecond;
            _context.SaveChanges();
        }

        public void SetDemoFileWorkerStatus(long matchId, bool success)
        {
            Demo demo = GetDemoById(matchId);
            demo.DemoFileWorkerStatus = success? DemoFileWorkerStatus.Finished: DemoFileWorkerStatus.Failed;
            _context.SaveChanges();
        }
    }

}

