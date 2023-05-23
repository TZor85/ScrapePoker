using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases
{
    public class GetActions3HandedUseCase : IGetActions3HandedUseCase
    {
        public GetActions3HandedResponse ExecuteButtonAction(GetActions3HandedRequest request)
        {
            try
            {
                var responseList = new List<ActionsResponse>();
                var response = new GetActions3HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                { }

                foreach (var list in responseList)
                {
                    foreach (var item in list.Hands)
                    {
                        if (item.Contains(string.Concat(request.Card0[0], request.Card1[0])) || item.Contains(string.Concat(request.Card1[0], request.Card0[0])))
                        {
                            response.Data.Action = list.Action;
                            response.Data.Style = list.Style;
                            response.Data.Position = list.Position;
                        }
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
                var responseList = new List<ActionsResponse>();
                var response = new GetActions3HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                {
                    

                }
                else
                {
                    
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
                        }
                            
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
                var responseList = new List<ActionsResponse>();
                var response = new GetActions3HandedResponse();

                //Suited
                if (request.Card0[1] == request.Card1[1])
                {
                    if (!request.P1Active)
                    {
                        
                    }
                    else if(request.BetP1 == 1)
                    {
                        
                            
                    }
                    else if(request.BetP1 > 1 && request.ChipsP1 > 1)
                    {
                        
                            
                    }
                    else if(request.BetP1 > 1 && request.ChipsP1 <= 1)
                    {
                                      
                    }

                }
                else
                {
                    if (!request.P1Active)
                    {
                        
                    }
                    else if (request.BetP1 == 1)
                    {
                        
                    }
                    else if (request.BetP1 > 1 && request.ChipsP1 > 1)
                    {
                        
                    }
                    else if (request.BetP1 > 1 && request.ChipsP1 <= 1)
                    {
                        
                    }
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
                        }
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
