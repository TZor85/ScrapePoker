using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop
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
