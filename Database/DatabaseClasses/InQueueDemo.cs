﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Database.Enumerals;

namespace Database.DatabaseClasses
{
    public partial class InQueueDemo
    {
        /// <summary>
        /// Navigational Propery.
        /// </summary>
        public Demo Demo;

        /// <summary>
        /// MatchId.
        /// </summary>
        /// <value></value>
        public long MatchId { get; set; }

        /// <summary>
        /// IDK
        /// </summary>
        public DateTime InsertDate { get; set; }

        /// <summary>
        /// Amount of Retries attempted to complete the last `Demo.DemoAnalyzeFailure`.
        /// </summary>
        public int RetryAttemptsOnCurrentFailure { get; set; }

        /// <summary>
        /// Current Queue the Demo is in.
        /// </summary>
        public Queue CurrentQueue { get; set; }

    }

    public enum Queue : byte
    {
        UnQueued = 0,

        DemoDownloader = 1,

        DemoFileWorker = 2,

        SitutationOperator = 3,
    }
}
