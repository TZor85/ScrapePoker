using OpenScrape.App.Aplication.UseCases;

namespace OpenScrape.App.Aplication
{

    public class GetRaiseVsSBLimpRequest : BaseRequest { }

    public class GetRaiseVsSBLimpResponse : BaseResponse { }

    public interface IGetRaiseVsSBLimp
    {
        GetRaiseVsSBLimpResponse Execute(GetRaiseVsSBLimpRequest request);
    }
}
