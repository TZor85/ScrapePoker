using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions;


public class GetActionCold4BetUseCaseRequest : BaseRequest
{
    public TablePosition RaiserPosition { get; set; }
    public TablePosition ThreeBetPosition { get; set; }
}

public class GetActionCold4BetUseCaseResponse : BaseResponse { }

public interface IGetActionCold4BetUseCase
{
    Task<GetActionCold4BetUseCaseResponse> Execute(GetActionCold4BetUseCaseRequest request);
}
