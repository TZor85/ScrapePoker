using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.Domain.Entities;

public class GameRound
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long HandNumber { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string TableName { get; set; } = string.Empty;

    // Estado del hero
    public string HeroCard1 { get; set; } = string.Empty;
    public string HeroCard2 { get; set; } = string.Empty;
    public TablePosition HeroPosition { get; set; }
    public decimal HeroStackStart { get; set; }

    // Board
    public List<string> FlopCards { get; set; } = new();
    public string? TurnCard { get; set; }
    public string? RiverCard { get; set; }

    // Decisiones por street
    public List<StreetDecision> Decisions { get; set; } = new();

    // Resultado
    public decimal PotSizeFinal { get; set; }
    public BoardPosition LastStreetPlayed { get; set; }
    public int NumOpponents { get; set; }
    public decimal HeroStackEnd { get; set; }
    public HandResult Result { get; set; } = HandResult.Unknown;
    public HandSituation Situation { get; set; } = HandSituation.None;

    // Sesión (para agrupar manos de una misma sesión)
    public string SessionId { get; set; } = string.Empty;
}

public enum HandResult
{
    Unknown,
    Won,
    Lost,
    Push
}
