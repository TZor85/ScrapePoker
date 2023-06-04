using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Aplication.UseCases
{
    public class Get3BetUseCase : IGet3BetUseCase
    {
        public Get3BetUseCaseResponse Execute(Get3BetUseCaseRequest request)
        {
            var response = new Get3BetUseCaseResponse();

            var action = request.Position switch
            {
                HeroPosition.SmallBlind => VsOpenRaise.GetOpenRaiseSBvsBB(request.Hand),
                
                _ => string.Empty
            };

            response.Action = action;


            return response;
        }

    }
}
