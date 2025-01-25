using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionCold4BetUseCase : IGetActionCold4BetUseCase
{

    private ActionScenarioUseCases _actionUseCase;

    public GetActionCold4BetUseCase(ActionScenarioUseCases actionUseCase)
    {
        _actionUseCase = actionUseCase;
    }
    public async Task<GetActionCold4BetUseCaseResponse> Execute(GetActionCold4BetUseCaseRequest request)
    {
        var response = new GetActionCold4BetUseCaseResponse();

        response.Action = await _actionUseCase.GetActionScenario.ExecuteAsync(GameSituation.Cold4Bet, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand,
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            OpenRaiser = request.RaiserPosition,
            ThreeBetPosition = request.ThreeBetPosition
        });

        return response;
    }
}
