using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetAction3BetUseCaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
        public HeroPosition VillainPosition { get; set; }
    }

    public class GetAction3BetUseCaseResponse : BaseResponse
    {

    }

    public interface IGetAction3BetUseCase
    {
        GetAction3BetUseCaseResponse Execute(GetAction3BetUseCaseRequest request);
    }
}
