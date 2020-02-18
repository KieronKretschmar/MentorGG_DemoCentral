using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataBase.Enumerals;

namespace DemoCentral.Models
{
    public class MatchHistoryModel
    {
        public List<MatchHistoryEntry> Entries { get; set; }

        public static MatchHistoryModel FromRecentMatches(long playerId, int recentMatches, int offset, IDemoCentralDBInterface dBInterface)
        {
            var matches = dBInterface.GetRecentMatches(playerId, recentMatches, offset);
            List<MatchHistoryEntry> matchHistoryEntries = new List<MatchHistoryEntry>();
            foreach (var match in matches)
            {
                matchHistoryEntries.Add(new MatchHistoryEntry
                {
                    MatchId = match.MatchId,
                    MatchDate = match.MatchDate,
                    Status = match.DemoFileWorkerStatus
                });
            }

            return new MatchHistoryModel
            {
                Entries = matchHistoryEntries
            };

        }

        public static MatchHistoryModel FromRecentFailedMatches(long playerId, int recentMatches, int offset, IDemoCentralDBInterface dBInterface)
        {
            var res = FromRecentMatches(playerId, recentMatches, offset, dBInterface);
            res.Entries = res.Entries.Where(x => x.Status == DemoFileWorkerStatus.Failed).ToList();
            return res;
        }


        public class MatchHistoryEntry
        {
            public long MatchId { get; set; }
            public DateTime MatchDate { get; set; }
            public DemoFileWorkerStatus Status { get; set; }
        }
    }

}
