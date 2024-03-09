using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionSqueezeUseCase : IGetActionSqueezeUseCase
    {
        public GetActionSqueezeResponse Execute(GetActionSqueezeRequest request)
        {
            var response = new GetActionSqueezeResponse();

            var action = request.Position switch
            {
                HeroPosition.BigBlind =>
                    request.RaiserPosition switch
                    {
                        HeroPosition.Button =>
                            request.CallerPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseBTNandCallSB(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.CutOff =>
                            request.CallerPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseCOandCallSB(request.Hand),
                                HeroPosition.Button =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseCOandCallBTN(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.MiddlePosition =>
                            request.CallerPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseMPandCallSB(request.Hand),
                                HeroPosition.Button =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseMPandCallBTN(request.Hand),
                                HeroPosition.CutOff =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseMPandCallCO(request.Hand),
                                _ => "Fold"
                            },
                        HeroPosition.EarlyPosition =>
                            request.CallerPosition switch
                            {
                                HeroPosition.SmallBlind =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseEPandCallSB(request.Hand),
                                HeroPosition.Button =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseEPandCallBTN(request.Hand),
                                HeroPosition.CutOff =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseEPandCallCO(request.Hand),
                                HeroPosition.MiddlePosition =>
                                     Squeeze.GetSqueezeBBvsOpenRaiseEPandCallMP(request.Hand),
                                _ => "Fold"
                            },
                        _ => "Fold"
                    },
                    HeroPosition.SmallBlind =>
                        request.RaiserPosition switch
                        {
                            HeroPosition.CutOff =>
                                request.CallerPosition switch
                                {                                
                                    HeroPosition.Button =>
                                         Squeeze.GetSqueezeSBvsOpenRaiseCOandCallBTN(request.Hand),
                                    _ => "Fold"
                                },
                            HeroPosition.MiddlePosition =>
                                request.CallerPosition switch
                                {   
                                    HeroPosition.Button =>
                                         Squeeze.GetSqueezeSBvsOpenRaiseMPandCallBTN(request.Hand),
                                    HeroPosition.CutOff =>
                                         Squeeze.GetSqueezeSBvsOpenRaiseMPandCallCO(request.Hand),
                                    _ => "Fold"
                                },
                            HeroPosition.EarlyPosition =>
                                request.CallerPosition switch
                                {  
                                    HeroPosition.Button =>
                                         Squeeze.GetSqueezeSBvsOpenRaiseEPandCallBTN(request.Hand),
                                    HeroPosition.CutOff =>
                                         Squeeze.GetSqueezeSBvsOpenRaiseEPandCallCO(request.Hand),
                                    HeroPosition.MiddlePosition =>
                                         Squeeze.GetSqueezeSBvsOpenRaiseEPandCallMP(request.Hand),
                                    _ => "Fold"
                                },
                            _ => "Fold"
                    },
                    HeroPosition.Button =>
                        request.RaiserPosition switch
                        {
                            HeroPosition.EarlyPosition =>
                                request.CallerPosition switch
                                {
                                    HeroPosition.CutOff =>
                                            Squeeze.GetSqueezeBTNvsOpenRaiseEPandCallCO(request.Hand),
                                    HeroPosition.MiddlePosition =>
                                            Squeeze.GetSqueezeBTNvsOpenRaiseEPandCallMP(request.Hand),
                                    _ => "Fold"
                                },
                            _ => "Fold"
                        },
                    HeroPosition.CutOff =>
                    request.RaiserPosition switch
                    {
                            HeroPosition.EarlyPosition =>
                                request.CallerPosition switch
                                {
                                    HeroPosition.MiddlePosition =>
                                            Squeeze.GetSqueezeCOvsOpenRaiseEPandCallMP(request.Hand),
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
