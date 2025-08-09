using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionRaiseOverLimperUseCase : IGetActionRaiseOverLimperUseCase
{
    private ActionScenarioUseCases _actionUseCase;

    public GetActionRaiseOverLimperUseCase(ActionScenarioUseCases actionUseCase)
    {
        _actionUseCase = actionUseCase;
    }

    public async Task<GetActionRaiseOverLimperUseCaseResponse> Execute(GetActionRaiseOverLimperUseCaseRequest request)
    {
        var response = new GetActionRaiseOverLimperUseCaseResponse();

        response.Action = await _actionUseCase.GetActionScenario.ExecuteAsync(GameSituation.RaiseOverLimpers, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand.Substring(0, 2),
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            Limper = request.LimperPosition
        });
         
        return response;
    }
}
