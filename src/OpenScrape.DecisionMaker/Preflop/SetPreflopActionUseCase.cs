using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop;

public class SetPreflopActionUseCase : ISetPreflopActionUseCase
{
    private readonly IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase _heroCallOpenRaiseAndGetSqueezeUseCase;
    private readonly IGetActionHero3BetAndOpenRaiser4BetUseCase _hero3BetAndOpenRaiser4BetUseCase;
    private readonly IGetActionVs3BetUseCaseUseCase _vs3BetUseCase;
    private readonly IGetActionVs3BetAndCallUseCase _vs3BetAndCallUseCase;
    private readonly IGetActionSqueezeUseCase _squeezeUseCase;
    private readonly IGetActionOpenRaiseUseCase _openRaiseUseCase;
    private readonly IGetActionRaiseOverLimperUseCase _raiseOverLimperUseCase;
    private readonly IGetAction3BetUseCase _threeBetUseCase;
    private readonly IGetActionRaiseVsSBLimpUseCase _raiseVsSBLimpUseCase;
    private readonly IGetActionCold4BetUseCase _cold4BetUseCase;

    public SetPreflopActionUseCase(
        IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase heroCallOpenRaiseAndGetSqueezeUseCase,
        IGetActionHero3BetAndOpenRaiser4BetUseCase hero3BetAndOpenRaiser4BetUseCase,
        IGetActionVs3BetUseCaseUseCase vs3BetUseCase,
        IGetActionVs3BetAndCallUseCase vs3BetAndCallUseCase,
        IGetActionSqueezeUseCase squeezeUseCase,
        IGetActionOpenRaiseUseCase openRaiseUseCase,
        IGetActionRaiseOverLimperUseCase raiseOverLimperUseCase,
        IGetAction3BetUseCase threeBetUseCase,
        IGetActionRaiseVsSBLimpUseCase raiseVsSBLimpUseCase,
        IGetActionCold4BetUseCase cold4BetUseCase)
    {
        ArgumentNullException.ThrowIfNull(heroCallOpenRaiseAndGetSqueezeUseCase);
        ArgumentNullException.ThrowIfNull(hero3BetAndOpenRaiser4BetUseCase);
        ArgumentNullException.ThrowIfNull(vs3BetUseCase);
        ArgumentNullException.ThrowIfNull(vs3BetAndCallUseCase);
        ArgumentNullException.ThrowIfNull(squeezeUseCase);
        ArgumentNullException.ThrowIfNull(openRaiseUseCase);
        ArgumentNullException.ThrowIfNull(raiseOverLimperUseCase);
        ArgumentNullException.ThrowIfNull(threeBetUseCase);
        ArgumentNullException.ThrowIfNull(raiseVsSBLimpUseCase);
        ArgumentNullException.ThrowIfNull(cold4BetUseCase);

        _heroCallOpenRaiseAndGetSqueezeUseCase = heroCallOpenRaiseAndGetSqueezeUseCase;
        _hero3BetAndOpenRaiser4BetUseCase = hero3BetAndOpenRaiser4BetUseCase;
        _vs3BetUseCase = vs3BetUseCase;
        _vs3BetAndCallUseCase = vs3BetAndCallUseCase;
        _squeezeUseCase = squeezeUseCase;
        _openRaiseUseCase = openRaiseUseCase;
        _raiseOverLimperUseCase = raiseOverLimperUseCase;
        _threeBetUseCase = threeBetUseCase;
        _raiseVsSBLimpUseCase = raiseVsSBLimpUseCase;
        _cold4BetUseCase = cold4BetUseCase;
    }

    public async Task<SetPreflopActionUseCaseResponse> Execute(SetPreflopActionUseCaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.PlayerState);

        var playerState = request.PlayerState.Clone();
        var responseAction = new ResponseAction
        {
            Action = request.ResponseAction.Action,
            HandSituation = request.ResponseAction.HandSituation,
            IsSecondAction = request.ResponseAction.IsSecondAction
        };

        // El mapa se arma recorriendo los jugadores con posición asignada: si el héroe
        // no quedó en ese recorrido, su posición no existe como clave.
        var heroBets = request.PreflopHeroPosition.TryGetValue(playerState.Position, out var bets)
            ? bets
            : new Dictionary<TablePosition, decimal>();

