namespace OpenScrape.Domain.ValueObjects;

/// <summary>
/// Estadísticos agregados de una categoría de telemetría (percentiles, máximo y conteo).
/// Unidades en milisegundos para facilitar serialización JSON y consultas SQL sobre <c>jsonb</c>.
/// </summary>
public sealed record CategoryStats(
    double P50Ms,
    double P95Ms,
    double MaxMs,
    long Count);
