namespace OpenScrape.Features.RegionsTableMap.Update;

public record UpdateRegionTableMapRequest(string Category, string Name, int PosX, int PosY, int Width, int Height, double Umbral, double InactiveUmbral, string Color, bool? IsColor, bool? IsHash, bool? IsOnlyNumber, bool? IsBoard);

