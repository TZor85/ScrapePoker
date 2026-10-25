using OpenScrape.App.Services;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>Logger nulo: los fixtures no persisten nada.</summary>
internal sealed class NoOpGameLogger : IGameLoggerService
{
    public void LogStreetDecision(StreetDecision decision)
    {
    }

    public void UpdateBoard(List<string> flopCards, string? turnCard = null, string? riverCard = null)
    {
    }

    public void UpdateSituation(HandSituation situation)
    {
    }
}
