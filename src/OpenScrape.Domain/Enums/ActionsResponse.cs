namespace OpenScrape.Domain.Enums;

public class ActionsResponse
{
    public string Action { get; set; } = default!;
    public List<string> Hands { get; set; } = default!;
    public Styles Style { get; set; } = default!;
    public Positions Position { get; set; } = default!;

}
