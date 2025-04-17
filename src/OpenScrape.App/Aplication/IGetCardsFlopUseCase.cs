using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Models;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Aplication;

public class GetCardsFlopUseCaseRequest : BaseRequest
{
    //public List<Domain.ValueObjects.Region> Regions { get; set; } = new List<Domain.ValueObjects.Region>();
    //public List<ImageRegion> ImageRegions { get; set; } = new List<ImageRegion>();
    public Image? Image { get; set; }
    public List<RegionTableMap> RegionsTableMap { get; set; } = new List<RegionTableMap>();
}

public class GetCardsFlopUseCaseResponse : BaseResponse
{
    public List<BoardData> DataBoard { get; set; } = new List<BoardData>();
}

public interface IGetCardsFlopUseCase
{
    Task<GetCardsFlopUseCaseResponse> ExecuteAsync(GetCardsFlopUseCaseRequest request);
}
