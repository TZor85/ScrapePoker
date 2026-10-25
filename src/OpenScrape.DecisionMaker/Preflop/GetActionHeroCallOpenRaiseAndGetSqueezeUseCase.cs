using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop
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
