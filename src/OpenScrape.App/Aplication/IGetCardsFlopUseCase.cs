using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Models;

namespace OpenScrape.App.Aplication
{
    public class GetCardsFlopUseCaseRequest : BaseRequest
    {
        public List<Regions> Regions { get; set; } = new List<Regions>();
        public List<ImageRegion> ImageRegions { get; set; } = new List<ImageRegion>();
        public Image? Image { get; set; }
    }

    public class GetCardsFlopUseCaseResponse : BaseResponse
    {
        public List<BoardData> DataBoard { get; set; } = new List<BoardData>();
    }

    public interface IGetCardsFlopUseCase
    {
        GetCardsFlopUseCaseResponse Execute(GetCardsFlopUseCaseRequest request);
    }
}
