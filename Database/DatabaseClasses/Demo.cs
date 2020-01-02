using System;
using RabbitTransfer.Enums;
using DataBase.Enumerals;
using System.Collections.Generic;

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
        public DemoAnalyzerStatus DemoAnalyzerStatus { get; set; }
        public string DemoAnalyzerVersion { get; set; }
        public DateTime UploadDate { get; set; }
        public string Event {get; set; }
    }
}
