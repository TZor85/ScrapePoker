using OpenScrape.App.Entities;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{

    public class SetPreflopActionUseCaseRequest
    {
        public ResponseAction ResponseAction { get; set; } = new ResponseAction();
        public TableScrapeResult ScrapeResult { get; set; } = new TableScrapeResult();

        public Dictionary<HeroPosition, Dictionary<HeroPosition, decimal>> PreflopHeroPosition = new Dictionary<HeroPosition, Dictionary<HeroPosition, decimal>>();
    }

    public class SetPreflopActionUseCaseResponse
    {
        public ResponseAction ResponseAction { get; set; } = new ResponseAction();
        public TableScrapeResult ScrapeResult { get; set; } = new TableScrapeResult();
    }

    public interface ISetPreflopActionUseCase
    {
        SetPreflopActionUseCaseResponse Execute(SetPreflopActionUseCaseRequest request);
    }
}
