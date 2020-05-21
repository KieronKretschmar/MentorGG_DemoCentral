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
        DemoAnalyzeInstruction CreateAnalyzeInstructions(long matchId);
        DemoAnalyzeInstruction CreateAnalyzeInstructions(Demo demo);
        Demo GetDemoById(long matchId);
        /// <summary>
        /// Returns the player matches in queue , empty list if none found
        /// </summary>
        List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0);
        List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0);
        bool IsReanalysisRequired(string hash, out long matchId, AnalyzerQuality requestedQuality);
        void RemoveDemo(Demo demo);
        void RemoveDemo(long matchId);
        void SetDatabaseVersion(Demo demo, string databaseVersion);
        void SetDatabaseVersion(long matchId, string databaseVersion);
        List<Demo> GetMatchesByUploader(long steamId);
        void SetBlobUrl(Demo demo, string blobUrl);
        void SetBlobUrl(long matchId, string blobUrl);
        void SetFileStatus(Demo demo, FileStatus status);
        void SetFileStatus(long matchId, FileStatus status);
        void SetFileWorkerStatus(Demo demo, DemoFileWorkerStatus status);
        void SetFileWorkerStatus(long matchId, DemoFileWorkerStatus status);
        void SetFrames(Demo demo, int framesPerSecond);
        void SetFrames(long matchId, int framesPerSecond);
        void SetHash(Demo demo, string hash);
        void SetHash(long matchId, string hash);
        void SetUploadStatus(Demo demo, bool success);
        void SetUploadStatus(long matchId, bool success);
        /// <summary>
        /// try to create a new entry in the demo table. Returns false and the matchId of the match, if the downloadUrl is already known, return true otherwise
        /// </summary>
        /// <param name="matchId">Return either a new matchId or the one of the found demo if the download url is known</param>
        /// <returns>true, if downloadUrl is unique</returns>
        bool TryCreateNewDemoEntryFromGatherer(DemoInsertInstruction model, AnalyzerQuality requestedQuality, out long matchId);

        /// <summary>
        /// Creates a new entry in the demo table. Returns the matchId of the newly created match.
        /// </summary>
        /// <returns>MatchId of the newly created match</returns>
        long CreateNewDemoEntryFromManualUpload(ManualDownloadReport model, AnalyzerQuality requestedQuality);
        List<Demo> GetRecentFailedMatches(long playerId, int recentMatches, int offset = 0);
        DemoDownloadInstruction CreateDownloadInstructions(Demo dbDemo);
        List<Demo> GetUnfinishedDemos(DateTime minUploadDate);
        bool ResetAnalysis(long matchId);
    }

    /// <summary>
    /// Basic implementation of the <see cref="IDemoCentralDBInterface"/>
    /// </summary>
    public class DemoCentralDBInterface : IDemoCentralDBInterface
    {
        private readonly DemoCentralContext _context;
        private readonly ILogger<DemoCentralDBInterface> _logger;

        public DemoCentralDBInterface(DemoCentralContext context, ILogger<DemoCentralDBInterface> logger)
        {
            _context = context;
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
                string critical = $"Requested hash update for non-existing demo [ {matchId} ]\n " +
                    $"One should have been created by DemoCentral on first receiving the demo from the Gatherer";
                _logger.LogCritical(critical);
                throw new InvalidOperationException(critical, e);
            }

            SetHash(demo, hash);
        }

        public void SetHash(Demo demo, string hash)
        {
            demo.Md5hash = hash;
            _context.SaveChanges();
        }

        public DemoAnalyzeInstruction CreateAnalyzeInstructions(long matchId)
        {
            var demo = GetDemoById(matchId);

            return CreateAnalyzeInstructions(demo);
        }

        public DemoAnalyzeInstruction CreateAnalyzeInstructions(Demo demo)
        {
            return new DemoAnalyzeInstruction
            {
                MatchId = demo.MatchId,
                Source = demo.Source,
                MatchDate = demo.MatchDate,
                BlobUrl = demo.BlobUrl,
                FramesPerSecond = demo.FramesPerSecond,
                Quality = demo.Quality,
            };
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
            SetFileStatus(demo, status);
        }

        public void SetFileStatus(Demo demo, FileStatus status)
        {
            demo.FileStatus = status;
            _context.SaveChanges();
        }

        public void SetBlobUrl(long matchId, string blobUrl)
        {
            var demo = GetDemoById(matchId);
            SetBlobUrl(demo, blobUrl);
        }

        public void SetBlobUrl(Demo demo, string blobUrl)
        {
            demo.BlobUrl = blobUrl;
            _context.SaveChanges();
        }

        public void RemoveDemo(long matchId)
        {
            var demo = GetDemoById(matchId);
            RemoveDemo(demo);
        }

        public void RemoveDemo(Demo demo)
        {
            _context.Demo.Remove(demo);
            _context.SaveChanges();
        }

        public void SetUploadStatus(long matchId, bool success)
        {
            var demo = GetDemoById(matchId);
            SetUploadStatus(demo, success);

        }

        public void SetUploadStatus(Demo demo, bool success)
        {
            demo.UploadStatus = success ? UploadStatus.Finished : UploadStatus.Failed;
            _context.SaveChanges();
        }

        public List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0)
        {
            var recentMatchesId = _context.Demo.Where(x => x.UploaderId == playerId).Take(recentMatches + offset).ToList();
            recentMatchesId.RemoveRange(0, offset);

            return recentMatchesId;
        }

        public List<Demo> GetRecentFailedMatches(long playerId, int recentMatches, int offset = 0)
        {
            var recentMatchesId = _context.Demo
                .Where(x => x.UploaderId == playerId)
                .Where(x => FileStatusCollections.Failed.Contains(x.FileStatus) || DemoFileWorkerStatusCollections.Failed.Contains(x.DemoFileWorkerStatus))
                .Take(recentMatches + offset)
                .ToList();
            recentMatchesId.RemoveRange(0, offset);

            return recentMatchesId;
        }

        /// <summary>
        /// Checks if a hash is already in the database, and analyzed with more frames than the requested amount \n
        /// if so the out parameter is the matchId of the original demo, else -1
        /// </summary>
        /// <param name="matchId">id of the original match or -1 if hash is unique</param>
        public bool IsReanalysisRequired(string hash, out long matchId, AnalyzerQuality requestedQuality)
        {
            var demo = _context.Demo.Where(x => x.Md5hash.Equals(hash)).SingleOrDefault();

            matchId = demo == null ? -1 : demo.MatchId;

            return demo == null || requestedQuality > demo.Quality;
        }


        public bool TryCreateNewDemoEntryFromGatherer(DemoInsertInstruction model, AnalyzerQuality requestedQuality, out long matchId)
        {
            //checkdownloadurl
            var demo = _context.Demo.SingleOrDefault(x => x.DownloadUrl.Equals(model.DownloadUrl));
            if (demo != null)
            {
                matchId = demo.MatchId;
                //Check whether a new entry has to be created as the new entry
                //would have a higher analyzer quality than the old one
                if (requestedQuality <= demo.Quality)
                    return false;

                if (demo.HasFailedAnalysis())
                    return false;

                _logger.LogInformation($"Selected Demo [ {demo.MatchId} ] for re-analysis due to higher quality. Current quality [ {demo.Quality} ], requested quality [ {requestedQuality} ].");

                demo.Quality = requestedQuality;
                demo.FramesPerSecond = FramesPerQuality.Frames[requestedQuality];
                _context.SaveChanges();

                // Debug issue https://gitlab.com/mentorgg/csgo/democentral/-/issues/34
                var demoFreshFromDb = _context.Demo.SingleOrDefault(x => x.MatchId == demo.MatchId);
                if(demoFreshFromDb.Quality != requestedQuality)
                {
                    _logger.LogError($"Debug Issue #34: Quality does not match. Quality from database [ {demoFreshFromDb.Quality} ], expected [ {requestedQuality} ].");
                }

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

        public long CreateNewDemoEntryFromManualUpload(ManualDownloadReport model, AnalyzerQuality requestedQuality)
        {
            var demo = Demo.FromManualUploadTransferModel(model);
            demo.Quality = requestedQuality;
            demo.FramesPerSecond = FramesPerQuality.Frames[requestedQuality];

            _context.Demo.Add(demo);

            _context.SaveChanges();

            return demo.MatchId;
        }

        public Demo GetDemoById(long matchId)
        {
            return _context.Demo.Single(x => x.MatchId == matchId);
        }

        public void SetDatabaseVersion(long matchId, string databaseVersion)
        {
            var demo = GetDemoById(matchId);
            SetDatabaseVersion(demo, databaseVersion);
        }

        public void SetDatabaseVersion(Demo demo, string databaseVersion)
        {
            demo.DatabaseVersion = databaseVersion;
            _context.SaveChanges();
        }

        public void SetFrames(long matchId, int framesPerSecond)
        {
            Demo demo = GetDemoById(matchId);

            SetFrames(demo, framesPerSecond);
        }

        public void SetFrames(Demo demo, int framesPerSecond)
        {
            demo.FramesPerSecond = (byte) framesPerSecond;
            _context.SaveChanges();
        }

        public void SetFileWorkerStatus(long matchId, DemoFileWorkerStatus status)
        {
            Demo demo = GetDemoById(matchId);
            SetFileWorkerStatus(demo, status);
        }

        public void SetFileWorkerStatus(Demo demo, DemoFileWorkerStatus status)
        {
            demo.DemoFileWorkerStatus = status;
            _context.SaveChanges();
        }

        public List<Demo> GetMatchesByUploader(long steamId)
        {
            return _context.Demo.Where(x => x.UploaderId == steamId).ToList();
        }

        public DemoDownloadInstruction CreateDownloadInstructions(Demo dbDemo)
        {
            var res = new DemoDownloadInstruction
            {
                DownloadUrl = dbDemo.DownloadUrl,
                MatchId = dbDemo.MatchId,
            };
            
            return res;
        }

        /// <summary>
        /// Returns IDs of demos for which the file is in BlobStorage but that were not inserted into MatchDb.
        /// </summary>
        /// <param name="minUploadDate"></param>
        /// <returns></returns>
        public List<Demo> GetUnfinishedDemos(DateTime minUploadDate)
        {
            var demosToReset = _context.Demo
                .Where(x => x.UploadDate >= minUploadDate)
                .Where(x=>x.FileStatus == FileStatus.InBlobStorage)
                .Where(x=>x.UploadStatus != UploadStatus.Finished)
                .ToList();

            return demosToReset;
        }

        /// <summary>
        /// Turns the match to it's state before it was sent to DemoFileWorker
        /// </summary>
        /// <param name="matchId"></param>
        /// <returns></returns>
        public bool ResetAnalysis(long matchId)
        {
            var demo = _context.Demo.SingleOrDefault(x => x.MatchId == matchId);
            if(demo == null)
            {
                _logger.LogError($"Tried to reset demo [ {matchId} ] but it was not in database.");
                return false;
            }

            demo.ToPreAnalysisState();
            _context.SaveChanges();
            return true;
        }
    }

}

