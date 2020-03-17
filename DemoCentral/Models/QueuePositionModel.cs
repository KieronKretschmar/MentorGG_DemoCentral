using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.Models
{
    public class QueueStatusModel
    {
        /// <summary>
        /// The position of the first demo in the queue that the user uploaded.
        /// </summary>
        public int FirstDemoPosition { get; set; }

        /// <summary>
        /// List of matchIds of all the matches in queue that were uploaded by the user.
        /// </summary>
        public List<long> MatchIds { get; set; }

        /// <summary>
        /// Total number of demos in queue.
        /// </summary>
        public int TotalQueueLength { get; set; }
    }
}
