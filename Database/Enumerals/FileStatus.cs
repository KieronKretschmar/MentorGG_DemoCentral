﻿using System.Collections.Generic;

namespace Database.Enumerals
{
    /// <summary>
    /// This enum contains the options for a demo file unrelated to demoFileWorker
    /// </summary>
    public enum FileStatus : byte
    {
        New = 0,
        Downloading = 10,
        DownloadRetrying = 11,
        DownloadFailed = 12,
        InBlobStorage = 30,
        Removed = 50
    }

    public static class FileStatusCollections
    {
        public static List<FileStatus> Failed = new List<FileStatus> { FileStatus.DownloadFailed };
    }
}
