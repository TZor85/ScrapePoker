using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop;


public class SetPreflopActionUseCaseRequest
{
    public ResponseAction ResponseAction { get; set; } = new ResponseAction();
    public PlayerGameState PlayerState { get; set; } = new PlayerGameState();

    public Dictionary<TablePosition, Dictionary<TablePosition, decimal>> PreflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
}

public class SetPreflopActionUseCaseResponse
{
    public ResponseAction ResponseAction { get; set; } = new ResponseAction();
    public PlayerGameState PlayerState { get; set; } = new PlayerGameState();
}

public interface ISetPreflopActionUseCase
{
    Task<SetPreflopActionUseCaseResponse> Execute(SetPreflopActionUseCaseRequest request);
}
