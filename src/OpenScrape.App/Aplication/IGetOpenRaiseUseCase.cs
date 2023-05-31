using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;


namespace OpenScrape.App.Aplication
{
    public class GetOpenRaiseUseCaseRequest : BaseRequest
    {
        
    }

    public class GetOpenRaiseUseCaseResponse : BaseResponse { }

    public interface IGetOpenRaiseUseCase
    {
        GetOpenRaiseUseCaseResponse Execute(GetOpenRaiseUseCaseRequest request);
    }
}
