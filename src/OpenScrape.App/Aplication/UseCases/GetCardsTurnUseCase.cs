using Marten;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Mappers;

namespace OpenScrape.App.Aplication.UseCases;

public class GetCardsTurnUseCase : IGetCardsTurnUseCase
{
    private List<CardDTO>? _cardsImages;

    private readonly IDocumentStore _dataBase;
    private ImageCropperService _imageCropperService = new();

    public GetCardsTurnUseCase(IDocumentStore database)
    {
        _dataBase = database;
    }

    public async Task<GetCardsTurnUseCaseResponse> ExecuteAsync(GetCardsTurnUseCaseRequest request)
    {
        var response = new GetCardsTurnUseCaseResponse();

        var session = _dataBase.LightweightSession();
        var regionTableMap = request.RegionsTableMap?.FirstOrDefault(f => f.Id == "Board");
        if (regionTableMap == null || regionTableMap.Regions == null || request.Image == null)
            return response;

        foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
        {
            var imageToBase64 = _imageCropperService.CropImageToBase64(request.Image, region.PosX, region.PosY, region.Width, region.Height);

            if (_cardsImages == null)
            {
                _cardsImages = new List<CardDTO>(); // Fixed: Initialize the list properly
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
                    case "Card4":
                        name = card.Name.Split(" ")[0];
                        force = card.Force;
                        suit = card.Suit;
                        location = 4;
                        break;
                    default:
                        break;
                }

                if (region.Name == "Card4")
                {
                    response.DataBoard = request.DataBoard ?? new List<BoardData>();

                    if (response.DataBoard?.Where(w => w.Position == BoardPosition.Flop).ToList().Count == 3)
                    {
                        response.DataBoard.Add(new BoardData
                        {
                            Name = name,
                            Force = force,
                            Suit = suit,
                            Position = BoardPosition.Turn,
                            Location = location
                        });
                    }
                }
            }
        }

        return response;
    }
}
