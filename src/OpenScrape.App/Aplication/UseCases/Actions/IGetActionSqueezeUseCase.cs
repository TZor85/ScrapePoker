using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionSqueezeRequest : BaseRequest
    {
        public HeroPosition RaiserPosition { get; set; }
        public HeroPosition CallerPosition { get; set; }
    }

    public class GetActionSqueezeResponse : BaseResponse
    {
    }


    public interface IGetActionSqueezeUseCase
    {
        GetActionSqueezeResponse Execute(GetActionSqueezeRequest request);
    }
}
