using OpenScrape.App.Enums;
using OpenScrape.App.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetCold4BetUseCase : IGetCold4BetUseCase
    {
        public GetCold4BetUseCaseResponse Execute(GetCold4BetUseCaseRequest request)
        {
            var response = new GetCold4BetUseCaseResponse();

            var action = request.Position switch
            {
                HeroPosition.BigBlind =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.Button =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseBTNand3BetSB(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.CutOff =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseCOand3BetSB(request.Hand),
                                HeroPosition.Button =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseCOand3BetBTN(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.MiddlePosition =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseMPand3BetSB(request.Hand),
                                HeroPosition.Button =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseMPand3BetBTN(request.Hand),
                                HeroPosition.CutOff =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseMPand3BetCO(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.EarlyPosition =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseEPand3BetSB(request.Hand),
                                HeroPosition.Button =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseEPand3BetBTN(request.Hand),
                                HeroPosition.CutOff =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseEPand3BetCO(request.Hand),
                                HeroPosition.MiddlePosition =>
                                     Cold4Bet.GetCold4BetBBvsOpenRaiseEPand3BetMP(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                HeroPosition.SmallBlind =>
                    request.RaiserPosition switch
                    {   
                        HeroPosition.CutOff =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.Button =>
                                     Cold4Bet.GetCold4BetSBvsOpenRaiseCOand3BetBTN(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.MiddlePosition =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.Button =>
                                     Cold4Bet.GetCold4BetSBvsOpenRaiseMPand3BetBTN(request.Hand),
                                HeroPosition.CutOff =>
                                     Cold4Bet.GetCold4BetSBvsOpenRaiseMPand3BetCO(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.EarlyPosition =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.Button =>
                                     Cold4Bet.GetCold4BetSBvsOpenRaiseEPand3BetBTN(request.Hand),
                                HeroPosition.CutOff =>
                                     Cold4Bet.GetCold4BetSBvsOpenRaiseEPand3BetCO(request.Hand),
                                HeroPosition.MiddlePosition =>
                                     Cold4Bet.GetCold4BetSBvsOpenRaiseEPand3BetMP(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                HeroPosition.Button =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.MiddlePosition =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.CutOff =>
                                        Cold4Bet.GetCold4BetBTNvsOpenRaiseMPand3BetCO(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.EarlyPosition =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.CutOff =>
                                        Cold4Bet.GetCold4BetBTNvsOpenRaiseEPand3BetCO(request.Hand),
                                HeroPosition.MiddlePosition =>
                                        Cold4Bet.GetCold4BetBTNvsOpenRaiseEPand3BetMP(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                HeroPosition.CutOff =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.EarlyPosition =>
                            request.ThreeBetVillainPosition switch
                            {
                                HeroPosition.MiddlePosition =>
                                        Cold4Bet.GetCold4BetCOvsOpenRaiseEPand3BetMP(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                _ => "Fold"
            };

            response.Action = action;

            return response;
        }
    }
}
