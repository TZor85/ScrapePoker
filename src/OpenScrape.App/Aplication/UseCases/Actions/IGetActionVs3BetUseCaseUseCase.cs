using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionVs3BetUseCaseRequest : BaseRequest
    {
        public TablePosition ThreeBetPosition { get; set; }
    }

    public class GetActionVs3BetUseCaseResponse : BaseResponse
    {

    }

    public interface IGetActionVs3BetUseCaseUseCase
    {
        Task<GetActionVs3BetUseCaseResponse> Execute(GetActionVs3BetUseCaseRequest request);
    }
}
