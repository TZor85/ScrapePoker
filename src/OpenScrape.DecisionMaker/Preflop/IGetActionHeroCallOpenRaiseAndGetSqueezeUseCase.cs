using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop
{

    public class GetActionHeroCallOpenRaiseAndGetSqueezeRequest : BaseRequest
    {
        public TablePosition SqueezerPosition { get; set; }
        public TablePosition RaiserPosition { get; set; }
        public bool RaiserFolds { get; set; }
    }

    public class GetActionHeroCallOpenRaiseAndGetSqueezeResponse : BaseResponse
    {
    }

    public interface IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase
    {
        Task<GetActionHeroCallOpenRaiseAndGetSqueezeResponse> Execute(GetActionHeroCallOpenRaiseAndGetSqueezeRequest request);
    }
}
