using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{

    public class GetActionVs3BetAndCallUseCaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
        public HeroPosition VillainPosition { get; set; }
        public HeroPosition CallerPosition { get; set; }
    }

    public class GetActionVs3BetAndCallUseCaseResponse : BaseResponse
    {

    }

    public interface IGetActionVs3BetAndCallUseCase
    {
        GetActionVs3BetAndCallUseCaseResponse Execute(GetActionVs3BetAndCallUseCaseRequest request);
    }
}
