using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication;

public class GetCardsRiverUseCaseRequest : BaseRequest
{
    public List<BoardData>? DataBoard { get; set; }
    public Image? Image { get; set; }
    public List<RegionTableMap> RegionsTableMap { get; set; } = new List<RegionTableMap>();
}

public class GetCardsRiverUseCaseResponse : BaseResponse
{
    public List<BoardData> DataBoard { get; set; } = new List<BoardData>();
}

public interface IGetCardsRiverUseCase
{
    Task<GetCardsRiverUseCaseResponse> ExecuteAsync(GetCardsRiverUseCaseRequest request);
}
