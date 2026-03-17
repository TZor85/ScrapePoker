using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.Domain.Entities;

/// <summary>
/// Representa una sesión de juego en una mesa específica.
/// Un documento por sesión/mesa en Marten, con todas las manos dentro.
/// </summary>
public class GameSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow;
    public decimal BigBlind { get; set; } = 0.50m;

    // Todas las manos de esta sesión
    public List<HandRecord> Hands { get; set; } = new();

    // Métricas agregadas (actualizadas al cerrar cada mano)
    public int TotalHands => Hands.Count;
    public decimal TotalProfit => Hands
        .Where(h => h.Result != HandResult.Unknown)
        .Sum(h => h.HeroStackEnd - h.HeroStackStart);
    public double BBPer100 => BigBlind > 0 && TotalHands > 0
        ? (double)(TotalProfit / BigBlind) / TotalHands * 100
        : 0;
}

/// <summary>
/// Registro de una mano individual dentro de una sesión.
/// Value object almacenado como parte del documento GameSession.
/// </summary>
public class HandRecord
{
    public long HandNumber { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Estado del hero
    public string HeroCard1 { get; set; } = string.Empty;
    public string HeroCard2 { get; set; } = string.Empty;
    public TablePosition HeroPosition { get; set; }
    public decimal HeroStackStart { get; set; }
    public decimal HeroStackEnd { get; set; }

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
    public HandResult Result { get; set; } = HandResult.Unknown;
    public HandSituation Situation { get; set; } = HandSituation.None;
}

public enum HandResult
{
    Unknown,
    Won,
    Lost,
    Push
}
