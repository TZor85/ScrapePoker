using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetCardsFlopUseCase : IGetCardsFlopUseCase
    {
        private readonly CardCacheService _cardCache;
        private readonly ImageCropperService _imageCropperService;
        private readonly ICoordinateScaler _coordinateScaler;

        public GetCardsFlopUseCase(CardCacheService cardCache, ImageCropperService imageCropperService, ICoordinateScaler coordinateScaler)
        {
            _cardCache = cardCache;
            _imageCropperService = imageCropperService;
            _coordinateScaler = coordinateScaler;
        }

        public async Task<GetCardsFlopUseCaseResponse> ExecuteAsync(GetCardsFlopUseCaseRequest request)
        {
            var response = new GetCardsFlopUseCaseResponse();

            var regionTableMap = request.RegionsTableMap?.FirstOrDefault(f => f.Id == "Board");
            if (regionTableMap == null || regionTableMap.Regions == null || request.Image == null)
                return response;

            var cardsImages = await _cardCache.GetCardsAsync();

            foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
            {
                var (x, y, width, height) = ScaleCoordinates(
                    region.PosX, region.PosY, region.Width, region.Height,
                    request.CurrentImageWidth, request.CurrentImageHeight);

                var imageToBase64 = _imageCropperService.CropImageToBase64(request.Image, x, y, width, height);

                var maxPorcentaje = 0.0;
                var card = new CardDTO { Name = string.Empty };

                var name = string.Empty;
                var force = 0;
                var suit = 0;
                var location = 0;

                foreach (var item in cardsImages)
                {
                    if (!string.IsNullOrEmpty(item.ImageBase64))
                    {
                        var pocentaje = _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64);

                        if (pocentaje > maxPorcentaje)
                        {
                            maxPorcentaje = pocentaje;
                            card = item;
                        }
                    }
                }

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

            return response;
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
