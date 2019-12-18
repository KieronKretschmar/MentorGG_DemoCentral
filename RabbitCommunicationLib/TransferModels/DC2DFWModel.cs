using System;


public class DC2DFWModel : TransferModel
{
    public DC2DFWModel(DC2DFWModel original)
    {
        Source = original.Source;
        FramesPerSecond = original.FramesPerSecond;
        MatchDate = original.MatchDate;
        Event = original.Event;
    }

    public DC2DFWModel() { }

    public string Source { get; set; }
    public double FramesPerSecond { get; set; }
    public DateTime MatchDate { get; set; }
    public string Event { get; set; }
    public string ZippedFilePath { get; set; }
}
