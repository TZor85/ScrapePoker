using System.Drawing;
using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetCardsFlopUseCase : IGetCardsFlopUseCase
    {
        private readonly ICardRecognizer _cardRecognizer;
        private readonly ICoordinateScaler _coordinateScaler;

        public GetCardsFlopUseCase(ICardRecognizer cardRecognizer, ICoordinateScaler coordinateScaler)
        {
            _cardRecognizer = cardRecognizer;
            _coordinateScaler = coordinateScaler;
        }

        public Task<GetCardsFlopUseCaseResponse> ExecuteAsync(GetCardsFlopUseCaseRequest request)
        {
            var response = new GetCardsFlopUseCaseResponse();

            var regionTableMap = request.RegionsTableMap?.FirstOrDefault(f => f.Id == "Board");
            if (regionTableMap == null || regionTableMap.Regions == null || request.Image is not Bitmap bitmap)
                return Task.FromResult(response);

            foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
            {
                var (x, y, width, height) = ScaleCoordinates(
                    region.PosX, region.PosY, region.Width, region.Height,
                    request.CurrentImageWidth, request.CurrentImageHeight);

                var match = _cardRecognizer.Recognize(bitmap, x, y, width, height);
                var card = match.Card ?? new CardDTO { Name = string.Empty };

                var name = string.Empty;
                var force = 0;
                var suit = 0;
                var location = 0;

                switch (region.Name)
                {
                    case "Card1":
                        name = card.Name.Split(" ")[0];
                        force = card.Force;
                        suit = card.Suit;
                        location = 1;
                        break;
                    case "Card2":
                        name = card.Name.Split(" ")[0];
                        force = card.Force;
                        suit = card.Suit;
                        location = 2;
                        break;
                    case "Card3":
                        name = card.Name.Split(" ")[0];
                        force = card.Force;
                        suit = card.Suit;
                        location = 3;
                        break;
                    default:
                        break;
                }

                if (response.DataBoard.Count < 3)
                {
                    response.DataBoard.Add(new BoardData
                    {
                        Name = name,
                        Force = force,
                        Suit = suit,
                        Position = BoardPosition.Flop,
                        Location = location
                    });
                }
            }

            return Task.FromResult(response);
        }

        private (int X, int Y, int Width, int Height) ScaleCoordinates(
            int posX, int posY, int width, int height,
            int currentWidth, int currentHeight)
        {
            if (currentWidth <= 0 || currentHeight <= 0 || !_coordinateScaler.IsInitialized)
            {
                return (posX, posY, width, height);
            }

            return _coordinateScaler.ScaleRegion(posX, posY, width, height, currentWidth, currentHeight);
        }
    }
}
