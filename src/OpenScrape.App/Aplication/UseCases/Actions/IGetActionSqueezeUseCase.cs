using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionSqueezeRequest : BaseRequest
{
    public TablePosition OpenRaiserPosition { get; set; }
    public TablePosition CallerPosition { get; set; }
}

public class GetActionSqueezeResponse : BaseResponse
{
}


public interface IGetActionSqueezeUseCase
{
    Task<GetActionSqueezeResponse> Execute(GetActionSqueezeRequest request);
}