        if (responseAction.IsSecondAction)
        {
            if (responseAction.Action is not null && playerState.HandSituation == HandSituation.Call)
            {
                var raiser = playerState.Players
                    .FirstOrDefault(f => f.Bet == playerState.CurrentBet && f.Position < playerState.Position);
                var squeezer = playerState.Players
                    .FirstOrDefault(w => w.Bet > playerState.CurrentBet && w.Position > playerState.Position);

                if (raiser is not null && squeezer is not null)
                {
                    var action = await GetHeroCallOpenRaiseAndGetSqueezeAction(
                                    heroBets,
                                    raiser.Position,
                                    squeezer.Position,
                                    playerState);

                    if (!string.IsNullOrEmpty(action))
                    {
                        responseAction.Action = action;
                        responseAction.HandSituation = action != "Fold" ? HandSituation.VsSqueeze : HandSituation.None;
                        responseAction.IsSecondAction = false;
                    }
                }
            }

            if (responseAction.Action is not null &&
                (playerState.HandSituation == HandSituation.ThreeBet ||
                 playerState.HandSituation == HandSituation.Squeeze))
            {
                var raiser = playerState.Players.FirstOrDefault(w => w.Bet > playerState.CurrentBet);
                if (raiser != null)
                {
                    var action = await GetHero3BetAndOpenRaiser4BetAction(heroBets, raiser.Position, playerState);
                    if (!string.IsNullOrEmpty(action))
                    {
                        responseAction.Action = action;
                        responseAction.HandSituation = action != "Fold" ? HandSituation.FourBet : HandSituation.None;
                        responseAction.IsSecondAction = false;
                    }
                }
            }

            if (responseAction.Action is not null && (playerState.HandSituation == HandSituation.OpenRaise || playerState.HandSituation == HandSituation.RaiseOverLimper))
            {
                var action = await GetOpenRaiseVs3BetAction(heroBets, playerState.Players.FirstOrDefault(w => w.Bet >= 1 && w.Position != TablePosition.BigBlind)?.Position ?? TablePosition.None, playerState);
                if (!string.IsNullOrEmpty(action))
                {
                    responseAction.Action = action;
                    responseAction.HandSituation = HandSituation.OpenRaiseVs3Bet;
                    responseAction.IsSecondAction = false;
                }

            }

            if (responseAction.Action is not null && (playerState.HandSituation == HandSituation.OpenRaise || playerState.HandSituation == HandSituation.RaiseOverLimper) && UserHandHelper.Exist4Bet(playerState))
            {
                var action = await GetOpenRaiseVs3BetAndCallAction(heroBets, playerState);
                if (!string.IsNullOrEmpty(action))
                {
                    responseAction.Action = action;
                    responseAction.HandSituation = HandSituation.OpenRaiseVs3BetAndCall;
                    responseAction.IsSecondAction = false;
                }

            }
        }


        //SQUEEZE
        if (responseAction.Action is null)
        {
            var action = await GetSqueezeAction(heroBets, playerState);
            if (!string.IsNullOrEmpty(action))
            {
                responseAction.Action = action;
                responseAction.HandSituation = HandSituation.Squeeze;
                responseAction.IsSecondAction = true;
            }
        }


        //OPEN RAISE
        if (responseAction.Action is null)
        {
            var action = await GetOpenRaiseAction(heroBets, playerState);
            if (!string.IsNullOrEmpty(action))
            {
                responseAction.Action = action;
                responseAction.HandSituation = HandSituation.OpenRaise;
                responseAction.IsSecondAction = true;
            }
        }

        //COLD 4BET
        if (responseAction.Action is null)
        {
            var action = await Cold4BetAction(heroBets, playerState);
            if (!string.IsNullOrEmpty(action))
            {
                responseAction.Action = action;
                responseAction.HandSituation = HandSituation.Cold4Bet;
                responseAction.IsSecondAction = true;
            }
        }

        //GET RAISE OVER LIMPER
        if (responseAction.Action is null)
        {
            var action = await GetRaiseOverLimperAction(heroBets, playerState);
            if (!string.IsNullOrEmpty(action))
            {
                responseAction.Action = action;
                responseAction.HandSituation = HandSituation.RaiseOverLimper;
                responseAction.IsSecondAction = true;
            }
        }

        //GET 3BET
        if (responseAction.Action is null)
        {
            if (playerState.Players.Count(a => a.Bet > 1) >= 1 && !UserHandHelper.Exist4Bet(playerState))
            {
                var action = await Get3BetAction(heroBets, playerState.Players.First(w => w.Bet > 1).Position, playerState);
                if (!string.IsNullOrEmpty(action))
                {
                    responseAction.Action = action;
                    responseAction.HandSituation = HandSituation.ThreeBet;
                    responseAction.IsSecondAction = true;
                }
            }
        }

