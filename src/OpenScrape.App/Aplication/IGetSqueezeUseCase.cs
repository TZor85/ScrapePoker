using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{
    public class GetSqueezeRequest : BaseRequest
    {
        public HeroPosition RaiserPosition { get; set; }
        public HeroPosition CallerPosition { get; set; }
    }

    public class GetSqueezeResponse : BaseResponse
    {     
    }


    public interface IGetSqueezeUseCase
    {
        GetSqueezeResponse Execute(GetSqueezeRequest request);
    }
}
