using OpenScrape.App.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.UseCases.UseCase
{
    public class GetActionsUseCase : IGetActionsUseCase
    {
        public GetActionsResponse Execute(GetActionsRequest request)
        {
            try
            {
                var response = new GetActionsResponse();
                response.Data = GetdAction(request.Card0, request.Card1, request.EffectiveStack);

                return response;
            }
            catch
            {
                return new GetActionsResponse();
            }
        }

        private string GetdAction(string v1, string v2, double effectiveStack)
        {
            var responseList = new List<KeyValuePair<string, List<string>>>();

            //Suited
            if (v1[1] == v2[1])
                responseList = Actions.GetButtonSuitedAction(effectiveStack);
            else
                responseList = Actions.GetButtonOffSuitedAction(effectiveStack);

            foreach (var list in responseList)
            {
                foreach (var item in list.Value)
                {
                    if (item.Contains(string.Concat(v1[0], v2[0])) || item.Contains(string.Concat(v2[0], v1[0])))
                        return list.Key;
                }
            }

            return string.Empty;
        }
    }
}
