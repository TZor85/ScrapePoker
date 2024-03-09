namespace OpenScrape.App.Aplication.UseCases.Actions
{

    public class GetActionRaiseVsSBLimpRequest : BaseRequest
    {
        public bool OnlyCall { get; set; }
    }

    public class GetActionRaiseVsSBLimpResponse : BaseResponse { }

    public interface IGetActionRaiseVsSBLimpUseCase
    {
        GetActionRaiseVsSBLimpResponse Execute(GetActionRaiseVsSBLimpRequest request);
    }
}
