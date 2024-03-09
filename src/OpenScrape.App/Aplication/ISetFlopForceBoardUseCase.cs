using OpenScrape.App.Entities;

namespace OpenScrape.App.Aplication
{
    public class SetFlopForceBoardUseCaseRequest
    {
        public TableScrapeResult TableScrapeResult { get; set; } = new TableScrapeResult();
    }

    public class SetFlopForceBoardUseCaseResponse
    {
        public TableScrapeResult TableScrapeResult { get; set; } = new TableScrapeResult();
    }

    public interface ISetFlopForceBoardUseCase
    {
        SetFlopForceBoardUseCaseResponse Execute(SetFlopForceBoardUseCaseRequest request);
    }
}
