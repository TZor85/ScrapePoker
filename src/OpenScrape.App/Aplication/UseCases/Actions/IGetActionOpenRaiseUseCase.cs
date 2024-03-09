namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionOpenRaiseUseCaseRequest : BaseRequest
    {

    }

    public class GetActionOpenRaiseUseCaseResponse : BaseResponse { }

    public interface IGetActionOpenRaiseUseCase
    {
        GetActionOpenRaiseUseCaseResponse Execute(GetActionOpenRaiseUseCaseRequest request);
    }
}
