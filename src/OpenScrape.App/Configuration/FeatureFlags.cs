namespace OpenScrape.App.Configuration;

public sealed class FeatureFlags
{
    public const string SectionName = "Features";

    public bool UseGameLoopCoordinator { get; set; } = false;
}
