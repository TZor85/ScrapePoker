namespace OpenScrape.App.Configuration;

public sealed class GameLoopOptions
{
    public const string SectionName = "GameLoop";

    public int CaptureIntervalMs { get; set; } = 100;

    public int StopTimeoutMs { get; set; } = 2000;
}
