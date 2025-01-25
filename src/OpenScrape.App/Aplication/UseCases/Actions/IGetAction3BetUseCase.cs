using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetAction3BetUseCaseRequest
{
    public string Hand { get; set; } = default!;
    public TablePosition Position { get; set; }
    public TablePosition OpenRaiserPosition { get; set; }
}

public class GetAction3BetUseCaseResponse : BaseResponse
{

}

public interface IGetAction3BetUseCase
{
    Task<GetAction3BetUseCaseResponse> Execute(GetAction3BetUseCaseRequest request);
}
