namespace OpenScrape.Domain.ValueObjects;

public record Hand(string Name, bool? Suited, string Action, int Percentage)
{
    public string Name { get; init; } = !string.IsNullOrWhiteSpace(Name)
        ? Name
        : throw new ArgumentException("El nombre de la mano no puede estar vacío.", nameof(Name));

    public int Percentage { get; init; } = Percentage >= 0 && Percentage <= 100
        ? Percentage
        : throw new ArgumentOutOfRangeException(nameof(Percentage), $"Percentage debe estar entre 0 y 100, valor: {Percentage}");
}
