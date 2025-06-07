using OpenScrape.App.Entities;

namespace OpenScrape.App.Helpers.FlopHelper;

public class FlopAnalyzerHelperReqest
{
    public PlayerGameState PlayerState { get; set; } = new PlayerGameState();
    public TableScrapeFlopResult TableScrapeFlopResult { get; set; } = new TableScrapeFlopResult();
}
