using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
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
