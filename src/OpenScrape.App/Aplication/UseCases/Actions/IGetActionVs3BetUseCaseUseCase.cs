using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionVs3BetUseCaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
        public HeroPosition VillainPosition { get; set; }
    }

    public class GetActionVs3BetUseCaseResponse : BaseResponse
    {

    }

    public interface IGetActionVs3BetUseCaseUseCase
    {
        GetActionVs3BetUseCaseResponse Execute(GetActionVs3BetUseCaseRequest request);
    }
}
