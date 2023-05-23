using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Enums;


namespace OpenScrape.App.Aplication
{
    public class GetOpenRaiseUseCaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
    }

    public class GetOpenRaiseUseCaseResponse : BaseResponse { }

    public interface IGetOpenRaiseUseCase
    {
        GetOpenRaiseUseCaseResponse Execute(GetOpenRaiseUseCaseRequest request);
    }
}
