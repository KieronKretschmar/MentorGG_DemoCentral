using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Enumerals;

namespace DemoCentral.Enumerals
{
    public static class FramesPerSubscription
    {
        public static Dictionary<AnalyzerQuality, int> Frames = new Dictionary<AnalyzerQuality, int>
        {
            {AnalyzerQuality.Low, 1},
            {AnalyzerQuality.Medium, 8},
            {AnalyzerQuality.High, 16}
        };
    }
}
