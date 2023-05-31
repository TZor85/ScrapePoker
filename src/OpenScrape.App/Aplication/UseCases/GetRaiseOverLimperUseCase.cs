using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetRaiseOverLimperUseCase : IGetRaiseOverLimperUseCase
    {
        public GetRaiseOverLimperUseCaseResponse Execute(GetRaiseOverLimperUseCaseRequest request)
        {
            var response = new GetRaiseOverLimperUseCaseResponse();

            response.Action = request.Position switch
            {
                HeroPosition.BigBlind => RaiseOverLimpers.GetBigBlindVsSmallBlindHands(request.Hand),
                HeroPosition.SmallBlind => RaiseOverLimpers.GetSmallBlindAction(request.Hand),
                HeroPosition.Button => RaiseOverLimpers.GetButtonAction(request.Hand),
                HeroPosition.CutOff => RaiseOverLimpers.GetCutOffAction(request.Hand),
                HeroPosition.MiddlePosition => RaiseOverLimpers.GetMiddleAction(request.Hand),
                _ => string.Empty
            };

            return response;
        }
    }
}
