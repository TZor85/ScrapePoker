using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetRaiseVsSBLimpUseCase : IGetRaiseVsSBLimpUseCase
    {
        public GetRaiseVsSBLimpResponse Execute(GetRaiseVsSBLimpRequest request)
        {
            var response = new GetRaiseVsSBLimpResponse();

            if (request.OnlyCall)
                response.Action = RaiseVsSBLimp.GetBigBlindvsSBCall(request.Hand);
            else
                response.Action = RaiseVsSBLimp.GetBigBlindvsSBCallAndRaise(request.Hand);


            return response;
        }
    }
}
