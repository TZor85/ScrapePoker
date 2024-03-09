namespace OpenScrape.App.Aplication.UseCases.Actions
{

    public class GetActionRaiseOverLimperUseCaseRequest : BaseRequest { }

    public class GetActionRaiseOverLimperUseCaseResponse : BaseResponse { }
    public interface IGetActionRaiseOverLimperUseCase
    {
        GetActionRaiseOverLimperUseCaseResponse Execute(GetActionRaiseOverLimperUseCaseRequest request);
    }
}
