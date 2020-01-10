﻿namespace DataBase.Enumerals
{
    public enum FileStatus
    {
        NEW = 0,
        DOWNLOADED = 20,
        DOWNLOADFAILED = 21,
        DOWNLOAD_RETRYING = 22,
        DOWNLOADING = 23,
        DUPLICATECHECKFAILED=31,
        UNZIPPED = 40,
        UNZIPFAILED = 41,
        REMOVED = 50
    }
}
