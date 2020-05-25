using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Enumerals;

namespace DemoCentral.Models
{
    public class MatchHistoryModel
    {
        public List<MatchHistoryEntry> Entries { get; set; }

        public static MatchHistoryModel FromRecentFailedMatches(long playerId, int recentMatches, int offset, IDemoTableInterface demoTableInterface)
        {
            var failedMatches = demoTableInterface.GetRecentFailedMatches(playerId, recentMatches, offset);
            var entries = failedMatches
                .Select(match => new MatchHistoryEntry
                {
                    MatchId = match.MatchId,
                    MatchDate = match.MatchDate,
                    Success = match.AnalysisSucceeded
                })
                .ToList();

            return new MatchHistoryModel
            {
                Entries = entries
            };
        }


        public class MatchHistoryEntry
        {
            public long MatchId { get; set; }
            public DateTime MatchDate { get; set; }
            public bool Success { get; set; }
        }
    }

}
