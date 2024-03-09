namespace OpenScrape.App.Aplication
{
    public class GetHashImageUseCase : IGetHashImageUseCase
    {
        public GetHashImageUseCaseResponse Execute(GetHashImageUseCaseRequest request)
        {
            List<bool> lResult = new List<bool>();
            var textHash = string.Empty;
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(request.Image!, new Size(20, 35));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }

            foreach (var item in lResult)
            {
                if (item)
                    textHash += "1";
                else
                    textHash += "0";
            }

            return new GetHashImageUseCaseResponse { Hash = textHash };
        }
    }
    
}
