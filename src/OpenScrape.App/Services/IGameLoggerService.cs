namespace OpenScrape.App.Services;

using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

/// <summary>
/// Superficie de registro que consume <see cref="GameCoordinator"/>.
/// Existe para poder instanciar el coordinador en los tests sin una base de datos.
/// </summary>
public interface IGameLoggerService
{
    void LogStreetDecision(StreetDecision decision);

    void UpdateBoard(List<string> flopCards, string? turnCard = null, string? riverCard = null);

    void UpdateSituation(HandSituation situation);
}
