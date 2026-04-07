using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionRaiseOverLimperUseCaseRequest : BaseRequest
{
    public TablePosition? LimperPosition { get; set; }
}

public class GetActionRaiseOverLimperUseCaseResponse : BaseResponse { }

public interface IGetActionRaiseOverLimperUseCase
{
    Task<GetActionRaiseOverLimperUseCaseResponse> Execute(GetActionRaiseOverLimperUseCaseRequest request);
}
