using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionHero3BetAndOpenRaiser4BetRequest : BaseRequest
    {
        public HeroPosition RaiserPosition { get; set; }
        public decimal BetSize { get; set; }
    }

    public class GetActionHero3BetAndOpenRaiser4BetResponse : BaseResponse
    {
    }

    public interface IGetActionHero3BetAndOpenRaiser4BetUseCase
    {
        GetActionHero3BetAndOpenRaiser4BetResponse Execute(GetActionHero3BetAndOpenRaiser4BetRequest request);
    }
}
