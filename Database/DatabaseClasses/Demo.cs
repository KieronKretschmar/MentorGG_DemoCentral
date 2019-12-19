using System;
using System.Collections.Generic;

namespace DataBase.DatabaseClasses
{
    public partial class Demo
    {
        public long MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public long UploaderId { get; set; }
        public byte UploadType { get; set; }
        public byte UploadStatus{get; set; }
        public byte Source { get; set; }
        public string DownloadUrl { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Md5hash { get; set; }
        public byte FileStatus { get; set; }
        public byte DemoAnalyzerStatus { get; set; }
        public string DemoAnalyzerVersion { get; set; }
        public DateTime UploadDate { get; set; }
        public string Event {get; set; }
    }
}
