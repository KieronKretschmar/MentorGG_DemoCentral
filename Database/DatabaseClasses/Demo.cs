using System;
using RabbitCommunicationLib.Enums;
using Database.Enumerals;
using RabbitCommunicationLib.TransferModels;

namespace Database.DatabaseClasses
{
    public partial class Demo
    {
        public long MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public long UploaderId { get; set; }
        public UploadType UploadType { get; set; }
        public UploadStatus UploadStatus{get; set; }
        public Source Source { get; set; }
        public string DownloadUrl { get; set; }
        public string BlobUrl { get; set; }
        public string Md5hash { get; set; }
        public FileStatus FileStatus { get; set; }
        public AnalyzerQuality Quality { get; set; }
        public byte FramesPerSecond { get; set; }
        public DemoFileWorkerStatus DemoFileWorkerStatus { get; set; }
        public string DemoFileWorkerVersion { get; set; }
        public string DatabaseVersion { get; set; }
        public DateTime UploadDate { get; set; }
        public string Event {get; set; }


        public static Demo FromGatherTransferModel(DemoInsertInstruction model)
        {
            return new Demo
            {
                MatchDate = model.MatchDate,
                UploaderId = model.UploaderId,
                UploadType = model.UploadType,
                UploadStatus = UploadStatus.New,
                Source = model.Source,
                DownloadUrl = model.DownloadUrl,
                BlobUrl = "",
                Md5hash = "",
                FileStatus = FileStatus.New,
                DemoFileWorkerStatus = DemoFileWorkerStatus.New,
                DemoFileWorkerVersion = "",
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
                UploadStatus = UploadStatus.New,
                Source = model.Source,
                DownloadUrl = null,
                BlobUrl = model.BlobUrl,
                Md5hash = "",
                FileStatus = FileStatus.New,
                DemoFileWorkerStatus = DemoFileWorkerStatus.New,
                DemoFileWorkerVersion = "",
                UploadDate = model.UploadDate,
            };
        }

        public bool HasFailedAnalysis()
        {
            switch (DemoFileWorkerStatus)
            {
                case DemoFileWorkerStatus.New:
                case DemoFileWorkerStatus.InQueue:
                case DemoFileWorkerStatus.Finished:
                    return false;
                case DemoFileWorkerStatus.BlobStorageDownloadFailed:
                case DemoFileWorkerStatus.UnzipFailed:
                case DemoFileWorkerStatus.DuplicateCheckFailed:
                case DemoFileWorkerStatus.AnalyzerFailed:
                case DemoFileWorkerStatus.CacheUploadFailed:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown DemoFileWorkerStatus [ {DemoFileWorkerStatus} ] for HasFailedAnalysis( Match [ {MatchId} ] )");
            }
        }

        /// <summary>
        /// Resets analysis to the stage where it was before DemoFileWorker
        /// </summary>
        public void ToPreAnalysisState()
        {
            UploadStatus = UploadStatus.New;
            Md5hash = "";
            DemoFileWorkerStatus = DemoFileWorkerStatus.New;
            DemoFileWorkerVersion = "";
        }
    }
}
