using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionRaiseVsSBLimpUseCase : IGetActionRaiseVsSBLimpUseCase
{
    private ActionScenarioUseCases _actionUseCase;

    public GetActionRaiseVsSBLimpUseCase(ActionScenarioUseCases actionUseCase)
    {
        _actionUseCase = actionUseCase;
    }

    public async Task<GetActionRaiseVsSBLimpResponse> Execute(GetActionRaiseVsSBLimpRequest request)
    {
        var response = new GetActionRaiseVsSBLimpResponse();

        response.Action = await _actionUseCase.GetActionScenario.ExecuteAsync(GameSituation.BigBlindVsSmallBlind, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand,
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            Limper = request.LimperPosition,
            ThreeBetPosition = request.ThreeBetPosition
        });

        return response;
    }
}
