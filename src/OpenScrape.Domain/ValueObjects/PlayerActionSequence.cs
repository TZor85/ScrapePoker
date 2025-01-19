namespace OpenScrape.Domain.ValueObjects;

public record PlayerActionSequence(string Name, string HeroPosition, string? OpenRaiser, string? ThreeBetPosition, string? Limper, string? Caller, string? Squeezer, decimal? BetSize, bool? IsGreater, bool? RaiserFolds, List<Hand> Hands);
