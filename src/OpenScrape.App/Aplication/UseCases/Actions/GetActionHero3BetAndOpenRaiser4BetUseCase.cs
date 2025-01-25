using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication.UseCases.Actions;

public class GetActionHero3BetAndOpenRaiser4BetUseCase : IGetActionHero3BetAndOpenRaiser4BetUseCase
{
    private ActionScenarioUseCases _actionScenarioUseCases;

    public GetActionHero3BetAndOpenRaiser4BetUseCase(ActionScenarioUseCases actionScenarioUseCases)
    {
        _actionScenarioUseCases = actionScenarioUseCases;
    }

    public async Task<GetActionHero3BetAndOpenRaiser4BetResponse> Execute(GetActionHero3BetAndOpenRaiser4BetRequest request)
    {
        var response = new GetActionHero3BetAndOpenRaiser4BetResponse();

        response.Action = await _actionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation.FourBet, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand,
            Suited = request.Hand.Contains('s') ? true : request.Hand.Contains('o') ? false : null,
            OpenRaiser = request.RaiserPosition,
            ThreeBetPosition = request.Position,
            IsGreater = request.IsGreater,
        });
        
        return response;
    }
}

