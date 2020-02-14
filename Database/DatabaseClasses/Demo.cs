using System;
using RabbitTransfer.Enums;
using DataBase.Enumerals;
using System.Collections.Generic;
using RabbitTransfer.TransferModels;
using Database.Enumerals;

namespace DataBase.DatabaseClasses
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
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Md5hash { get; set; }
        public FileStatus FileStatus { get; set; }
        public AnalyzerQuality Quality { get; set; }
        public byte FramesPerSecond { get; set; }
        public DemoFileWorkerStatus DemoFileWorkerStatus { get; set; }
        public string DemoFileWorkerVersion { get; set; }
        public string DatabaseVersion { get; set; }
        public DateTime UploadDate { get; set; }
        public string Event {get; set; }


        public static Demo FromGatherTransferModel(GathererTransferModel model)
        {
            return new Demo
            {
                MatchDate = model.MatchDate,
                UploaderId = model.UploaderId,
                UploadType = model.UploadType,
                UploadStatus = UploadStatus.NEW,
                Source = model.Source,
                DownloadUrl = model.DownloadUrl,
                FileName = "",
                FilePath = "",
                Md5hash = "",
                FileStatus = FileStatus.NEW,
                DemoFileWorkerStatus = DemoFileWorkerStatus.New,
                DemoFileWorkerVersion = "",
                UploadDate = DateTime.UtcNow,
            };
        }
    }
}
