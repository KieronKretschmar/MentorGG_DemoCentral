using Database.DatabaseClasses;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
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

        bool IsAnalysisRequired(string hash, out long? matchId, AnalyzerQuality requestedQuality);

        List<Demo> GetMatchesByUploader(long steamId);

        void RemoveDemo(Demo demo);

        void SetBlobUrl(Demo demo, string blobUrl);


        /// <summary>
        /// Sets the Analyze state
        /// If success if true the block is ignored.
        /// </summary>
        /// <param name="demo"></param>
        /// <param name="analysisFinishedSuccessfully"></param>
        /// <param name="block"></param>
        void SetAnalyzeState(Demo demo, bool analysisFinishedSuccessfully, DemoAnalysisBlock? block = null);

        void SetHash(Demo demo, string hash);
        void SetHash(long matchId, string hash);
        void SetUploadStatus(Demo demo, bool success);
        void SetUploadStatus(long matchId, bool success);
        List<long> GetExpiredDemosId();

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
        long CreateNewDemoEntryFromManualUpload(ManualDownloadInsertInstruction model, AnalyzerQuality requestedQuality);
        List<Demo> GetRecentFailedMatches(long playerId, int recentMatches, int offset = 0);
        List<Demo> GetFailedDemos(DateTime minUploadDate);
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

        public List<long> GetRecentMatchIds(long playerId, int recentMatches, int offset = 0)
        {
            List<long> recentMatchesId = _context.Demo.Where(x => x.UploaderId == playerId).Select(x => x.MatchId).Take(recentMatches + offset).ToList();
            recentMatchesId.RemoveRange(0, offset);

            return recentMatchesId;
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
                .Where(x => x.AnalysisSucceeded == false)
                .Take(recentMatches + offset)
                .ToList();
            recentMatchesId.RemoveRange(0, offset);

            return recentMatchesId;
        }

        /// <summary>
        /// Checks if a hash is already in the database, and analyzed with higher quality than the requested amount
        /// 
        /// </summary>
        /// <param name="matchId">id of the original match</param>
        public bool IsAnalysisRequired(string hash, out long? matchId, AnalyzerQuality requestedQuality)
        {
            matchId = null;

            //Check the Demo table if an entry contains the MD5Hash `hash`.
            var demo = _context.Demo.Where(x => x.MD5Hash.Equals(hash)).SingleOrDefault();

            // If a match was found.
            if (demo != null)
            {
                // Set the output parameter to it's MatchId.
                matchId = demo.MatchId;
                
                // If the requested quality it HIGHER than the currently analysed
                // Quality, return true to indciate that this Demo does need analysed 
                if (requestedQuality > demo.Quality && demo.AnalysisSucceeded)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            // If no matching Demo was found
            // Indicating this Demo's MD5 Hash has not been seen before
            // Allow Analysis
            else
            {   
                return true;
            }
        }


        public bool TryCreateNewDemoEntryFromGatherer(DemoInsertInstruction model, AnalyzerQuality requestedQuality, out long matchId)
        {
            // Check if an exisiting Demo has the same DownloadUrl.
            var demo = _context.Demo.SingleOrDefault(x => x.DownloadUrl.Equals(model.DownloadUrl));

            // If a Demo was found with the same DownloadUrl.
            if (demo != null)
            {
                matchId = demo.MatchId;

                // If the Demo that was found has a higher quality and has successfully been analysed
                // Do not allow analysis
                if (requestedQuality <= demo.Quality && demo.AnalysisSucceeded)
                    return false;

                // If the Demo that was found has not succeeded and not in a queue
                // Do not allow analysis
                if (!demo.AnalysisSucceeded && demo.InQueueDemo == null)
                    return false;

                _logger.LogInformation($"Selected Demo [ {demo.MatchId} ] for re-analysis due to higher quality. Current quality [ {demo.Quality} ], requested quality [ {requestedQuality} ].");

                demo.Quality = requestedQuality;
                
                _context.SaveChanges();

                return true;
            }
            else
            {
                demo = Demo.FromGatherTransferModel(model);
                demo.Quality = requestedQuality;
                
                _context.Demo.Add(demo);
                _context.SaveChanges();

                matchId = demo.MatchId;

                return true;
            }

        }

        public long CreateNewDemoEntryFromManualUpload(ManualDownloadInsertInstruction model, AnalyzerQuality requestedQuality)
        {
            var demo = Demo.FromManualUploadTransferModel(model);
            demo.Quality = requestedQuality;

            _context.Demo.Add(demo);

            _context.SaveChanges();

            return demo.MatchId;
        }

        public Demo GetDemoById(long matchId)
        {
            return _context.Demo.Single(x => x.MatchId == matchId);
        }

        public List<Demo> GetMatchesByUploader(long steamId)
        {
            return _context.Demo.Where(x => x.UploaderId == steamId).ToList();
        }

        /// <summary>
        /// Returns Demos for which AnalysisStatus is not Success.
        /// </summary>
        /// <param name="minUploadDate"></param>
        /// <returns></returns>
        public List<Demo> GetFailedDemos(DateTime minUploadDate)
        {  
            return _context.Demo
                .Where(x => x.UploadDate >= minUploadDate)
                .Where(x=>x.AnalysisSucceeded == false)
                .ToList();
        }

        /// <inheritdoc/>
        public void SetAnalyzeState(Demo demo, bool analysisFinishedSuccessfully, DemoAnalysisBlock? block = null)
        {
            if (analysisFinishedSuccessfully)
            {
                demo.AnalysisSucceeded = true;
            }
            else
            {
                demo.AnalysisSucceeded = false;
                demo.AnalysisBlockReason = block;
            }
            _context.SaveChanges();
        }
        

        public List<long> GetExpiredDemosId()
        {
            throw new NotImplementedException();
        }
    }

}

