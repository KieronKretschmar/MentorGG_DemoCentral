using Database.DatabaseClasses;
using DemoCentral.Enumerals;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DemoCentral.Helpers
{
    public static class DemoExtensions
    {
         public static DemoAnalyzeInstruction ToAnalyzeInstruction(this Demo demo)
        {
            return new DemoAnalyzeInstruction
            {
                MatchId = demo.MatchId,
                Source = demo.Source,
                MatchDate = demo.MatchDate,
                BlobUrl = demo.BlobUrl,
                FramesPerSecond = FramesPerQuality.Frames[demo.Quality],
                Quality = demo.Quality
            };
        }

        public static DemoDownloadInstruction ToDownloadInstruction(this Demo demo)
        {
            return new DemoDownloadInstruction
            {
                MatchId = demo.MatchId,
                DownloadUrl = demo.DownloadUrl
            };
        }
    }
}
