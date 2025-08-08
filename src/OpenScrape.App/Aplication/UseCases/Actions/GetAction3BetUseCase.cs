using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetAction3BetUseCase : IGetAction3BetUseCase
{
    private ActionScenarioUseCases _actionUseCase;

    public GetAction3BetUseCase(ActionScenarioUseCases actionUseCase)
    {
        _actionUseCase = actionUseCase;
    }

    public async Task<GetAction3BetUseCaseResponse> Execute(GetAction3BetUseCaseRequest request)
    {
        var response = new GetAction3BetUseCaseResponse();

        response.Action = await _actionUseCase.GetActionScenario.ExecuteAsync(GameSituation.ThreeBet, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand.Substring(0, 2),
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            OpenRaiser = request.OpenRaiserPosition
        });

        return response;
    }
}
