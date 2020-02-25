namespace DataBase.Enumerals
{
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
