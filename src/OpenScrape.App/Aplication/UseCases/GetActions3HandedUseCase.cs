using OpenScrape.App.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetActions3HandedUseCase : IGetActions3HandedUseCase
    {
        public GetActions3HandedResponse ExecuteButtonAction(GetActions3HandedRequest request)
        {
            try
            {
                var responseList = new List<KeyValuePair<string, List<string>>>();
                var response = new GetActions3HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                    responseList = Actions3Handed.GetButtonSuitedAction(request.EffectiveStack);
                else
                    responseList = Actions3Handed.GetButtonOffSuitedAction(request.EffectiveStack);

                foreach (var list in responseList)
                {
                    foreach (var item in list.Value)
                    {
                        if (item.Contains(string.Concat(request.Card0[0], request.Card1[0])) || item.Contains(string.Concat(request.Card1[0], request.Card0[0])))
                            response.Data = list.Key;
                    }
                }
                
                return response;
            }
            catch
            {
                return new GetActions3HandedResponse();
            }
        }

        public GetActions3HandedResponse ExecuteSmallBlindAction(GetActions3HandedRequest request)
        {
            try
            {
                var responseList = new List<KeyValuePair<string, List<string>>>();
                var response = new GetActions3HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                {
                    if (!request.P2Active)
                        responseList = Actions3Handed.GetSBvsBBSuitedAction(request.EffectiveStack);
                    else if (request.BetP2 == 1)
                        responseList = Actions3Handed.GetSBvsBTNLimpSuitedAction(request.EffectiveStack);
                    else if(request.BetP2 > 1 && request.ChipsP2 > 1)
                        responseList = Actions3Handed.GetSBvsBTNOpenRaiseSuitedAction(request.EffectiveStack);
                    else if (request.BetP2 > 1 && request.ChipsP2 <= 1)
                        responseList = Actions3Handed.GetSBvsBTNAllInSuitedAction(request.EffectiveStack);

                }
                else
                {
                    if (!request.P2Active)
                        responseList = Actions3Handed.GetSBvsBBOffSuitedAction(request.EffectiveStack);
                    else if (request.BetP2 == 1)
                        responseList = Actions3Handed.GetSBvsBTNLimpOffSuitedAction(request.EffectiveStack);
                    else if (request.BetP2 > 1 && request.ChipsP2 > 1)
                        responseList = Actions3Handed.GetSBvsBTNOpenRaiseOffSuitedAction(request.EffectiveStack);
                    else if (request.BetP2 > 1 && request.ChipsP2 <= 1)
                        responseList = Actions3Handed.GetSBvsBTNAllInOffSuitedAction(request.EffectiveStack);
                }

                foreach (var list in responseList)
                {
                    foreach (var item in list.Value)
                    {
                        if (item.Contains(string.Concat(request.Card0[0], request.Card1[0])) || item.Contains(string.Concat(request.Card1[0], request.Card0[0])))
                            response.Data = list.Key;
                    }
                }

                return response;

            }
            catch
            {
                return new GetActions3HandedResponse();
            }
        }

        public GetActions3HandedResponse ExecuteBigBlindAction(GetActions3HandedRequest request)
        {
            try
            {
                var responseList = new List<KeyValuePair<string, List<string>>>();
                var response = new GetActions3HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                {
                    if (!request.P1Active)
                    {
                        if (request.BetP2 == 1)
                            responseList = Actions3Handed.GetBBvsSBLimpSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 > 1)
                            responseList = Actions3Handed.GetBBvsSBOpenRaiseSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 <= 1)
                            responseList = Actions3Handed.GetBBvsSBAllInSuitedAction(request.EffectiveStack);
                    }
                    else if(request.BetP1 == 1)
                    {
                        if(!request.P2Active)
                            responseList = Actions3Handed.GetBBvsBTNLimpAndFoldSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 == 1)
                            responseList = Actions3Handed.GetBBvsBTNLimpAndCallSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 > 1)
                            responseList = Actions3Handed.GetBBvsBTNLimpAnd3BetSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 <= 1)
                            responseList = Actions3Handed.GetBBvsBTNLimpAndAllInSuitedAction(request.EffectiveStack);
                            
                    }
                    else if(request.BetP1 > 1 && request.ChipsP1 > 1)
                    {
                        if (!request.P2Active)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAndFoldSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 == request.BetP1)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAndCallSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > request.BetP1 && request.ChipsP2 > 1)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAnd3BetSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > request.BetP1 && request.ChipsP2 <= 1)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAndAllInSuitedAction(request.EffectiveStack);
                            
                    }
                    else if(request.BetP1 > 1 && request.ChipsP1 <= 1)
                    {
                        if(!request.P2Active)
                            responseList = Actions3Handed.GetBBvsBTNAllInAndFoldSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 == request.BetP1 || request.BetP2 > request.BetP1)
                            responseList = Actions3Handed.GetBBvsBTNAllInAndCallSuitedAction(request.EffectiveStack);                            
                    }

                }
                else
                {
                    if (!request.P1Active)
                    {
                        if (request.BetP2 == 1)
                            responseList = Actions3Handed.GetBBvsSBLimpOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 > 1)
                            responseList = Actions3Handed.GetBBvsSBOpenRaiseOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 <= 1)
                            responseList = Actions3Handed.GetBBvsSBAllInOffSuitedAction(request.EffectiveStack);
                    }
                    else if (request.BetP1 == 1)
                    {
                        if (!request.P2Active)
                            responseList = Actions3Handed.GetBBvsBTNLimpAndFoldOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 == 1)
                            responseList = Actions3Handed.GetBBvsBTNLimpAndCallOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 > 1)
                            responseList = Actions3Handed.GetBBvsBTNLimpAnd3BetOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > 1 && request.ChipsP2 <= 1)
                            responseList = Actions3Handed.GetBBvsBTNLimpAndAllInOffSuitedAction(request.EffectiveStack);
                    }
                    else if (request.BetP1 > 1 && request.ChipsP1 > 1)
                    {
                        if (!request.P2Active)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAndFoldOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 == request.BetP1)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAndCallOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > request.BetP1 && request.ChipsP2 > 1)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAnd3BetOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 > request.BetP1 && request.ChipsP2 <= 1)
                            responseList = Actions3Handed.GetBBvsBTNOpenRaiseAndAllInOffSuitedAction(request.EffectiveStack);
                    }
                    else if (request.BetP1 > 1 && request.ChipsP1 <= 1)
                    {
                        if (!request.P2Active)
                            responseList = Actions3Handed.GetBBvsBTNAllInAndFoldOffSuitedAction(request.EffectiveStack);
                        else if (request.BetP2 == request.BetP1 || request.BetP2 > request.BetP1)
                            responseList = Actions3Handed.GetBBvsBTNAllInAndCallOffSuitedAction(request.EffectiveStack);
                    }
                }

                foreach (var list in responseList)
                {
                    foreach (var item in list.Value)
                    {
                        if (item.Contains(string.Concat(request.Card0[0], request.Card1[0])) || item.Contains(string.Concat(request.Card1[0], request.Card0[0])))
                            response.Data = list.Key;
                    }
                }

                return response;
            }
            catch
            {
                return new GetActions3HandedResponse();
            }
        }

        
    }
}
