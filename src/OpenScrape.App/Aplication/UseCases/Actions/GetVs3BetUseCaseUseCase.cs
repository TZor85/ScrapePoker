using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetVs3BetUseCaseUseCase : IGetVs3BetUseCaseUseCase
    {
        public GetVs3BetUseCaseResponse Execute(GetVs3BetUseCaseRequest request)
        {
            var response = new GetVs3BetUseCaseResponse();

            var action = request.Position switch
            {
                HeroPosition.SmallBlind =>
                    request.VillainPosition switch
                    {
                        HeroPosition.BigBlind =>
                            OpenRaiseVs3Bet.GetOpenRaiseSBvs3BetBB(request.Hand),
                        _ => "Fold"
                    },
                HeroPosition.Button =>
                    request.VillainPosition switch 
                    {
                        HeroPosition.BigBlind =>
                            OpenRaiseVs3Bet.GetOpenRaiseBTNvs3BetBB(request.Hand),
                        HeroPosition.SmallBlind =>
                            OpenRaiseVs3Bet.GetOpenRaiseBTNvs3BetSB(request.Hand),
                        _ => "Fold"
                    },
                HeroPosition.CutOff =>
                    request.VillainPosition switch
                    { 
                        HeroPosition.Button =>
                            OpenRaiseVs3Bet.GetOpenRaiseCOvs3BetBTN(request.Hand),
                        HeroPosition.BigBlind =>
                            OpenRaiseVs3Bet.GetOpenRaiseCOvs3BetBB(request.Hand),
                        HeroPosition.SmallBlind =>
                            OpenRaiseVs3Bet.GetOpenRaiseCOvs3BetSB(request.Hand),
                        _ => "Fold"
                    },
                HeroPosition.MiddlePosition =>
                    request.VillainPosition switch
                    { 
                        HeroPosition.CutOff =>
                            OpenRaiseVs3Bet.GetOpenRaiseMPvs3BetCO(request.Hand),
                        HeroPosition.Button =>
                            OpenRaiseVs3Bet.GetOpenRaiseMPvs3BetBTN(request.Hand),
                        HeroPosition.BigBlind =>
                            OpenRaiseVs3Bet.GetOpenRaiseMPvs3BetBB(request.Hand),
                        HeroPosition.SmallBlind =>
                            OpenRaiseVs3Bet.GetOpenRaiseMPvs3BetSB(request.Hand),
                        _ => "Fold"
                    },
                HeroPosition.EarlyPosition =>
                request.VillainPosition switch
                {
                    HeroPosition.MiddlePosition =>
                        OpenRaiseVs3Bet.GetOpenRaiseEPvs3BetMP(request.Hand),
                    HeroPosition.CutOff =>
                        OpenRaiseVs3Bet.GetOpenRaiseEPvs3BetCO(request.Hand),
                    HeroPosition.Button =>
                        OpenRaiseVs3Bet.GetOpenRaiseEPvs3BetBTN(request.Hand),
                    HeroPosition.BigBlind =>
                        OpenRaiseVs3Bet.GetOpenRaiseEPvs3BetBB(request.Hand),
                    HeroPosition.SmallBlind =>
                        OpenRaiseVs3Bet.GetOpenRaiseEPvs3BetSB(request.Hand),
                    _ => "Fold"
                },
                _ => "Fold"
            };

            response.Action = action;


            return response;
        }
    }
}
