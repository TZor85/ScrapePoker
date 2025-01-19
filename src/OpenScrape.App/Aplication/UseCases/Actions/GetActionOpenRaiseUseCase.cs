using OpenScrape.App.Tables;
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

        await _actionUseCase.GetActionScenario.ExecuteAsync(GameSituation.RaiseOverLimpers, new ActionScenarioRequest
        {
            HeroPosition = request.Position,
            HandName = request.Hand,
            Suited = request.Hand.Contains("s") ? true : false,
        });

        //response.Action = request.Position switch
        //{
        //    HeroPosition.SmallBlind => OpenRaises.GetSmallBlindAction(request.Hand),
        //    HeroPosition.Button => OpenRaises.GetButtonAction(request.Hand),
        //    HeroPosition.CutOff => OpenRaises.GetCutOffAction(request.Hand),
        //    HeroPosition.MiddlePosition => OpenRaises.GetMiddleAction(request.Hand),
        //    HeroPosition.EarlyPosition => OpenRaises.GetEarlyAction(request.Hand),
        //    _ => string.Empty
        //};

        return response;
    }
}
