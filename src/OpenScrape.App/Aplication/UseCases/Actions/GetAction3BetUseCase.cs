using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetAction3BetUseCase : IGetAction3BetUseCase
    {
        public GetAction3BetUseCaseResponse Execute(GetAction3BetUseCaseRequest request)
        {
            var response = new GetAction3BetUseCaseResponse();

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
                        _ => "Fold"
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
                        _ => "Fold"
                    },
                HeroPosition.Button =>
                    request.VillainPosition switch
                    {
                        HeroPosition.MiddlePosition =>
                            _3BetVsRaiser.Get3BetBTNvsRaiseMP(request.Hand),
                        HeroPosition.CutOff =>
                            _3BetVsRaiser.Get3BetBTNvsRaiseCO(request.Hand),
                        HeroPosition.EarlyPosition =>
                            _3BetVsRaiser.Get3BetBTNvsRaiseEP(request.Hand),
                        _ => "Fold"
                    },
                HeroPosition.CutOff =>
                    request.VillainPosition switch
                    {
                        HeroPosition.MiddlePosition =>
                            _3BetVsRaiser.Get3BetCOvsRaiseMP(request.Hand),
                        HeroPosition.EarlyPosition =>
                            _3BetVsRaiser.Get3BetCOvsRaiseEP(request.Hand),
                        _ => "Fold"
                    },
                HeroPosition.EarlyPosition =>
                    request.VillainPosition switch
                    {
                        HeroPosition.EarlyPosition =>
                            _3BetVsRaiser.Get3BetMPvsRaiseEP(request.Hand),
                        _ => "Fold"
                    },
                _ => "Fold"
            };

            response.Action = action;


            return response;
        }

    }
}
