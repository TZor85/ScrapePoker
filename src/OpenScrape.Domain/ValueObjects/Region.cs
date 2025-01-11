namespace OpenScrape.Domain.ValueObjects;

public record Region(string Category, string Name, int PosX, int PosY, int Width, int Height, bool? IsHash, bool? IsColor, bool? IsBoard, string? Color, bool? IsOnlyNumber, double? InactiveUmbral, double? Umbral);
