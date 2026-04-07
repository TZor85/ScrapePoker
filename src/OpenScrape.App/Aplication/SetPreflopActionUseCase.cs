using OpenScrape.App.Aplication.UseCases.Actions;
using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;

namespace OpenScrape.App.Aplication;

public class SetPreflopActionUseCase : ISetPreflopActionUseCase
{
    private ActionScenarioUseCases _actionScenarioUseCases;

    readonly IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase _heroCallOpenRaiseAndGetSqueezeUseCase;
    readonly IGetActionHero3BetAndOpenRaiser4BetUseCase _hero3BetAndOpenRaiser4BetUseCase;
    readonly IGetActionVs3BetUseCaseUseCase _vs3BetUseCase;
    readonly IGetActionVs3BetAndCallUseCase _vs3BetAndCallUseCase;
    readonly IGetActionSqueezeUseCase _squeezeUseCase;
    readonly IGetActionOpenRaiseUseCase _openRaiseUseCase;
    readonly IGetActionRaiseOverLimperUseCase _raiseOverLimperUseCase;
    readonly IGetAction3BetUseCase _threeBetUseCase;
    readonly IGetActionRaiseVsSBLimpUseCase _raiseVsSBLimpUseCase;
    readonly IGetActionCold4BetUseCase _cold4BetUseCase;

    public SetPreflopActionUseCase(ActionScenarioUseCases actionScenarioUseCases)
    {
        _actionScenarioUseCases = actionScenarioUseCases;

        _heroCallOpenRaiseAndGetSqueezeUseCase = new GetActionHeroCallOpenRaiseAndGetSqueezeUseCase(_actionScenarioUseCases);
        _hero3BetAndOpenRaiser4BetUseCase = new GetActionHero3BetAndOpenRaiser4BetUseCase(_actionScenarioUseCases);
        _vs3BetUseCase = new GetActionVs3BetUseCaseUseCase(_actionScenarioUseCases);
        _vs3BetAndCallUseCase = new GetActionVs3BetAndCallUseCase(_actionScenarioUseCases);
        _squeezeUseCase = new GetActionSqueezeUseCase(_actionScenarioUseCases);
        _openRaiseUseCase = new GetActionOpenRaiseUseCase(_actionScenarioUseCases);
        _raiseOverLimperUseCase = new GetActionRaiseOverLimperUseCase(_actionScenarioUseCases);
        _threeBetUseCase = new GetAction3BetUseCase(_actionScenarioUseCases);
        _raiseVsSBLimpUseCase = new GetActionRaiseVsSBLimpUseCase(_actionScenarioUseCases);
        _cold4BetUseCase = new GetActionCold4BetUseCase(_actionScenarioUseCases);

    }

