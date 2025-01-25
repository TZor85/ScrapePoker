using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionOpenRaiseUseCase : IGetActionOpenRaiseUseCase
{
    private ActionScenarioUseCases _actionUseCase;

    public GetActionOpenRaiseUseCase(ActionScenarioUseCases actionUseCase)
    {
        _actionUseCase = actionUseCase;
    }

    public async Task<GetActionOpenRaiseUseCaseResponse> Execute(GetActionOpenRaiseUseCaseRequest request)
    {
        var response = new GetActionOpenRaiseUseCaseResponse();

        response.Action = await _actionUseCase.GetActionScenario.ExecuteAsync(GameSituation.OpenRaise, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand,
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
        });

        return response;
    }
}
