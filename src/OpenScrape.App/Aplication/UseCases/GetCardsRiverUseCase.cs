using Marten;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Mappers;

namespace OpenScrape.App.Aplication.UseCases;

public class GetCardsRiverUseCase : IGetCardsRiverUseCase
{
    private List<CardDTO>? _cardsImages;

    private readonly IDocumentStore _dataBase;
    private ImageCropperService _imageCropperService = new();

    public GetCardsRiverUseCase(IDocumentStore database)
    {
        _dataBase = database;
    }

    public async Task<GetCardsRiverUseCaseResponse> ExecuteAsync(GetCardsRiverUseCaseRequest request)
    {
        var response = new GetCardsRiverUseCaseResponse();

        var session = _dataBase.LightweightSession();
        var regionTableMap = request.RegionsTableMap?.FirstOrDefault(f => f.Id == "Board");
        if (regionTableMap == null || regionTableMap.Regions == null || request.Image == null)
            return response;

        foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
        {
            var imageToBase64 = _imageCropperService.CropImageToBase64(request.Image, region.PosX, region.PosY, region.Width, region.Height);

            if (_cardsImages == null)
            {
                _cardsImages = [];
                var cards = await session.Query<Card>().ToListAsync();
                foreach (var item in cards)
                {
                    _cardsImages.Add(item.ToDto());
                }
            }

            if (_cardsImages != null)
            {
                var maxPorcentaje = 0.0;
                var card = new CardDTO { Name = string.Empty };

                var name = string.Empty;
                var force = 0;
                var suit = 0;
                var location = 0;

                foreach (var item in _cardsImages)
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
                    response.DataBoard = request.DataBoard ?? new List<BoardData>(); ;

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
        }

        return response;
    }
}

