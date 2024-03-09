namespace OpenScrape.App.Aplication
{
    public class GetCropImageUseCase : IGetCropImageUseCase
    {
        public GetCropImageUseCaseResponse Execute(GetCropImageUseCaseRequest request)
        {
            Bitmap bmp = new Bitmap(request.Section.Width, request.Section.Height);
            Graphics g = Graphics.FromImage(bmp);

            g.DrawImage(request.Source!, 0, 0, request.Section, GraphicsUnit.Pixel);

            return new GetCropImageUseCaseResponse { Image = bmp };
        }
    }
}
