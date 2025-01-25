using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionSqueezeUseCase : IGetActionSqueezeUseCase
{
    private ActionScenarioUseCases _actionScenarioUseCases;

    public GetActionSqueezeUseCase(ActionScenarioUseCases actionScenarioUseCases)
    {
        _actionScenarioUseCases = actionScenarioUseCases;
    }

    public async Task<GetActionSqueezeResponse> Execute(GetActionSqueezeRequest request)
    {
        var response = new GetActionSqueezeResponse();

        response.Action = await _actionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation.Squeeze, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand,
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            OpenRaiser = request.OpenRaiserPosition,
            Caller = request.CallerPosition
        });

        return response;
    }
}
