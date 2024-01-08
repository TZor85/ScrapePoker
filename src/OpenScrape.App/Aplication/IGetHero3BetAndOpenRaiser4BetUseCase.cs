using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{
    public class GetHero3BetAndOpenRaiser4BetRequest : BaseRequest
    {
        public HeroPosition RaiserPosition { get; set; }
        public decimal BetSize { get; set; }
    }

    public class GetHero3BetAndOpenRaiser4BetResponse : BaseResponse
    {        
    }

    public interface IGetHero3BetAndOpenRaiser4BetUseCase
    {
        GetHero3BetAndOpenRaiser4BetResponse Execute(GetHero3BetAndOpenRaiser4BetRequest request);
    }
}
