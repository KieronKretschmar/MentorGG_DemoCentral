using System;
using System.Collections.Generic;
using System.Text;


public class GathererTransferModel : TransferModel
{
    public string DownloadUrl { get; set; }
    public DateTime MatchDate { get; set; }
    public long UploaderId { get; set; }
    public byte Source { get; set; }
    public byte UploadType { get; set; }

}
