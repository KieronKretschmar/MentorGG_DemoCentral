using Database.DatabaseClasses;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Database.Enumerals;
using Microsoft.Extensions.Logging;
using Database.Enumerals;
using DemoCentral.Enumerals;
using RabbitCommunicationLib.Enums;

namespace DemoCentral
{
    /// <summary>
    /// Interface for the Demo table of the database
    /// </summary>
    public interface IDemoTableInterface
    {
        Demo GetDemoById(long matchId);

        /// <summary>
        /// Returns the player matches in queue , empty list if none found
        /// </summary>
        List<Demo> GetRecentMatches(long playerId, int recentMatches, int offset = 0);
        List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0);

        bool IsReanalysisRequired(string hash, out long matchId, AnalyzerQuality requestedQuality);

        DemoAnalyzeInstruction CreateAnalyzeInstructions(Demo demo);

        List<Demo> GetMatchesByUploader(long steamId);

        void RemoveDemo(Demo demo);

        void SetBlobUrl(Demo demo, string blobUrl);

        void SetFileStatus(Demo demo, FileStatus status);

        void SetAnalyzeState(Demo demo, bool success, DemoAnalysisBlock failure = DemoAnalysisBlock.Unknown);

        void SetFrames(Demo demo, int framesPerSecond);

        void SetHash(Demo demo, string hash);
        
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
    /// Basic implementation of the <see cref="IDemoTableInterface"/>
    /// </summary>
    public class DemoTableInterface : IDemoTableInterface
    {
        private readonly DemoCentralContext _context;
        private readonly ILogger<DemoTableInterface> _logger;

        public DemoTableInterface(DemoCentralContext context, ILogger<DemoTableInterface> logger)
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
            demo.MD5Hash = hash;
            _context.SaveChanges();
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

        public void SetFileStatus(Demo demo, FileStatus status)
        {
            demo.FileStatus = status;
            _context.SaveChanges();
        }

        public void SetBlobUrl(Demo demo, string blobUrl)
        {
            demo.BlobUrl = blobUrl;
            _context.SaveChanges();
        }

        public void RemoveDemo(Demo demo)
        {
            _context.Demo.Remove(demo);
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
                .Where(x => FileStatusCollections.Failed.Contains(x.FileStatus) || x.AnalysisStatus == GenericStatus.Failure)
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
            var demo = _context.Demo.Where(x => x.MD5Hash.Equals(hash)).SingleOrDefault();

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

                if (demo.AnalysisStatus == GenericStatus.Failure)
                    return false;

                _logger.LogInformation($"Selected Demo [ {demo.MatchId} ] for re-analysis due to higher quality. Current quality [ {demo.Quality} ], requested quality [ {requestedQuality} ].");

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

        public void SetFrames(Demo demo, int framesPerSecond)
        {
            demo.FramesPerSecond = (byte) framesPerSecond;
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
        /// Returns IDs of demos for which the file is in BlobStorage where AnalysisStatus is not Success.
        /// </summary>
        /// <param name="minUploadDate"></param>
        /// <returns></returns>
        public List<Demo> GetUnfinishedDemos(DateTime minUploadDate)
        {
            var demosToReset = _context.Demo
                .Where(x => x.UploadDate >= minUploadDate)
                .Where(x=>x.FileStatus == FileStatus.InBlobStorage)
                .Where(x=>x.AnalysisStatus != GenericStatus.Success)
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

        /// <summary>
        /// Set's the Analze state
        /// If success if true `failure` is ignored.
        /// </summary>
        /// <param name="demo"></param>
        /// <param name="success"></param>
        /// <param name="failure"></param>
        public void SetAnalyzeState(Demo demo, bool success, DemoAnalysisBlock failure = DemoAnalysisBlock.Unknown)
        {
            if (success)
            {
                demo.AnalysisStatus = GenericStatus.Success;
            }
            else
            {
                demo.AnalysisStatus = GenericStatus.Failure;
                demo.DemoAnalysisBlock = failure;
            }
            _context.SaveChanges();
        }
    }

}

