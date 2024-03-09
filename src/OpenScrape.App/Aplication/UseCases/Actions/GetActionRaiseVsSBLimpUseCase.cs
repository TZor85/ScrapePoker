using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionRaiseVsSBLimpUseCase : IGetActionRaiseVsSBLimpUseCase
    {
        public GetActionRaiseVsSBLimpResponse Execute(GetActionRaiseVsSBLimpRequest request)
        {
            var response = new GetActionRaiseVsSBLimpResponse();

            if (request.OnlyCall)
                response.Action = RaiseVsSBLimp.GetBigBlindvsSBCall(request.Hand);
            else
                response.Action = RaiseVsSBLimp.GetBigBlindvsSBCallAndRaise(request.Hand);


            return response;
        }
    }
}
