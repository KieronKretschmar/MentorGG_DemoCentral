using System.Collections.Generic;

namespace Database.Enumerals
{
    /// <summary>
    /// This enum contains all possible values from demofileworker
    /// </summary>
    public enum DemoFileWorkerStatus : byte
    {
        New = 0,
        InQueue = 1,
        Finished = 2,

        BlobStorageDownloadFailed = 31,
        UnzipFailed = 32,
        DuplicateCheckFailed = 33,
        AnalyzerFailed = 34, 
        CacheUploadFailed = 35,
    }

    public static class DemoFileWorkerStatusCollections
    {
        public static List<DemoFileWorkerStatus> Failed = new List<DemoFileWorkerStatus> 
        {
            DemoFileWorkerStatus.BlobStorageDownloadFailed,
            DemoFileWorkerStatus.UnzipFailed,
            DemoFileWorkerStatus.DuplicateCheckFailed,
            DemoFileWorkerStatus.AnalyzerFailed,
            DemoFileWorkerStatus.CacheUploadFailed
        };
    }
}