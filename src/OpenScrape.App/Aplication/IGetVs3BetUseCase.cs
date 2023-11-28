using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{
    public class GetVs3BetUseCaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
        public HeroPosition VillainPosition { get; set; }
    }

    public class GetVs3BetUseCaseResponse : BaseResponse
    {

    }

    public interface IGetVs3BetUseCase
    {
        GetVs3BetUseCaseResponse Execute(GetVs3BetUseCaseRequest request);
    }
}
