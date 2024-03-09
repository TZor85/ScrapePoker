using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{

    public class GetActionCold4BetUseCaseRequest : BaseRequest
    {
        public HeroPosition RaiserPosition { get; set; }
        public HeroPosition ThreeBetVillainPosition { get; set; }
    }

    public class GetActionCold4BetUseCaseResponse : BaseResponse
    {

    }

    public interface IGetActionCold4BetUseCase
    {
        GetActionCold4BetUseCaseResponse Execute(GetActionCold4BetUseCaseRequest request);
    }
}
