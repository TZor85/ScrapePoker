using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop;

public class GetActionRaiseVsSBLimpRequest : BaseRequest
{
    public TablePosition? LimperPosition { get; set; }
    public TablePosition? ThreeBetPosition { get; set; }
}

public class GetActionRaiseVsSBLimpResponse : BaseResponse { }

public interface IGetActionRaiseVsSBLimpUseCase
{
    Task<GetActionRaiseVsSBLimpResponse> Execute(GetActionRaiseVsSBLimpRequest request);
}