        if (string.IsNullOrEmpty(responseAction.Action))
        {
            responseAction.Action = "None";
            responseAction.HandSituation = HandSituation.None;
            responseAction.IsSecondAction = false;
        }

        if (responseAction.Action.Contains("Call"))
            playerState.HandSituation = HandSituation.Call;

        return new SetPreflopActionUseCaseResponse
        {
            ResponseAction = responseAction,
            PlayerState = playerState
        };
    }

    private async Task<string> GetHeroCallOpenRaiseAndGetSqueezeAction(Dictionary<TablePosition, decimal> preflopTablePosition, TablePosition raiserPosition, TablePosition squeezerPosition, PlayerGameState playerState)
    {
        var responseAction = new GetActionHeroCallOpenRaiseAndGetSqueezeResponse();

        var betsPosition = preflopTablePosition
                            .Where(w => w.Key != TablePosition.BigBlind && w.Key != TablePosition.None)
                            .Select(s => s.Key).ToList();

        var count = preflopTablePosition.Count(c => c.Value > playerState.CurrentBet);
        var raiserCall = false;

        if (count > 1)
            raiserCall = true;

        var request = new GetActionHeroCallOpenRaiseAndGetSqueezeRequest
        {
            Hand = UserHandHelper.SetHandValue(playerState),
            Position = playerState.Position,
            SqueezerPosition = squeezerPosition,
            RaiserPosition = raiserPosition,
            RaiserFolds = !raiserCall
        };

        responseAction = await _heroCallOpenRaiseAndGetSqueezeUseCase.Execute(request);

        return responseAction.Action;
    }

    private async Task<string> GetHero3BetAndOpenRaiser4BetAction(Dictionary<TablePosition, decimal> preflopTablePosition, TablePosition raiserPosition, PlayerGameState playerState)
    {
        var responseAction = new GetActionHero3BetAndOpenRaiser4BetResponse();

        var betsPosition = preflopTablePosition
                            .Where(w => w.Key != TablePosition.BigBlind && w.Key != TablePosition.None)
                            .Select(s => s.Key).ToList();


        if (!preflopTablePosition.TryGetValue(raiserPosition, out var betSize))
            return string.Empty;

        var request = new GetActionHero3BetAndOpenRaiser4BetRequest
        {
            Hand = UserHandHelper.SetHandValue(playerState),
            Position = playerState.Position,
            RaiserPosition = raiserPosition,
            BetSize = betSize,
            IsGreater = SetIsGreater(playerState.Position, raiserPosition, betSize)
        };

        responseAction = await _hero3BetAndOpenRaiser4BetUseCase.Execute(request);

        return responseAction.Action;
    }

    private bool? SetIsGreater(TablePosition position, TablePosition raiserPosition, decimal betSize)
    {
        var thresholds = new Dictionary<(TablePosition, TablePosition), decimal?>
        {
            // BigBlind vs others
            {(TablePosition.BigBlind, TablePosition.SmallBlind), 20m},
            {(TablePosition.BigBlind, TablePosition.Button), 23.5m},
            {(TablePosition.BigBlind, TablePosition.CutOff), 22.5m},
            {(TablePosition.BigBlind, TablePosition.Middle), 22m},
            {(TablePosition.BigBlind, TablePosition.Early), null},

            // SmallBlind vs others
            {(TablePosition.SmallBlind, TablePosition.Button), 23.5m},
            {(TablePosition.SmallBlind, TablePosition.CutOff), null},
            {(TablePosition.SmallBlind, TablePosition.Middle), 20.5m},
            {(TablePosition.SmallBlind, TablePosition.Early), 20.5m},

            // Button vs others
            {(TablePosition.Button, TablePosition.CutOff), null},
            {(TablePosition.Button, TablePosition.Middle), 19m},
            {(TablePosition.Button, TablePosition.Early), 19.5m},

            // CutOff vs others
            {(TablePosition.CutOff, TablePosition.Middle), 19m},
            {(TablePosition.CutOff, TablePosition.Early), 19m},

            // Middle vs others
            {(TablePosition.Middle, TablePosition.Early), 19m}
        };

        if (!thresholds.TryGetValue((position, raiserPosition), out var threshold))
            return false;

        return threshold.HasValue ? betSize > threshold : null;
    }

    private async Task<string> GetOpenRaiseVs3BetAction(Dictionary<TablePosition, decimal> preflopTablePosition, TablePosition villainPosition, PlayerGameState playerState)
    {
        var responseAction = new GetActionVs3BetUseCaseResponse();

        if (preflopTablePosition.Any(a => a.Value > playerState.CurrentBet))
        {
            var openRaiseCommand = new GetActionVs3BetUseCaseRequest
            {
                Hand = UserHandHelper.SetHandValue(playerState),
                Position = playerState.Position,
                ThreeBetPosition = villainPosition
            };

            responseAction = await _vs3BetUseCase.Execute(openRaiseCommand);
        }

        return responseAction.Action;
    }

    private async Task<string> GetOpenRaiseVs3BetAndCallAction(Dictionary<TablePosition, decimal> preflopTablePosition, PlayerGameState playerState)
    {

        var responseAction = new GetActionVs3BetAndCallUseCaseResponse();

        if (preflopTablePosition.Any(a => a.Value > playerState.CurrentBet))
        {
            var command = new GetActionVs3BetAndCallUseCaseRequest
            {
                Hand = UserHandHelper.SetHandValue(playerState),
                Position = playerState.Position,
                ThreeBetPosition = preflopTablePosition.First(f => f.Value > playerState.CurrentBet).Key,
                CallerPosition = preflopTablePosition.Last(l => l.Value > playerState.CurrentBet).Key
            };

            responseAction = await _vs3BetAndCallUseCase.Execute(command);
        }

        return responseAction.Action;
    }

    private async Task<string?> GetSqueezeAction(Dictionary<TablePosition, decimal> preflopTablePosition, PlayerGameState playerState)
    {
        var responseAction = new GetActionSqueezeResponse();

        var bet = 2m;
        TablePosition raiser = TablePosition.None;
        TablePosition caller = TablePosition.None;

        var betsPosition = preflopTablePosition
                            .Where(w => w.Key != TablePosition.BigBlind && w.Key != TablePosition.None)
                            .Select(s => s.Key).ToList().OrderBy(o => o);

        foreach (var item in betsPosition)
        {
            if (preflopTablePosition.First(w => w.Key == item).Value == bet && item != raiser && raiser != TablePosition.None)
            {
                caller = item;
            }

            if (preflopTablePosition.First(w => w.Key == item).Value > 1 && raiser == TablePosition.None)
            {
                bet = preflopTablePosition.First(w => w.Key == item).Value;
                raiser = item;
            }
        }

        if (raiser != TablePosition.None && caller != TablePosition.None)
        {
            var getSqueezeRequest = new GetActionSqueezeRequest
            {
                Hand = UserHandHelper.SetHandValue(playerState),
                Position = playerState.Position,
                OpenRaiserPosition = raiser,
                CallerPosition = caller
            };

            responseAction = await _squeezeUseCase.Execute(getSqueezeRequest);
        }

        return responseAction.Action;
    }

    private async Task<string> Cold4BetAction(Dictionary<TablePosition, decimal> dictionary, PlayerGameState playerState)
    {
        var responseAction = new GetActionCold4BetUseCaseResponse();
        var openRaiseValue = 0m;
        var openRaisePosition = TablePosition.None;

        var threeBetValue = 0m;
        var threeBetPosition = TablePosition.None;

        foreach (var item in dictionary.OrderBy(o => o.Key))
        {
            if (item.Value > 1 && openRaiseValue == 0)
            {
                openRaiseValue = item.Value;
                openRaisePosition = item.Key;
            }

            if (item.Value > 1 && item.Value > openRaiseValue && threeBetValue == 0)
            {
                threeBetValue = item.Value;
                threeBetPosition = item.Key;
            }

        }

        if (openRaiseValue > 0 && threeBetValue > 0)
        {
            var Cold4BetCommand = new GetActionCold4BetUseCaseRequest
            {
                Hand = UserHandHelper.SetHandValue(playerState),
                Position = playerState.Position,
                RaiserPosition = openRaisePosition,
                ThreeBetPosition = threeBetPosition
            };

            responseAction = await _cold4BetUseCase.Execute(Cold4BetCommand);
        }

        return responseAction.Action;
    }


    private async Task<string> GetOpenRaiseAction(Dictionary<TablePosition, decimal> preflopTablePosition, PlayerGameState playerState)
    {
        var responseAction = new GetActionOpenRaiseUseCaseResponse();

        var cont = 0;
        var betsPosition = preflopTablePosition
                            .Where(w => w.Key != TablePosition.BigBlind && w.Key != TablePosition.SmallBlind && w.Key != TablePosition.None)
                            .Select(s => s.Key).ToList().OrderBy(o => o);

        foreach (var item in betsPosition)
        {
            if (preflopTablePosition.First(w => w.Key == item).Value > 0)
                cont++;
        }

        string? action = null;

        if (cont == 0)
        {
            var smallBlind = playerState.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind);

            if (playerState.Position == TablePosition.BigBlind && smallBlind?.Bet == 1)
            {
                var openRaiseVsLimpSBCommand = new GetActionRaiseVsSBLimpRequest
                {
                    Hand = UserHandHelper.SetHandValue(playerState),
                    Position = playerState.Position,
                    LimperPosition = TablePosition.SmallBlind,
                    ThreeBetPosition = null
                };

                var resultSBCall = await _raiseVsSBLimpUseCase.Execute(openRaiseVsLimpSBCommand);
                action = resultSBCall.Action;
            }

            if (playerState.Position == TablePosition.BigBlind && smallBlind is not null && smallBlind.Bet > 1)
            {
                var openRaiseVsLimpSBCommand = new GetActionRaiseVsSBLimpRequest
                {
                    Hand = UserHandHelper.SetHandValue(playerState),
                    Position = playerState.Position,
                    LimperPosition = TablePosition.SmallBlind,
                    ThreeBetPosition = TablePosition.SmallBlind
                };

                var resultSBRaise = await _raiseVsSBLimpUseCase.Execute(openRaiseVsLimpSBCommand);
                action = resultSBRaise.Action;
            }

            if (action == null)
            {
                var openRaiseCommand = new GetActionOpenRaiseUseCaseRequest
                {
                    Hand = UserHandHelper.SetHandValue(playerState),
                    Position = playerState.Position
                };

                var result = await _openRaiseUseCase.Execute(openRaiseCommand);
                action = result.Action;
            }
        }

        return action ?? string.Empty;
    }
    private async Task<string> GetRaiseOverLimperAction(Dictionary<TablePosition, decimal> preflopTablePosition, PlayerGameState playerState)
    {
        var responseAction = new GetActionRaiseOverLimperUseCaseResponse();
        TablePosition? limperPosition = null;

        if (!playerState.Players.Any(a => a.Bet > 1))
        {
            if ((preflopTablePosition.ContainsKey(TablePosition.SmallBlind) && preflopTablePosition[TablePosition.SmallBlind] == 1) ||
                (preflopTablePosition.ContainsKey(TablePosition.Button) && preflopTablePosition[TablePosition.Button] == 1) ||
                (preflopTablePosition.ContainsKey(TablePosition.CutOff) && preflopTablePosition[TablePosition.CutOff] == 1) ||
                (preflopTablePosition.ContainsKey(TablePosition.Middle) && preflopTablePosition[TablePosition.Middle] == 1) ||
                (preflopTablePosition.ContainsKey(TablePosition.Early) && preflopTablePosition[TablePosition.Early] == 1))
            {

                if (playerState.Position != TablePosition.SmallBlind
                    && preflopTablePosition.TryGetValue(TablePosition.SmallBlind, out var smallBlindBet)
                    && smallBlindBet == 1
                    && preflopTablePosition.Count == 1)
                {
                    limperPosition = TablePosition.SmallBlind;
                }

                var openRaiseCommand = new GetActionRaiseOverLimperUseCaseRequest
                {
                    Hand = UserHandHelper.SetHandValue(playerState),
                    Position = playerState.Position,
                    LimperPosition = limperPosition
                };

                responseAction = await _raiseOverLimperUseCase.Execute(openRaiseCommand);
            }
        }

        return responseAction.Action;
    }

    private async Task<string> Get3BetAction(Dictionary<TablePosition, decimal> preflopTablePosition, TablePosition villainPosition, PlayerGameState playerState)
    {
        var responseAction = new GetAction3BetUseCaseResponse();

        if (preflopTablePosition.Any(a => a.Value > 1))
        {
            var openRaiseCommand = new GetAction3BetUseCaseRequest
            {
                Hand = UserHandHelper.SetHandValue(playerState),
                Position = playerState.Position,
                OpenRaiserPosition = villainPosition
            };

            responseAction = await _threeBetUseCase.Execute(openRaiseCommand);

        }

        return responseAction.Action;
    }
}
