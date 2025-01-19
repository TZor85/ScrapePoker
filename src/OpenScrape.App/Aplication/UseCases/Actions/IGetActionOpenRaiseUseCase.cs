namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionOpenRaiseUseCaseRequest : BaseRequest
    {

    }

    public class GetActionOpenRaiseUseCaseResponse : BaseResponse { }

    public interface IGetActionOpenRaiseUseCase
    {
        Task<GetActionOpenRaiseUseCaseResponse> Execute(GetActionOpenRaiseUseCaseRequest request);
    }
}
