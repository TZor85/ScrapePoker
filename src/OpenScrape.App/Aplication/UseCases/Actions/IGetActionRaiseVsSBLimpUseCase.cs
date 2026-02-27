using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions;

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