    public async Task<SetPreflopActionUseCaseResponse> Execute(SetPreflopActionUseCaseRequest request)
    {
        if (request.PlayerState != null)
        {
            if (request.ResponseAction.IsSecondAction)
            {
                if (request.ResponseAction.Action is not null && request.PlayerState.HandSituation == HandSituation.Call)
                {
                    var action = await GetHeroCallOpenRaiseAndGetSqueezeAction(
                                    request.PreflopHeroPosition[request.PlayerState.Position],
                                    request.PlayerState.Players.First(f => f.Bet == request.PlayerState.CurrentBet && f.Position < request.PlayerState.Position).Position,
                                    request.PlayerState.Players.First(w => w.Bet > request.PlayerState.CurrentBet && w.Position > request.PlayerState.Position).Position,
                                    request.PlayerState);

                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HandSituation = action != "Fold" ? HandSituation.VsSqueeze : HandSituation.None;
                        request.ResponseAction.IsSecondAction = false;
                    }

                }

                if (request.ResponseAction.Action is not null &&
                    (request.PlayerState.HandSituation == HandSituation.ThreeBet ||
                     request.PlayerState.HandSituation == HandSituation.Squeeze))
                {
                    var raiser = request.PlayerState.Players.FirstOrDefault(w => w.Bet > request.PlayerState.CurrentBet);
                    if (raiser != null)
                    {
                        var action = await GetHero3BetAndOpenRaiser4BetAction(request.PreflopHeroPosition[request.PlayerState.Position], raiser.Position, request.PlayerState);
                        if (!string.IsNullOrEmpty(action))
                        {
                            request.ResponseAction.Action = action;
                            request.ResponseAction.HandSituation = action != "Fold" ? HandSituation.FourBet : HandSituation.None;
                            request.ResponseAction.IsSecondAction = false;
                        }
                    }
                }

                if (request.ResponseAction.Action is not null && (request.PlayerState.HandSituation == HandSituation.OpenRaise || request.PlayerState.HandSituation == HandSituation.RaiseOverLimper))
                {
                    var action = await GetOpenRaiseVs3BetAction(request.PreflopHeroPosition[request.PlayerState.Position], request.PlayerState.Players.FirstOrDefault(w => w.Bet >= 1 && w.Position != TablePosition.BigBlind)?.Position ?? TablePosition.None, request.PlayerState);
                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HandSituation = HandSituation.OpenRaiseVs3Bet;
                        request.ResponseAction.IsSecondAction = false;
                    }

                }

                if (request.ResponseAction.Action is not null && (request.PlayerState.HandSituation == HandSituation.OpenRaise || request.PlayerState.HandSituation == HandSituation.RaiseOverLimper) && UserHandHelper.Exist4Bet(request.PlayerState))
                {
                    var action = await GetOpenRaiseVs3BetAndCallAction(request.PreflopHeroPosition[request.PlayerState.Position], request.PlayerState);
                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HandSituation = HandSituation.OpenRaiseVs3BetAndCall;
                        request.ResponseAction.IsSecondAction = false;
                    }

                }
            }
        }


        //SQUEEZE
        if (request.ResponseAction.Action is null)
        {
            var action = await GetSqueezeAction(request.PreflopHeroPosition[request.PlayerState.Position], request.PlayerState);
            if (!string.IsNullOrEmpty(action))
            {
                request.ResponseAction.Action = action;
                request.ResponseAction.HandSituation = HandSituation.Squeeze;
                request.ResponseAction.IsSecondAction = true;
            }
            ;
        }


        //OPEN RAISE
        if (request.ResponseAction.Action is null)
        {
            var action = await GetOpenRaiseAction(request.PreflopHeroPosition[request.PlayerState.Position], request.PlayerState);
            if (!string.IsNullOrEmpty(action))
            {
                request.ResponseAction.Action = action;
                request.ResponseAction.HandSituation = HandSituation.OpenRaise;
                request.ResponseAction.IsSecondAction = true;
            }
            ;
        }

        //COLD 4BET
        if (request.ResponseAction.Action is null)
        {
            var action = await Cold4BetAction(request.PreflopHeroPosition[request.PlayerState.Position], request.PlayerState);
            if (!string.IsNullOrEmpty(action))
            {
                request.ResponseAction.Action = action;
                request.ResponseAction.HandSituation = HandSituation.Cold4Bet;
                request.ResponseAction.IsSecondAction = true;
            }
            ;
        }

        //GET RAISE OVER LIMPER
        if (request.ResponseAction.Action is null)
        {
            var action = await GetRaiseOverLimperAction(request.PreflopHeroPosition[request.PlayerState.Position], request.PlayerState);
            if (!string.IsNullOrEmpty(action))
            {
                request.ResponseAction.Action = action;
                request.ResponseAction.HandSituation = HandSituation.RaiseOverLimper;
                request.ResponseAction.IsSecondAction = true;
            }
            ;
        }

        //GET 3BET
        if (request.ResponseAction.Action is null)
        {
            if (request.PlayerState.Players.Count(a => a.Bet > 1) >= 1 && !UserHandHelper.Exist4Bet(request.PlayerState))
            {
                var action = await Get3BetAction(request.PreflopHeroPosition[request.PlayerState.Position], request.PlayerState.Players.First(w => w.Bet > 1).Position, request.PlayerState);
                if (!string.IsNullOrEmpty(action))
                {
                    request.ResponseAction.Action = action;
                    request.ResponseAction.HandSituation = HandSituation.ThreeBet;
                    request.ResponseAction.IsSecondAction = true;
                }
                ;
            }
        }

        if (string.IsNullOrEmpty(request.ResponseAction?.Action))
        {

            request.ResponseAction.Action = "None";
            request.ResponseAction.HandSituation = HandSituation.None;
            request.ResponseAction.IsSecondAction = false;

        }

        if (request.ResponseAction.Action.Contains("Call"))
            request.PlayerState.HandSituation = HandSituation.Call;


        return new SetPreflopActionUseCaseResponse
        {
            ResponseAction = request.ResponseAction,
            PlayerState = request.PlayerState
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


        var betSize = preflopTablePosition.First(f => f.Key == raiserPosition).Value;

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
            if (playerState.Position == TablePosition.BigBlind && playerState.Players.First(f => f.Name == "P5").Bet == 1)
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

            if (playerState.Position == TablePosition.BigBlind && playerState.Players.First(f => f.Name == "P5").Bet > 1)
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

        return action;
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

                if (playerState.Position != TablePosition.SmallBlind && preflopTablePosition[TablePosition.SmallBlind] == 1 && preflopTablePosition.Count == 1)
                    limperPosition = TablePosition.SmallBlind;

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
