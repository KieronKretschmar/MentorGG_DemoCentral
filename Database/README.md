# README

DemoCentral has one DataBase with two tables.
One for meta demo info, and for keeping track of all currently enqueued demos. 
The DataBase is generated code-first, both models are in `DatabaseClasses/`.

# `DEMO`
 `DataBaseClasses/Demo.cs`  
 
```
        public long MatchId 
        public DateTime MatchDate 
        public long UploaderId 
        public UploadType UploadType 
        public UploadStatus UploadStatus
        public Source Source 
        public string DownloadUrl 
        public string FileName 
        public string FilePath 
        public string Md5hash 
        public FileStatus FileStatus
        public DemoAnalyzerStatus DemoAnalyzerStatus 
        public string DemoAnalyzerVersion 
        public DateTime UploadDate
        public string Event 
```
Every unique demo aqcuired gets stored in this DataBase. 
# `InQueueDemo`
`DataBaseClasses/InQueueDemo.cs`
```
        public long MatchId { get; set; }
        public long UploaderId { get; set; }
        public DateTime MatchDate { get; set; }
        public DateTime InsertDate { get; set; }
        public bool DDQUEUE { get; set; }
        public bool DFWQUEUE { get; set; }
        public bool SOQUEUE { get; set; }
        public int Retries { get; set; }
```

This table keeps track of every demo currently in a queue.
### Must-Knows
- One entry per `InQueueDemo` , multiple columns, one for each queue.
- If an entry gets updated, so that every `*QUEUE` column is false, the entry gets removed immediately.
