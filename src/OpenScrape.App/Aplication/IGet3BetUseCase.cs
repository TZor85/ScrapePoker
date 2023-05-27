using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{
    public class Get3BetUseCaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
        public HeroPosition VillainPosition { get; set; }
    }

    public class Get3BetUseCaseResponse : BaseResponse
    {

    }

    public interface IGet3BetUseCase
    {
        Get3BetUseCaseResponse Execute(Get3BetUseCaseRequest request);
    }
}
