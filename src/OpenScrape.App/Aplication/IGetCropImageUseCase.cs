namespace OpenScrape.App.Aplication
{
    public class GetCropImageUseCaseRequest
    {
        public Bitmap? Source { get; set; }
        public Rectangle Section { get; set; }

    }

    public class GetCropImageUseCaseResponse
    {
        public Bitmap Image { get; set; } = new Bitmap(1, 1);
    }

    public interface IGetCropImageUseCase
    {
        GetCropImageUseCaseResponse Execute(GetCropImageUseCaseRequest request);
    }
}
