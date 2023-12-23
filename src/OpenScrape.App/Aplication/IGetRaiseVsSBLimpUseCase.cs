using OpenScrape.App.Aplication.UseCases;

namespace OpenScrape.App.Aplication
{

    public class GetRaiseVsSBLimpRequest : BaseRequest 
    {
        public bool OnlyCall { get; set; }
    }

    public class GetRaiseVsSBLimpResponse : BaseResponse { }

    public interface IGetRaiseVsSBLimpUseCase
    {
        GetRaiseVsSBLimpResponse Execute(GetRaiseVsSBLimpRequest request);
    }
}
