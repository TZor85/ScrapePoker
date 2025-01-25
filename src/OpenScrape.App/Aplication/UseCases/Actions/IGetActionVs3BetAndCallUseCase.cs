using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions;


public class GetActionVs3BetAndCallUseCaseRequest : BaseRequest
{
    public TablePosition ThreeBetPosition { get; set; }
    public TablePosition CallerPosition { get; set; }
}

public class GetActionVs3BetAndCallUseCaseResponse : BaseResponse { }

public interface IGetActionVs3BetAndCallUseCase
{
    Task<GetActionVs3BetAndCallUseCaseResponse> Execute(GetActionVs3BetAndCallUseCaseRequest request);
}
