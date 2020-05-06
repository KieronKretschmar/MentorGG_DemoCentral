using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.DatabaseClasses
{
    public partial class InQueueDemo
    {
        public long MatchId { get; set; }
        public long UploaderId { get; set; }
        public DateTime MatchDate { get; set; }
        public DateTime InsertDate { get; set; }
        public bool DDQUEUE { get; set; }
        public bool DFWQUEUE { get; set; }
        public bool SOQUEUE { get; set; }
        public int Retries { get; set; }

        private List<bool> Queues => new List<bool> { DDQUEUE, DFWQUEUE, SOQUEUE };

        public bool InAnyQueue()
        {
            return Queues.Contains(true);
        }
    }
}
