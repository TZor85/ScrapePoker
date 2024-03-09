using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionHeroCallOpenRaiseAndGetSqueezeUseCase : IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase
    {
        public GetActionHeroCallOpenRaiseAndGetSqueezeResponse Execute(GetActionHeroCallOpenRaiseAndGetSqueezeRequest request)
        {
            var response = new GetActionHeroCallOpenRaiseAndGetSqueezeResponse();

            var action = request.Position switch
            {
                HeroPosition.SmallBlind =>
                    request.RaiserPosition switch 
                    { 
                        HeroPosition.Button =>
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetBTNOpenRaiseHeroCallSBAndBBSqueezeAndBTNFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetBTNOpenRaiseHeroCallSBAndBBSqueezeAndBTNCall(request.Hand)
                            },
                        _ => "Fold"
                    },
                HeroPosition.Button =>
                    request.SqueezerPosition switch 
                    { 
                        HeroPosition.SmallBlind =>
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetCOOpenRaiseHeroCallBTNAndSBSqueezeAndCOFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetCOOpenRaiseHeroCallBTNAndSBSqueezeAndCOCall(request.Hand),
                            },
                        HeroPosition.BigBlind => 
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetCOOpenRaiseHeroCallBTNAndBBSqueezeAndCOFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetCOOpenRaiseHeroCallBTNAndBBSqueezeAndCOCall(request.Hand)
                            },
                        _ => "Fold"
                    },
                HeroPosition.CutOff =>
                    request.SqueezerPosition switch
                    { 
                        HeroPosition.SmallBlind =>
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetMPOpenRaiseHeroCallCOAndSBSqueezeAndMPFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetMPOpenRaiseHeroCallCOAndSBSqueezeAndMPCall(request.Hand)
                            },
                        HeroPosition.BigBlind => 
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetMPOpenRaiseHeroCallCOAndBBSqueezeAndMPFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetMPOpenRaiseHeroCallCOAndBBSqueezeAndMPCall(request.Hand)
                            },
                        HeroPosition.Button => 
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetMPOpenRaiseHeroCallCOAndBTNSqueezeAndMPFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetMPOpenRaiseHeroCallCOAndBTNSqueezeAndMPCall(request.Hand)
                            },
                        _ => "Fold"
                    },
                HeroPosition.MiddlePosition =>
                    request.SqueezerPosition switch 
                    {
                        HeroPosition.SmallBlind =>
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndSBSqueezeAndEPFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndSBSqueezeAndEPCall(request.Hand)
                            },
                        HeroPosition.BigBlind => 
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndBBSqueezeAndEPFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndBBSqueezeAndEPCall(request.Hand)
                            },
                        HeroPosition.Button => 
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndBTNSqueezeAndEPFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndBTNSqueezeAndEPCall(request.Hand)
                            },
                        HeroPosition.CutOff =>
                            request.RaiserCall switch
                            {
                                false => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndCOSqueezeAndEPFold(request.Hand),
                                true => HeroCallOpenRaiseAndGetSqueeze.GetEPOpenRaiseHeroCallMPAndCOSqueezeAndEPCall(request.Hand)
                            },
                        _ => "Fold"
                    },
                _ => "Fold"
            };

            return response;
        }
    }
}
