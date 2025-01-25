using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication;


public class SetPreflopActionUseCaseRequest
{
    public ResponseAction ResponseAction { get; set; } = new ResponseAction();
    public TableScrapeResult ScrapeResult { get; set; } = new TableScrapeResult();

    public Dictionary<TablePosition, Dictionary<TablePosition, decimal>> PreflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
}

public class SetPreflopActionUseCaseResponse
{
    public ResponseAction ResponseAction { get; set; } = new ResponseAction();
    public TableScrapeResult ScrapeResult { get; set; } = new TableScrapeResult();
}

public interface ISetPreflopActionUseCase
{
    Task<SetPreflopActionUseCaseResponse> Execute(SetPreflopActionUseCaseRequest request);
}
