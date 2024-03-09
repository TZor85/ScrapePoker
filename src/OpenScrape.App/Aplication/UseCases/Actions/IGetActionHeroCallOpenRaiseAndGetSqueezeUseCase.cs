using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{

    public class GetActionHeroCallOpenRaiseAndGetSqueezeRequest : BaseRequest
    {
        public HeroPosition SqueezerPosition { get; set; }
        public HeroPosition RaiserPosition { get; set; }
        public bool RaiserCall { get; set; }
    }

    public class GetActionHeroCallOpenRaiseAndGetSqueezeResponse : BaseResponse
    {
    }

    public interface IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase
    {
        GetActionHeroCallOpenRaiseAndGetSqueezeResponse Execute(GetActionHeroCallOpenRaiseAndGetSqueezeRequest request);
    }
}
