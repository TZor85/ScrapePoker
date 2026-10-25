using OpenScrape.App.Aplication;
using OpenScrape.Domain.Interfaces;
using OpenScrape.Api.Persistence;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Arma el caso de uso preflop completo con implementaciones reales y un store
/// de rangos fijo, para poder reproducir un escenario sin base de datos.
/// </summary>
internal static class PreflopTestCaseBuilder
{
    public static SetPreflopActionUseCase Create(IPreflopRangeStore rangeStore, IRandomProvider randomProvider)
    {
        var scenarios = new ActionScenarioUseCases(new GetActionScenario(rangeStore, randomProvider));

        return new SetPreflopActionUseCase(
            new GetActionHeroCallOpenRaiseAndGetSqueezeUseCase(scenarios),
            new GetActionHero3BetAndOpenRaiser4BetUseCase(scenarios),
            new GetActionVs3BetUseCaseUseCase(scenarios),
            new GetActionVs3BetAndCallUseCase(scenarios),
            new GetActionSqueezeUseCase(scenarios),
            new GetActionOpenRaiseUseCase(scenarios),
            new GetActionRaiseOverLimperUseCase(scenarios),
            new GetAction3BetUseCase(scenarios),
            new GetActionRaiseVsSBLimpUseCase(scenarios),
            new GetActionCold4BetUseCase(scenarios));
    }
}
