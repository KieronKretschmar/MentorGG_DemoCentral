using System;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.TransferModels;

namespace Database.DatabaseClasses
{
    public partial class Demo
    {
        /// <summary>
        /// Navigational Propery.
        /// </summary>
        public InQueueDemo InQueueDemo { get; set; }

        /// <summary>
        /// MatchId.
        /// </summary>
        /// <value></value>
        public long MatchId { get; set; }

        /// <summary>
        /// When the Match took place.
        /// </summary>
        /// <value></value>
        public DateTime MatchDate { get; set; }

        /// <summary>
        /// SteamId of the uploader.
        /// </summary>
        /// <value></value>
        public long UploaderId { get; set; }

        /// <summary>
        /// The Method used to obtain a Demo.
        /// </summary>
        public UploadType UploadType { get; set; }

        /// <summary>
        /// Source of the Demo.
        /// Where the demo came from.
        /// </summary>
        /// <value></value>
        public Source Source { get; set; }

        /// <summary>
        /// Download Url to download the Demo.
        /// </summary>
        /// <value></value>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Internal BlobUrl of the Demo file.
        /// </summary>
        /// <value></value>
        public string BlobUrl { get; set; }

        /// <summary>
        /// MD5 Hash of the Demo file.
        /// </summary>
        /// <value></value>
        public string MD5Hash { get; set; }

        /// <summary>
        /// Quality the Demo is analyzed in.
        /// </summary>
        /// <value></value>
        public AnalyzerQuality Quality { get; set; }

        /// <summary>
        /// Outcome of the Demo analysis process.
        /// </summary>
        /// <value></value>
        public bool AnalysisSucceeded { get; set; } = false;

        /// <summary>
        /// Reason why the analysis process stopped for this demo.
        /// </summary>
        /// <value></value>
        public DemoAnalysisBlock? AnalysisBlockReason { get; set; } = null;

        /// <summary>
        /// When the Demo was first seen.
        /// </summary>
        /// <value></value>
        public DateTime UploadDate { get; set; }

        /// <summary>
        /// When the Demo is set to expire.
        /// </summary>
        /// <value></value>
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// Indicates whether MatchData has been removed due to the Expiry Date passing.
        /// </summary>
        /// <value></value>
        public bool MatchDataRemoved { get; set; } = false;


        public static Demo FromGatherTransferModel(DemoInsertInstruction model)
        {
            return new Demo
            {
                MatchDate = model.MatchDate,
                UploaderId = model.UploaderId,
                UploadType = model.UploadType,
                Source = model.Source,
                DownloadUrl = model.DownloadUrl,
                BlobUrl = "",
                MD5Hash = "",
                UploadDate = DateTime.UtcNow,
            };
        }

        public static Demo FromManualUploadTransferModel(ManualDownloadInsertInstruction model)
        {
            return new Demo
            {
                MatchDate = model.MatchDate,
                UploaderId = model.UploaderId,
                UploadType = model.UploadType,
                Source = model.Source,
                DownloadUrl = null,
                BlobUrl = model.BlobUrl,
                MD5Hash = "",
                UploadDate = model.UploadDate,
            };
        }
    }
}
