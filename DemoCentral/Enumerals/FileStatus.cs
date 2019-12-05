using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DemoCentral.Enumerals
{
    public enum FileStatus
    {
        NEW = 0,
        ANALYZED = 10,
        DOWNLOADED = 20,
        DOWNLOADFAILED = 30,
        ANALYZERFAILED = 31,
        UNZIPFAILED =32,
        REMOVED = 40
    }
}
