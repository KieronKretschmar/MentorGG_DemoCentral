using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DemoCentral.Models
{
    public class DownloadUrlModel
    {
        public List<DemoUrlPair> demos { get; set; }
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class DemoUrlPair
    {
        public long MatchId { get; set; }
        public string DownloadUrl { get; set; }
    }


}
