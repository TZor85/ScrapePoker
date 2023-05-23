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
                return new GetActions2HandedResponse();
            }
        }
                
    }
}
