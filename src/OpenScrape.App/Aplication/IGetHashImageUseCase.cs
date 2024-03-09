namespace OpenScrape.App.Aplication
{
    public class GetHashImageUseCaseRequest
    {
        public Bitmap? Image { get; set; }
    }

    public class GetHashImageUseCaseResponse
    {
        public string Hash { get; set; } = string.Empty;
    }

    public interface IGetHashImageUseCase
    {
        GetHashImageUseCaseResponse Execute(GetHashImageUseCaseRequest request);
    }
}
