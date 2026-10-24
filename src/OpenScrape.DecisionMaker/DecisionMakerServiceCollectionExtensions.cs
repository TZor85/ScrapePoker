using Microsoft.Extensions.DependencyInjection;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.DecisionMaker;

/// <summary>
/// Registra el motor completo. El host que lo usa debe aportar
/// <see cref="Microsoft.Extensions.Options.IOptions{T}"/> con el perfil de estrategia
/// y una implementación de <see cref="OpenScrape.Domain.Interfaces.IPreflopRangeStore"/>.
/// </summary>
public static class DecisionMakerServiceCollectionExtensions
{
    public static IServiceCollection AddDecisionMaker(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IRandomProvider, SystemRandomProvider>();
        services.AddSingleton<MonteCarloSimulator>();
        services.AddSingleton<IMonteCarloSimulator>(sp => sp.GetRequiredService<MonteCarloSimulator>());
        services.AddSingleton<OutsCalculator>();
        services.AddSingleton<IOutsCalculator>(sp => sp.GetRequiredService<OutsCalculator>());
        services.AddSingleton<PreflopEquityCalculator>();
        services.AddSingleton<BitHandEvaluator>();
        services.AddSingleton<IHandEvaluator>(sp => sp.GetRequiredService<BitHandEvaluator>());
        services.AddSingleton<BoardTextureAnalyzer>();
        services.AddSingleton<IBoardTextureAnalyzer>(sp => sp.GetRequiredService<BoardTextureAnalyzer>());
        services.AddSingleton<PreflopAnalyzer>();
        services.AddSingleton<IPreflopAnalyzer>(sp => sp.GetRequiredService<PreflopAnalyzer>());

        services.AddSingleton<BetSizingService>();
        services.AddSingleton<IBetSizingService>(sp => sp.GetRequiredService<BetSizingService>());
        services.AddSingleton<RangePolarizer>();
        services.AddSingleton<IRangePolarizer>(sp => sp.GetRequiredService<RangePolarizer>());
        services.AddSingleton<ThresholdsRegistry>();
        services.AddSingleton<IThresholdsRegistry>(sp => sp.GetRequiredService<ThresholdsRegistry>());

        services.AddSingleton<UnifiedPokerCalculator>();
        services.AddSingleton<IPokerCalculator>(sp => sp.GetRequiredService<UnifiedPokerCalculator>());

        services.AddSingleton<PostflopDecisionService>();
        services.AddSingleton<IPostflopDecisionService>(sp => sp.GetRequiredService<PostflopDecisionService>());

        services.AddScoped<GetActionScenario>();
        services.AddScoped<ActionScenarioUseCases>();
        services.AddScoped<ISetPreflopActionUseCase, SetPreflopActionUseCase>();
        services.AddScoped<SetPreflopActionUseCase>(sp =>
            sp.GetRequiredService<ISetPreflopActionUseCase>() as SetPreflopActionUseCase
            ?? throw new InvalidOperationException("El caso de uso preflop no es la implementación local."));

        services.AddScoped<IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase, GetActionHeroCallOpenRaiseAndGetSqueezeUseCase>();
        services.AddScoped<IGetActionHero3BetAndOpenRaiser4BetUseCase, GetActionHero3BetAndOpenRaiser4BetUseCase>();
        services.AddScoped<IGetActionVs3BetUseCaseUseCase, GetActionVs3BetUseCaseUseCase>();
        services.AddScoped<IGetActionVs3BetAndCallUseCase, GetActionVs3BetAndCallUseCase>();
        services.AddScoped<IGetActionSqueezeUseCase, GetActionSqueezeUseCase>();
        services.AddScoped<IGetActionOpenRaiseUseCase, GetActionOpenRaiseUseCase>();
        services.AddScoped<IGetActionRaiseOverLimperUseCase, GetActionRaiseOverLimperUseCase>();
        services.AddScoped<IGetAction3BetUseCase, GetAction3BetUseCase>();
        services.AddScoped<IGetActionRaiseVsSBLimpUseCase, GetActionRaiseVsSBLimpUseCase>();
        services.AddScoped<IGetActionCold4BetUseCase, GetActionCold4BetUseCase>();

        services.AddScoped<PreflopRequestFactory>();
        services.AddScoped<PostflopRequestFactory>();
        services.AddScoped<PostflopDecisionEngine>();
        services.AddScoped<IDecisionEngine, LocalDecisionEngine>();

        return services;
    }
}
