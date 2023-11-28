using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class Get3BetUseCase : IGet3BetUseCase
    {
        public Get3BetUseCaseResponse Execute(Get3BetUseCaseRequest request)
        {
            var response = new Get3BetUseCaseResponse();

            var action = request.Position switch
            {
                HeroPosition.BigBlind =>
                    request.VillainPosition switch
                    {
                        HeroPosition.MiddlePosition =>
                            _3BetVsRaiser.Get3BetBBvsRaiseMP(request.Hand),
                        HeroPosition.CutOff =>
                            _3BetVsRaiser.Get3BetBBvsRaiseCO(request.Hand),
                        HeroPosition.Button =>
                            _3BetVsRaiser.Get3BetBBvsRaiseBTN(request.Hand),
                        HeroPosition.EarlyPosition =>
                            _3BetVsRaiser.Get3BetBBvsRaiseEP(request.Hand),
                        HeroPosition.SmallBlind =>
                            _3BetVsRaiser.Get3BetBBvsRaiseSB(request.Hand),
                        _ => string.Empty
                    },
                HeroPosition.SmallBlind =>
                    request.VillainPosition switch
                    {
                        HeroPosition.MiddlePosition =>
                            _3BetVsRaiser.Get3BetSBvsRaiseMP(request.Hand),
                        HeroPosition.CutOff =>
                            _3BetVsRaiser.Get3BetSBvsRaiseCO(request.Hand),
                        HeroPosition.Button =>
                            _3BetVsRaiser.Get3BetSBvsRaiseBTN(request.Hand),
                        HeroPosition.EarlyPosition =>
                            _3BetVsRaiser.Get3BetSBvsRaiseEP(request.Hand),
                        _ => string.Empty
                    },
                _ => string.Empty
            };

            response.Action = action;


            return response;
        }

    }
}
