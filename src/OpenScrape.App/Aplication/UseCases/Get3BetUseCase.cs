using Nancy;
using OpenScrape.App.Enums;
using OpenScrape.App.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication.UseCases
{
    public class Get3BetUseCase : IGet3BetUseCase
    {
        public Get3BetUseCaseResponse Execute(Get3BetUseCaseRequest request)
        {
            var response = new Get3BetUseCaseResponse();

            var action = request.Position switch
            {
                HeroPosition.SmallBlind => Vs3Bet.GetSBvs3BetBB(request.Hand),
                
                _ => string.Empty
            };

            response.Action = action;


            return response;
        }

    }
}
