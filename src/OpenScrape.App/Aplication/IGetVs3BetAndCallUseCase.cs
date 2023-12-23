using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{

    public class GetVs3BetAndCallUseCaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
        public HeroPosition VillainPosition { get; set; }
        public HeroPosition CallerPosition { get; set; }
    }

    public class GetVs3BetAndCallUseCaseResponse : BaseResponse
    {

    }

    public interface IGetVs3BetAndCallUseCase
    {
        GetVs3BetAndCallUseCaseResponse Execute(GetVs3BetAndCallUseCaseRequest request);
    }
}
