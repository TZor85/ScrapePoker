using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions
{
    public class GetActionHeroCallOpenRaiseAndGetSqueezeUseCase : IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase
    {
        private ActionScenarioUseCases _actionScenarioUseCases;

        public GetActionHeroCallOpenRaiseAndGetSqueezeUseCase(ActionScenarioUseCases actionScenarioUseCases)
        {
            _actionScenarioUseCases = actionScenarioUseCases;
        }

        public async Task<GetActionHeroCallOpenRaiseAndGetSqueezeResponse> Execute(GetActionHeroCallOpenRaiseAndGetSqueezeRequest request)
        {
            var response = new GetActionHeroCallOpenRaiseAndGetSqueezeResponse();

            response.Action = await _actionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation.VsSqueeze, new ActionScenarioRequest
            {
                HeroPosition = request.Position,
                HandName = request.Hand.Substring(0, 2),
                Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
                OpenRaiser = request.RaiserPosition,
                Squeezer = request.SqueezerPosition,
                RaiserFolds = request.RaiserFolds
            });

            return response;
        }
    }
}
