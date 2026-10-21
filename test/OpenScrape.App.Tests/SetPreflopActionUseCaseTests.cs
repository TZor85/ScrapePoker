using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases.Actions;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

/// <summary>
/// Escenarios adversos del motor preflop: datos de OCR incompletos (una posición
/// ausente, un asiento que no existe, un raiser que no coincide con la apuesta).
/// El motor debe degradar a "sin acción" en vez de lanzar.
/// </summary>
[TestFixture]
public class SetPreflopActionUseCaseTests
{
    /// <summary>
    /// Todos los colaboradores comparten el mismo stub: ninguna rama de estos
    /// escenarios depende de un escenario persistido concreto.
    /// </summary>
    private static SetPreflopActionUseCase CreateUseCase()
    {
        var stub = new PreflopUseCasesStub();
        return new SetPreflopActionUseCase(stub, stub, stub, stub, stub, stub, stub, stub, stub, stub);
    }

    [Test]
    public async Task Execute_SinSmallBlindEnElMapa_NoLanzaExcepcion()
    {
        var state = new PlayerGameState
        {
            Position = TablePosition.CutOff,
            CurrentBet = 1m,
            Players =
            [
                new Player { Name = "P0", Position = TablePosition.CutOff, Bet = 1m, Active = true },
                new Player { Name = "P1", Position = TablePosition.Button, Bet = 1m, Active = true }
            ]
        };

        var request = new SetPreflopActionUseCaseRequest
        {
            PlayerState = state,
            ResponseAction = new ResponseAction()
        };
        request.PreflopHeroPosition[TablePosition.CutOff] = new Dictionary<TablePosition, decimal>
        {
            [TablePosition.CutOff] = 1m,
            [TablePosition.Button] = 1m
        };

        var useCase = CreateUseCase();

        Assert.DoesNotThrowAsync(async () => await useCase.Execute(request));
    }

    [Test]
    public async Task Execute_HeroEnBigBlindSinAsientoP5_NoLanzaExcepcion()
    {
        var state = new PlayerGameState
        {
            Position = TablePosition.BigBlind,
            CurrentBet = 1m,
            Players =
            [
                new Player { Name = "P0", Position = TablePosition.BigBlind, Bet = 1m, Active = true },
                new Player { Name = "P1", Position = TablePosition.Early, Bet = 0m, Active = true }
            ]
        };

        var request = new SetPreflopActionUseCaseRequest
        {
            PlayerState = state,
            ResponseAction = new ResponseAction()
        };
        request.PreflopHeroPosition[TablePosition.BigBlind] = new Dictionary<TablePosition, decimal>
        {
            [TablePosition.BigBlind] = 1m,
            [TablePosition.Early] = 0m
        };

        var useCase = CreateUseCase();

        Assert.DoesNotThrowAsync(async () => await useCase.Execute(request));
    }

    [Test]
    public async Task Execute_SegundaAccionSinRaiserQueCoincida_NoLanzaExcepcion()
    {
        var state = new PlayerGameState
        {
            Position = TablePosition.Early,
            CurrentBet = 3m,
            HandSituation = HandSituation.Call,
            Players =
            [
                new Player { Name = "P0", Position = TablePosition.Early, Bet = 0m, Active = true },
                new Player { Name = "P1", Position = TablePosition.Button, Bet = 3m, Active = true }
            ]
        };

        var request = new SetPreflopActionUseCaseRequest
        {
            PlayerState = state,
            ResponseAction = new ResponseAction
            {
                Action = "Call",
                HandSituation = HandSituation.Call,
                IsSecondAction = true
            }
        };
        request.PreflopHeroPosition[TablePosition.Early] = new Dictionary<TablePosition, decimal>
        {
            [TablePosition.Early] = 0m,
            [TablePosition.Button] = 3m
        };

        var useCase = CreateUseCase();

        Assert.DoesNotThrowAsync(async () => await useCase.Execute(request));
    }
    [Test]
    public async Task Execute_RaiserSinPosicionEnElMapa_NoLanzaExcepcion()
    {
        var state = new PlayerGameState
        {
            Position = TablePosition.Early,
            CurrentBet = 3m,
            HandSituation = HandSituation.ThreeBet,
            Players =
            [
                new Player { Name = "P0", Position = TablePosition.Early, Bet = 0m, Active = true },
                new Player { Name = "P1", Position = TablePosition.Button, Bet = 5m, Active = true }
            ]
        };

        var request = new SetPreflopActionUseCaseRequest
        {
            PlayerState = state,
            ResponseAction = new ResponseAction
            {
                Action = "Call",
                HandSituation = HandSituation.ThreeBet,
                IsSecondAction = true
            }
        };
        request.PreflopHeroPosition[TablePosition.Early] = new Dictionary<TablePosition, decimal>
        {
            [TablePosition.Early] = 0m
        };

        var useCase = CreateUseCase();

        Assert.DoesNotThrowAsync(async () => await useCase.Execute(request));
    }
}

