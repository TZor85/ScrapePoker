using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{

    public class GetHeroCallOpenRaiseAndGetSqueezeRequest : BaseRequest
    {
        public HeroPosition SqueezerPosition { get; set; }
        public HeroPosition RaiserPosition { get; set; }
        public bool RaiserCall { get; set; }
    }

    public class GetHeroCallOpenRaiseAndGetSqueezeResponse : BaseResponse
    {        
    }

    public interface IGetHeroCallOpenRaiseAndGetSqueezeUseCase
    {
        GetHeroCallOpenRaiseAndGetSqueezeResponse Execute(GetHeroCallOpenRaiseAndGetSqueezeRequest request);
    }
}
