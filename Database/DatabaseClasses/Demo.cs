﻿using System;
using RabbitCommunicationLib.Enums;
using Database.Enumerals;
using RabbitCommunicationLib.TransferModels;

namespace Database.DatabaseClasses
{
    public partial class Demo
    {
        /// <summary>
        /// Navigational Propery.
        /// </summary>
        public InQueueDemo InQueueDemo { get; set; }

        /// <summary>
        /// MatchId.
        /// </summary>
        /// <value></value>
        public long MatchId { get; set; }

        /// <summary>
        /// When the Match took place.
        /// </summary>
        /// <value></value>
        public DateTime MatchDate { get; set; }

        /// <summary>
        /// SteamId of the uploader.
        /// </summary>
        /// <value></value>
        public long UploaderId { get; set; }

        /// <summary>
        /// The Method used to obtain a Demo.
        /// </summary>
        public UploadType UploadType { get; set; }

        /// <summary>
        /// Matchwriter Storage status.
        /// </summary>
        /// <value></value>    
        public GenericStatus MatchWriterStatus {get; set; }

        /// <summary>
        /// Source of the Demo.
        /// Where the demo came from.
        /// </summary>
        /// <value></value>
        public Source Source { get; set; }

        /// <summary>
        /// Download Url to download the Demo.
        /// </summary>
        /// <value></value>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Internal BlobUrl of the Demo file.
        /// </summary>
        /// <value></value>
        public string BlobUrl { get; set; }

        /// <summary>
        /// MD5 Hash of the Demo file.
        /// </summary>
        /// <value></value>
        public string MD5Hash { get; set; }

        /// <summary>
        /// Current status of the Demo file.
        /// </summary>
        /// <value></value>
        public FileStatus FileStatus { get; set; }

        /// <summary>
        /// Quality the Demo is analyzed in.
        /// </summary>
        /// <value></value>
        public AnalyzerQuality Quality { get; set; }

        /// <summary>
        /// Frames Per Second the Demo is analyzed in.
        /// </summary>
        /// <value></value>
        public byte FramesPerSecond { get; set; }

        /// <summary>
        /// DemoFileWorker general status
        /// </summary>
        /// <value></value>
        public GenericStatus DemoFileWorkerStatus { get; set; } = GenericStatus.Unknown;

        /// <summary>
        /// If DemoFileWorkerStatus is `Failure`, How DemoFileWorker failed.
        /// </summary>
        /// <value></value>
        public DemoAnalyzeFailure DemoAnalyzeFailure { get; set; } = DemoAnalyzeFailure.Unknown;

        /// <summary>
        /// When the Demo was first seen.
        /// </summary>
        /// <value></value>
        public DateTime UploadDate { get; set; }


        public static Demo FromGatherTransferModel(DemoInsertInstruction model)
        {
            return new Demo
            {
                MatchDate = model.MatchDate,
                UploaderId = model.UploaderId,
                UploadType = model.UploadType,
                MatchWriterStatus = GenericStatus.Unknown,
                Source = model.Source,
                DownloadUrl = model.DownloadUrl,
                BlobUrl = "",
                MD5Hash = "",
                FileStatus = FileStatus.New,
                UploadDate = DateTime.UtcNow,
            };
        }

        public static Demo FromManualUploadTransferModel(ManualDownloadReport model)
        {
            return new Demo
            {
                MatchDate = model.MatchDate,
                UploaderId = model.UploaderId,
                UploadType = model.UploadType,
                MatchWriterStatus = GenericStatus.Unknown,
                Source = model.Source,
                DownloadUrl = null,
                BlobUrl = model.BlobUrl,
                MD5Hash = "",
                FileStatus = FileStatus.New,
                UploadDate = model.UploadDate,
            };
        }

        /// <summary>
        /// Resets analysis to the stage where it was before DemoFileWorker
        /// </summary>
        public void ToPreAnalysisState()
        {
            MatchWriterStatus = GenericStatus.Unknown;
            MD5Hash = "";
            DemoFileWorkerStatus = GenericStatus.Unknown;
        }
    }
}
