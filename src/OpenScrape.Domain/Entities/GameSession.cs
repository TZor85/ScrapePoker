using System.Text.Json.Serialization;
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

    // Manos de esta sesión — no se persisten embebidas; cada HandRecord
    // vive en su propia colección Marten con FK GameSessionId.
    // Esta lista se usa solo en memoria durante la sesión activa.
    [JsonIgnore]
    public List<HandRecord> Hands { get; set; } = new();

    // Métricas agregadas — calculadas en memoria, no persistidas en Marten
    [JsonIgnore]
    public int TotalHands => Hands.Count;
    [JsonIgnore]
    public decimal TotalProfit => Hands
        .Where(h => h.Result != HandResult.Unknown)
        .Sum(h => h.HeroStackEnd - h.HeroStackStart);
    [JsonIgnore]
    public double BBPer100 => BigBlind > 0m && TotalHands > 0
        ? (double)(TotalProfit / BigBlind) / TotalHands * 100
        : 0;

    /// <summary>
    /// Valida que la sesión tenga datos mínimos consistentes.
    /// </summary>
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(SessionId) &&
        !string.IsNullOrWhiteSpace(TableName) &&
        BigBlind > 0m;
}

/// <summary>
/// Registro de una mano individual. Documento Marten independiente
/// vinculado a su GameSession mediante GameSessionId.
/// </summary>
public class HandRecord
{
    // Identificador único del documento Marten
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // FK hacia el GameSession padre
    public string GameSessionId { get; set; } = string.Empty;

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
