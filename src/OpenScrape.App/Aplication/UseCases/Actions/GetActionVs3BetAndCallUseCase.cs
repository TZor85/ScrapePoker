using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionVs3BetAndCallUseCase : IGetActionVs3BetAndCallUseCase
{
    private ActionScenarioUseCases _actionScenarioUseCase;

    public GetActionVs3BetAndCallUseCase(ActionScenarioUseCases actionScenarioUseCase)
    {
        _actionScenarioUseCase = actionScenarioUseCase;
    }

    public async Task<GetActionVs3BetAndCallUseCaseResponse> Execute(GetActionVs3BetAndCallUseCaseRequest request)
    {
        var response = new GetActionVs3BetAndCallUseCaseResponse();

        response.Action = await _actionScenarioUseCase.GetActionScenario.ExecuteAsync(GameSituation.VsThreeBetAndCall, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand.Substring(0, 2),
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            ThreeBetPosition = request.ThreeBetPosition,
            Caller = request.CallerPosition,
            OpenRaiser = request.Position
        });

        return response;
    }
}
