using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionVs3BetAndCallUseCase : IGetActionVs3BetAndCallUseCase
    {
        public GetActionVs3BetAndCallUseCaseResponse Execute(GetActionVs3BetAndCallUseCaseRequest request)
        {
            var response = new GetActionVs3BetAndCallUseCaseResponse();

            var action = request.Position switch
            {
                HeroPosition.Button =>
                    request.VillainPosition switch
                    {
                        HeroPosition.SmallBlind =>
                            request.CallerPosition switch
                            {
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseBTNvs3BetSBAndBBCall(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                HeroPosition.CutOff =>
                    request.VillainPosition switch
                    {
                        HeroPosition.SmallBlind =>
                            request.CallerPosition switch
                            {
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseCOvs3BetSBAndBBCall(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.Button =>
                            request.CallerPosition switch
                            {
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseCOvs3BetBTNAndBBCall(request.Hand),
                                HeroPosition.SmallBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseCOvs3BetBTNAndSBCall(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                HeroPosition.MiddlePosition =>
                    request.VillainPosition switch
                    {
                        HeroPosition.SmallBlind =>
                            request.CallerPosition switch
                            {
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseMPvs3BetSBAndBBCall(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.Button => 
                            request.CallerPosition switch
                            {
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseMPvs3BetBTNAndBBCall(request.Hand),
                                HeroPosition.SmallBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseMPvs3BetBTNAndSBCall(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.CutOff =>
                            request.CallerPosition switch
                            {
                                HeroPosition.Button =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseMPvs3BetCOAndBTNCall(request.Hand),
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseMPvs3BetCOAndBBCall(request.Hand),
                                HeroPosition.SmallBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseMPvs3BetCOAndSBCall(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                HeroPosition.EarlyPosition =>
                    request.VillainPosition switch
                    {
                        HeroPosition.SmallBlind =>
                            request.CallerPosition switch
                            {
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetSBAndBBCall(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.Button =>
                            request.CallerPosition switch
                            {
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetBTNAndBBCall(request.Hand),
                                HeroPosition.SmallBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetBTNAndSBCall(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.CutOff =>
                            request.CallerPosition switch
                            {
                                HeroPosition.Button =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetCOAndBTNCall(request.Hand),
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetCOAndBBCall(request.Hand),
                                HeroPosition.SmallBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetCOAndSBCall(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.MiddlePosition =>
                            request.CallerPosition switch
                            {
                                HeroPosition.CutOff =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetMPAndCOCall(request.Hand),
                                HeroPosition.Button =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetMPAndBTNCall(request.Hand),
                                HeroPosition.BigBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetMPAndBBCall(request.Hand),
                                HeroPosition.SmallBlind =>
                                    OpenRaiseVs3Bet_Call.GetOpenRaiseEPvs3BetMPAndSBCall(request.Hand),
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
