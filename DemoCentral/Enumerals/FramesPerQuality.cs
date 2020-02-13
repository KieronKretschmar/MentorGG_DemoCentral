using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Enumerals;

namespace DemoCentral.Enumerals
{
    public static class FramesPerQuality
    {
        public static Dictionary<AnalyzerQuality, byte> Frames = new Dictionary<AnalyzerQuality, byte>
        {
            {AnalyzerQuality.Low, 1},
            {AnalyzerQuality.Medium, 8},
            {AnalyzerQuality.High, 16}
        };
    }
}
