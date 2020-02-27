namespace DataBase.Enumerals
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
}
