using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetOpenRaiseUseCase : IGetOpenRaiseUseCase
    {
        public GetOpenRaiseUseCaseResponse Execute(GetOpenRaiseUseCaseRequest request)
        {
            var response = new GetOpenRaiseUseCaseResponse();

            response.Action = request.Position switch
            {
                HeroPosition.SmallBlind => OpenRaises.GetSmallBlindAction(request.Hand),
                HeroPosition.Button => OpenRaises.GetButtonAction(request.Hand),
                HeroPosition.CutOff => OpenRaises.GetCutOffAction(request.Hand),
                HeroPosition.MiddlePosition => OpenRaises.GetMiddleAction(request.Hand),
                HeroPosition.EarlyPosition => OpenRaises.GetEarlyAction(request.Hand),
                _ => string.Empty
            };

            return response;
        }
    }
}
