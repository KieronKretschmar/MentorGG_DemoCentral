namespace DataBase.Enumerals
{
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
}