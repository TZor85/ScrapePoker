using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases
{

    public class GetCold4BetUseCaseRequest : BaseRequest
    {
        public HeroPosition RaiserPosition { get; set; }
        public HeroPosition ThreeBetVillainPosition { get; set; }
    }

    public class GetCold4BetUseCaseResponse : BaseResponse
    {

    }

    public interface IGetCold4BetUseCase
    {
        GetCold4BetUseCaseResponse Execute(GetCold4BetUseCaseRequest request);
    }
}
