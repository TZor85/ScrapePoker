using OpenScrape.App.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetActions2HandedUseCase : IGetActions2HandedUseCase
    {
        public GetActions2HandedResponse ExecuteOpenRaise(GetActions2HandedRequest request)
        {
            try
            {
                var responseList = new List<ActionsResponse>();
                var response = new GetActions2HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                    responseList = Actions2Handed.GetOpenRaiseSuitedAction(request.EffectiveStack);
                else
                    responseList = Actions2Handed.GetOpenRaiseOffSuitedAction(request.EffectiveStack);

                foreach (var list in responseList)
                {
                    foreach (var item in list.Hands)
                    {
                        if (item.Contains(string.Concat(request.Card0[0], request.Card1[0])) || item.Contains(string.Concat(request.Card1[0], request.Card0[0])))
                        {
                            response.Data.Action = list.Action;
                            response.Data.Style = list.Style;
                            response.Data.Position = list.Position;
                            response.Data.PreflopAction = list.PreflopAction;
                        }

                    }
                }

                return response;

            }
            catch
            {
                return new GetActions2HandedResponse();
            }
        }

        public GetActions2HandedResponse ExecuteVsPlayer(GetActions2HandedRequest request)
        {
            try
            {
                var responseList = new List<ActionsResponse>();
                var response = new GetActions2HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                {
                    if (request.BetP1 == 1 || request.BetP2 == 1)
                        responseList = Actions2Handed.GetVsLimpSuitedAction(request.EffectiveStack);
                    else if ((request.BetP1 > 1 && request.ChipsP1 > 1) || (request.BetP2 > 1 && request.ChipsP2 > 1))
                        responseList = Actions2Handed.GetVsOpenRaiseSuitedAction(request.EffectiveStack);
                    else if ((request.BetP1 > 1 && request.ChipsP1 <= 1) || (request.BetP2 > 1 && request.ChipsP2 <= 1))
                        responseList = Actions2Handed.GetVsAllInSuitedAction(request.EffectiveStack);
                }
                else
                {
                    if (request.BetP1 == 1 || request.BetP2 == 1)
                        responseList = Actions2Handed.GetVsLimpOffSuitedAction(request.EffectiveStack);
                    else if ((request.BetP1 > 1 && request.ChipsP1 > 1) || (request.BetP2 > 1 && request.ChipsP2 > 1))
                        responseList = Actions2Handed.GetVsOpenRaiseOffSuitedAction(request.EffectiveStack);
                    else if ((request.BetP1 > 1 && request.ChipsP1 <= 1) || (request.BetP2 > 1 && request.ChipsP2 <= 1))
                        responseList = Actions2Handed.GetVsAllInOffSuitedAction(request.EffectiveStack);
                }

                foreach (var list in responseList)
                {
                    foreach (var item in list.Hands)
                    {
                        if (item.Contains(string.Concat(request.Card0[0], request.Card1[0])) || item.Contains(string.Concat(request.Card1[0], request.Card0[0])))
                        {
                            response.Data.Action = list.Action;
                            response.Data.Style = list.Style;
                            response.Data.Position = list.Position;
                            response.Data.PreflopAction = list.PreflopAction;
                        }
                    }
                }

                return response;

            }
            catch
            {
                return new GetActions2HandedResponse();
            }
        }
                
    }
}
