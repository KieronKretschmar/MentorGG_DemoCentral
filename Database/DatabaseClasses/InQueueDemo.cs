namespace Database.DatabaseClasses
{
    public partial class InQueueDemo
    {
        /// <summary>
        /// Navigational Property.
        /// </summary>
        public Demo Demo { get; set; }

        /// <summary>
        /// MatchId.
        /// </summary>
        public long MatchId { get; set; }

        /// <summary>
        /// Amount of Retries attempted to complete the last `Demo.DemoAnalyzeFailure`.
        /// </summary>
        public int RetryAttemptsOnCurrentFailure { get; set; }

        /// <summary>
        /// Current Queue the Demo is in.
        /// </summary>
        public Queue CurrentQueue { get; set; }

    }

    public enum Queue : byte
    {
        DemoDownloader = 10,

        DemoFileWorker = 20,

        MatchWriter = 30,

        SituationOperator = 40,
    }
}
