using OpenScrape.Domain.Enums;

namespace OpenScrape.Features.ActionScenario;

public class ActionScenarioRequest
{
    public string? HandName { get; set; }
    public bool? Suited { get; set; }
    public TablePosition? HeroPosition { get; set; }
    public TablePosition? OpenRaiser { get; set; }
    public TablePosition? ThreeBetPosition { get; set; }
    public TablePosition? Limper { get; set; }
    public TablePosition? Caller { get; set; }
    public TablePosition? Squeezer { get; set; }
    public decimal? BetSize { get; set; }
    public bool? IsGreater { get; set; }
    public bool? RaiserFolds { get; set; }
}
