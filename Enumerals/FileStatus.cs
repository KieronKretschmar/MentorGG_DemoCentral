using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DemoCentral.Enumerals
{
    public enum FileStatus
    {
        NEW = 0,
        DOWNLOADED = 20,
        DOWNLOADFAILED = 21,
        DOWNLOAD_RETRYING =22,
        UNZIPPED = 40,
        UNZIPFAILED =41,
        REMOVED = 50
    }
}
