using OpenScrape.App.Entities;
using OpenScrape.App.Enums;
using OpenScrape.App.Helpers;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetCardsFlopUseCase : IGetCardsFlopUseCase
    {
        readonly IGetHashImageUseCase _getHashImageUseCase;
        readonly IGetCropImageUseCase _getCropImageUseCase;

        public GetCardsFlopUseCase(IGetHashImageUseCase getHashImageUseCase, IGetCropImageUseCase getCropImageUseCase)
        {
            _getHashImageUseCase = getHashImageUseCase;
            _getCropImageUseCase = getCropImageUseCase;
        }

        public GetCardsFlopUseCaseResponse Execute(GetCardsFlopUseCaseRequest request)
        {
            var response = new GetCardsFlopUseCaseResponse();

            foreach (var item in request.Regions.Where(x => x.IsHash && !x.Name.Contains("u0card")))
            {
                var maxEqual = 0;
                var max = 0;

                var name = string.Empty;
                var force = 0;
                var suit = 0;

                var imageBmp = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(request.Image), Section = new Rectangle(item.X, item.Y, item.Width, item.Height) }).Image;
                string iHash1 = _getHashImageUseCase
                                .Execute(new GetHashImageUseCaseRequest { Image = CaptureWindowsHelper.BinaryImage(imageBmp, 130) }).Hash;

                foreach (var image in request.ImageRegions)
                {
                    int equalElements = iHash1.Zip(image.Value, (i, j) => i == j).Count(eq => eq);

                    if (equalElements > maxEqual)
                        maxEqual = equalElements;

                    if (maxEqual > max && maxEqual >= (700 * 0.9))
                    {
                        switch (item.Name)
                        {
                            case "b0card1":
                                name = image.Name.Split(" ")[0];
                                force = image.Force;
                                suit = image.Suit;

                                max = maxEqual;
                                break;
                            case "b0card2":
                                name = image.Name.Split(" ")[0];
                                force = image.Force;
                                suit = image.Suit;

                                max = maxEqual;
                                break;
                            case "b0card3":
                                name = image.Name.Split(" ")[0];
                                force = image.Force;
                                suit = image.Suit;

                                max = maxEqual;
                                break;
                            default:
                                break;
                        }
                    }
                }

                response.DataBoard.Add(new BoardData
                {
                    Name = name,
                    Force = force,
                    Suit = suit,
                    Position = BoardPosition.Flop
                });

            }

            return response;
        }
    }
}
