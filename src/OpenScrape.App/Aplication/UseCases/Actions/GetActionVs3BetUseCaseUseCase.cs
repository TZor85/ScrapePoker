using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionVs3BetUseCaseUseCase : IGetActionVs3BetUseCaseUseCase
{
    private ActionScenarioUseCases _actionScenarioUseCases;

    public GetActionVs3BetUseCaseUseCase(ActionScenarioUseCases actionScenarioUseCases)
    {
        _actionScenarioUseCases = actionScenarioUseCases;
    }

    public async Task<GetActionVs3BetUseCaseResponse> Execute(GetActionVs3BetUseCaseRequest request)
    {
        var response = new GetActionVs3BetUseCaseResponse();

        response.Action = await _actionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation.VsThreeBet, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand.Substring(0, 2),
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            ThreeBetPosition = request.ThreeBetPosition,
            OpenRaiser = request.Position
        });

        return response;
    }
}
