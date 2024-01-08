using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetHero3BetAndOpenRaiser4BetUseCase : IGetHero3BetAndOpenRaiser4BetUseCase
    {
        public GetHero3BetAndOpenRaiser4BetResponse Execute(GetHero3BetAndOpenRaiser4BetRequest request)
        {
            var response = new GetHero3BetAndOpenRaiser4BetResponse();

            var action = request.Position switch
            {
                HeroPosition.BigBlind =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.SmallBlind =>
                            request.BetSize switch
                            {
                                <=20 => Hero3BetAndOpenRaiser4Bet.GetSBOpenRaiseHeroInBB3BetAndSB4Bet_Minus20BB(request.Hand),
                                >20 => Hero3BetAndOpenRaiser4Bet.GetSBOpenRaiseHeroInBB3BetAndSB4Bet_Plus20BB(request.Hand)
                            },
                        HeroPosition.Button =>
                            request.BetSize switch
                            {
                                <=23.5m => Hero3BetAndOpenRaiser4Bet.GetBTNOpenRaiseHeroInBB3BetAndBTN4Bet_Minus23_5BB(request.Hand),
                                >23.5m => Hero3BetAndOpenRaiser4Bet.GetBTNOpenRaiseHeroInBB3BetAndBTN4Bet_Plus23_5BB(request.Hand)
                            },
                        HeroPosition.CutOff =>
                            request.BetSize switch
                            {
                                <=22.5m => Hero3BetAndOpenRaiser4Bet.GetCOOpenRaiseHeroInBB3BetAndCO4Bet_Minus22_5BB(request.Hand),
                                >22.5m => Hero3BetAndOpenRaiser4Bet.GetCOOpenRaiseHeroInBB3BetAndCO4Bet_Plus22_5BB(request.Hand)
                            },
                        HeroPosition.MiddlePosition => 
                            request.BetSize switch
                            {
                                <=22m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInBB3BetAndMP4Bet_Minus22BB(request.Hand),
                                >22m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInBB3BetAndMP4Bet_Plus22BB(request.Hand)
                            },
                        HeroPosition.EarlyPosition =>
                                Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInBB3BetAndEP4Bet(request.Hand),
                        _ => "Fold"
                    },
                HeroPosition.SmallBlind =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.Button =>
                            request.BetSize switch
                            {
                                <=23.5m => Hero3BetAndOpenRaiser4Bet.GetBTNOpenRaiseHeroInSB3BetAndBTN4Bet_Minus23_5BB(request.Hand),
                                >23.5m => Hero3BetAndOpenRaiser4Bet.GetBTNOpenRaiseHeroInSB3BetAndBTN4Bet_Plus23_5BB(request.Hand)
                            },
                        HeroPosition.CutOff =>
                                Hero3BetAndOpenRaiser4Bet.GetCOOpenRaiseHeroInSB3BetAndCO4Bet(request.Hand),
                        HeroPosition.MiddlePosition =>
                            request.BetSize switch
                            {
                                <=20.5m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInSB3BetAndMP4Bet_Minus20_5BB(request.Hand),
                                >20.5m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInSB3BetAndMP4Bet_Plus20_5BB(request.Hand)
                            },
                        HeroPosition.EarlyPosition =>
                            request.BetSize switch
                            {
                                <=20.5m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInSB3BetAndEP4Bet_Minus20_5BB(request.Hand),
                                >20.5m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInSB3BetAndEP4Bet_Plus20_5BB(request.Hand)
                            },
                        _ => "Fold"
                    },
                HeroPosition.Button =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.CutOff =>
                            Hero3BetAndOpenRaiser4Bet.GetCOOpenRaiseHeroInBTN3BetAndCO4Bet(request.Hand),
                        HeroPosition.MiddlePosition =>
                            request.BetSize switch
                            {
                                <=19m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInBTN3BetAndMP4Bet_Minus19BB(request.Hand),
                                >19m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInBTN3BetAndMP4Bet_Plus19BB(request.Hand)
                            },
                        HeroPosition.EarlyPosition =>
                            request.BetSize switch
                            {
                                <=19.5m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInBTN3BetAndEP4Bet_Minus19_5BB(request.Hand),
                                >19.5m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInBTN3BetAndEP4Bet_Plus19_5BB(request.Hand)
                            },
                        _ => "Fold"
                    },
                HeroPosition.CutOff =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.MiddlePosition =>
                            request.BetSize switch
                            {
                                <=19m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInCO3BetAndMP4Bet_Minus19BB(request.Hand),
                                >19m => Hero3BetAndOpenRaiser4Bet.GetMPOpenRaiseHeroInCO3BetAndMP4Bet_Plus19BB(request.Hand)
                            },
                        HeroPosition.EarlyPosition =>
                            request.BetSize switch
                            {
                                <=19m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInCO3BetAndEP4Bet_Minus19BB(request.Hand),
                                >19m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInCO3BetAndEP4Bet_Plus19BB(request.Hand)
                            },
                        _ => "Fold"
                    },
                HeroPosition.MiddlePosition =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.EarlyPosition =>
                            request.BetSize switch
                            {
                                <=19m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInMP3BetAndEP4Bet_Minus19BB(request.Hand),
                                >19m => Hero3BetAndOpenRaiser4Bet.GetEPOpenRaiseHeroInMP3BetAndEP4Bet_Plus19BB(request.Hand)
                            },
                        _ => "Fold"
                    },
                _ => "Fold"
            };

            return response;
        }
    }
    
}
