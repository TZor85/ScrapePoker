using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop;

public class GetActionRaiseOverLimperUseCaseRequest : BaseRequest
{
    public TablePosition? LimperPosition { get; set; }
}

public class GetActionRaiseOverLimperUseCaseResponse : BaseResponse { }

public interface IGetActionRaiseOverLimperUseCase
{
    Task<GetActionRaiseOverLimperUseCaseResponse> Execute(GetActionRaiseOverLimperUseCaseRequest request);
}
