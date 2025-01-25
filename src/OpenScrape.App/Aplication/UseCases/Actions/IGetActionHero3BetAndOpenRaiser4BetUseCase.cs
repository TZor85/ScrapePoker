using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionHero3BetAndOpenRaiser4BetRequest : BaseRequest
{
    public TablePosition RaiserPosition { get; set; }
    public TablePosition ThreeBetPosition { get; set; }
    public bool? IsGreater { get; set; }
    public decimal BetSize { get; set; }
}

public class GetActionHero3BetAndOpenRaiser4BetResponse : BaseResponse
{
}

public interface IGetActionHero3BetAndOpenRaiser4BetUseCase
{
    Task<GetActionHero3BetAndOpenRaiser4BetResponse> Execute(GetActionHero3BetAndOpenRaiser4BetRequest request);
}
