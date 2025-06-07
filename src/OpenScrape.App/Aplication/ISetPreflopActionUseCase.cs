using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication;


public class SetPreflopActionUseCaseRequest
{
    public ResponseAction ResponseAction { get; set; } = new ResponseAction();
    public PlayerGameState ScrapeResult { get; set; } = new PlayerGameState();

    public Dictionary<TablePosition, Dictionary<TablePosition, decimal>> PreflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
}

public class SetPreflopActionUseCaseResponse
{
    public ResponseAction ResponseAction { get; set; } = new ResponseAction();
    public PlayerGameState ScrapeResult { get; set; } = new PlayerGameState();
}

public interface ISetPreflopActionUseCase
{
    Task<SetPreflopActionUseCaseResponse> Execute(SetPreflopActionUseCaseRequest request);
}
