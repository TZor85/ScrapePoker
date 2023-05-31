using OpenScrape.App.Aplication.UseCases;

namespace OpenScrape.App.Aplication
{

    public class GetRaiseOverLimperUseCaseRequest : BaseRequest { }

    public class GetRaiseOverLimperUseCaseResponse : BaseResponse { }
    public interface IGetRaiseOverLimperUseCase
    {
        GetRaiseOverLimperUseCaseResponse Execute(GetRaiseOverLimperUseCaseRequest request);
    }
}
