using OpenScrape.App.Entities;

namespace OpenScrape.App.Aplication
{
    public class SetFlopForceBoardUseCaseRequest
    {
        public TableScrapeResult TableScrapeResult { get; set; } = new TableScrapeResult();
        public TableScrapeFlopResult TableScrapeFlopResult { get; set; } = new TableScrapeFlopResult();
    }

    public class SetFlopForceBoardUseCaseResponse
    {
        public TableScrapeResult TableScrapeResult { get; set; } = new TableScrapeResult();
        public TableScrapeFlopResult TableScrapeFlopResult { get; set; } = new TableScrapeFlopResult();
    }

    public interface ISetFlopForceBoardUseCase
    {
        SetFlopForceBoardUseCaseResponse Execute(SetFlopForceBoardUseCaseRequest request);
    }
}