/// <summary>
/// Colaborador que devuelve respuestas neutras: deja que el motor siga su curso
/// sin depender de escenarios persistidos en la base.
/// </summary>
internal sealed class PreflopUseCasesStub : IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase,
    IGetActionHero3BetAndOpenRaiser4BetUseCase,
    IGetActionVs3BetUseCaseUseCase,
    IGetActionVs3BetAndCallUseCase,
    IGetActionSqueezeUseCase,
    IGetActionOpenRaiseUseCase,
    IGetActionRaiseOverLimperUseCase,
    IGetAction3BetUseCase,
    IGetActionRaiseVsSBLimpUseCase,
    IGetActionCold4BetUseCase
{
    public Task<GetActionHeroCallOpenRaiseAndGetSqueezeResponse> Execute(GetActionHeroCallOpenRaiseAndGetSqueezeRequest request)
        => Task.FromResult(new GetActionHeroCallOpenRaiseAndGetSqueezeResponse { Action = string.Empty });

    public Task<GetActionHero3BetAndOpenRaiser4BetResponse> Execute(GetActionHero3BetAndOpenRaiser4BetRequest request)
        => Task.FromResult(new GetActionHero3BetAndOpenRaiser4BetResponse { Action = string.Empty });

    public Task<GetActionVs3BetUseCaseResponse> Execute(GetActionVs3BetUseCaseRequest request)
        => Task.FromResult(new GetActionVs3BetUseCaseResponse { Action = string.Empty });

    public Task<GetActionVs3BetAndCallUseCaseResponse> Execute(GetActionVs3BetAndCallUseCaseRequest request)
        => Task.FromResult(new GetActionVs3BetAndCallUseCaseResponse { Action = string.Empty });

    public Task<GetActionSqueezeResponse> Execute(GetActionSqueezeRequest request)
        => Task.FromResult(new GetActionSqueezeResponse { Action = string.Empty });

    public Task<GetActionOpenRaiseUseCaseResponse> Execute(GetActionOpenRaiseUseCaseRequest request)
        => Task.FromResult(new GetActionOpenRaiseUseCaseResponse { Action = string.Empty });

    public Task<GetActionRaiseOverLimperUseCaseResponse> Execute(GetActionRaiseOverLimperUseCaseRequest request)
        => Task.FromResult(new GetActionRaiseOverLimperUseCaseResponse { Action = string.Empty });

    public Task<GetAction3BetUseCaseResponse> Execute(GetAction3BetUseCaseRequest request)
        => Task.FromResult(new GetAction3BetUseCaseResponse { Action = string.Empty });

    public Task<GetActionRaiseVsSBLimpResponse> Execute(GetActionRaiseVsSBLimpRequest request)
        => Task.FromResult(new GetActionRaiseVsSBLimpResponse { Action = string.Empty });

    public Task<GetActionCold4BetUseCaseResponse> Execute(GetActionCold4BetUseCaseRequest request)
        => Task.FromResult(new GetActionCold4BetUseCaseResponse { Action = string.Empty });
}
