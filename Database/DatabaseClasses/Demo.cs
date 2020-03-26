using System;
using RabbitCommunicationLib.Enums;
using DataBase.Enumerals;
using RabbitCommunicationLib.TransferModels;

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

        public bool HasFailedAnalysis()
        {
            switch (DemoFileWorkerStatus)       
            {
                case DemoFileWorkerStatus.New:
                case DemoFileWorkerStatus.InQueue:
                case DemoFileWorkerStatus.Finished:
                    return false;
                default:
                    return true;
            }
        }
    }
}
