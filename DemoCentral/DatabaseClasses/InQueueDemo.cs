using System;
using System.Collections.Generic;

namespace DemoCentral.DatabaseClasses
{
    public partial class InQueueDemo
    {
        public long MatchId { get; set; }
        public DateTime MatchDate { get; set; }
        public long UploaderId { get; set; }
        public byte UploadType { get; set; }
        public byte Source { get; set; }
        public DateTime InsertDate { get; set; }
        public bool DDQUEUE { get; set; }
        public bool DFWQUEUE { get; set; }
        public bool SOQUEUE { get; set; }
        public int Retries { get; set; }
    }
}
