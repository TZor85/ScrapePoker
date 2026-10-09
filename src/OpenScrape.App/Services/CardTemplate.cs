using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Plantilla de carta precomputada: píxeles BGRA contiguos, dimensiones y dHash.
/// Evita decodificar base64 y recomputar el hash en cada comparación.
/// </summary>
public sealed record CardTemplate(CardDTO Card, byte[] Bgra, int Width, int Height, ulong DHash);
