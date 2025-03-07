namespace OpenScrape.Domain.ValueObjects;

public class PotOddsResult
{
    public decimal PotOddsPercentage { get; set; }    // Pot odds en porcentaje (ej: 25%)
    public decimal EquityPercentage { get; set; }     // Equity en porcentaje (ej: 35%)
    public bool ShouldCall { get; set; }              // ¿Es rentable pagar?
    public string? Street { get; set; }               // Calle actual (ej: "Flop")
}
