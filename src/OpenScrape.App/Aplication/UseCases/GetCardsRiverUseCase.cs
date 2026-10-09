using System.Drawing;
using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases;

public class GetCardsRiverUseCase : IGetCardsRiverUseCase
{
    private readonly ICardRecognizer _cardRecognizer;
    private readonly ICoordinateScaler _coordinateScaler;

    public GetCardsRiverUseCase(ICardRecognizer cardRecognizer, ICoordinateScaler coordinateScaler)
    {
        _cardRecognizer = cardRecognizer;
        _coordinateScaler = coordinateScaler;
    }

    public Task<GetCardsRiverUseCaseResponse> ExecuteAsync(GetCardsRiverUseCaseRequest request)
    {
        var response = new GetCardsRiverUseCaseResponse();

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
                case "Card5":
                    name = card.Name.Split(" ")[0];
                    force = card.Force;
                    suit = card.Suit;
                    location = 5;
                    break;
                default:
                    break;
            }

            if (region.Name == "Card5")
            {
                response.DataBoard = request.DataBoard ?? new List<BoardData>();

                if (response.DataBoard?.Where(w => w.Position == BoardPosition.Turn).ToList().Count == 1)
                {
                    response.DataBoard.Add(new BoardData
                    {
                        Name = name,
                        Force = force,
                        Suit = suit,
                        Position = BoardPosition.River,
                        Location = location
                    });
                }
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
