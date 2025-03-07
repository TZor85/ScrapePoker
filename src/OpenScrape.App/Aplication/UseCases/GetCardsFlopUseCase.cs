using Marten;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Mappers;
using System.Windows.Forms;
using System.Xml.Linq;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetCardsFlopUseCase : IGetCardsFlopUseCase
    {
        private List<CardDTO>? _cardsImages;

        private readonly IDocumentStore _dataBase;
        readonly IGetHashImageUseCase _getHashImageUseCase;
        readonly IGetCropImageUseCase _getCropImageUseCase;
        private ImageCropperService _imageCropperService = new();

        public GetCardsFlopUseCase(IDocumentStore database, IGetHashImageUseCase getHashImageUseCase, IGetCropImageUseCase getCropImageUseCase)
        {
            _getHashImageUseCase = getHashImageUseCase;
            _getCropImageUseCase = getCropImageUseCase;
            _dataBase = database;
        }

        public async Task<GetCardsFlopUseCaseResponse> Execute(GetCardsFlopUseCaseRequest request)
        {
            var response = new GetCardsFlopUseCaseResponse();

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
                        case "Card1":
                            name = card.Name.Split(" ")[0];
                            force = card.Force;
                            suit = card.Suit;
                            break;
                        case "Card2":
                            name = card.Name.Split(" ")[0];
                            force = card.Force;
                            suit = card.Suit;
                            break;
                        case "Card3":
                            name = card.Name.Split(" ")[0];
                            force = card.Force;
                            suit = card.Suit;
                            break;
                        default:
                            break;
                    }
                    

                    response.DataBoard.Add(new BoardData
                    {
                        Name = name,
                        Force = force,
                        Suit = suit,
                        Position = BoardPosition.Flop
                    });
                }
            }


            //foreach (var item in request.Regions)
            //{
            //    var maxEqual = 0;
            //    var max = 0;

            //    var name = string.Empty;
            //    var force = 0;
            //    var suit = 0;

            //    var imageBmp = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(request.Image), Section = new Rectangle(item.PosX, item.PosY, item.Width, item.Height) }).Image;
            //    string iHash1 = _getHashImageUseCase
            //                    .Execute(new GetHashImageUseCaseRequest { Image = CaptureWindowsHelper.BinaryImage(imageBmp, 130) }).Hash;

            //    foreach (var image in request.ImageRegions)
            //    {
            //        int equalElements = iHash1.Zip(image.Value, (i, j) => i == j).Count(eq => eq);

            //        if (equalElements > maxEqual)
            //            maxEqual = equalElements;

            //        if (maxEqual > max && maxEqual >= (700 * 0.9))
            //        {
            //            switch (item.Name)
            //            {
            //                case "b0card1":
            //                    name = image.Name.Split(" ")[0];
            //                    force = image.Force;
            //                    suit = image.Suit;

            //                    max = maxEqual;
            //                    break;
            //                case "b0card2":
            //                    name = image.Name.Split(" ")[0];
            //                    force = image.Force;
            //                    suit = image.Suit;

            //                    max = maxEqual;
            //                    break;
            //                case "b0card3":
            //                    name = image.Name.Split(" ")[0];
            //                    force = image.Force;
            //                    suit = image.Suit;

            //                    max = maxEqual;
            //                    break;
            //                default:
            //                    break;
            //            }
            //        }
            //    }

            

            //}

            return response;
        }
    }
}
