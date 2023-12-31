﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitCommunicationLib.Enums;

namespace DemoCentral.Enumerals
{
    public static class FramesPerQuality
    {
        public static Dictionary<AnalyzerQuality, byte> Frames = new Dictionary<AnalyzerQuality, byte>
        {
            {AnalyzerQuality.Low, 1},
            {AnalyzerQuality.Medium, 4},
            {AnalyzerQuality.High, 8}
        };
    }
}
