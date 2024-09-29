using OpenScrape.App.Aplication.UseCases.Actions;
using OpenScrape.App.Entities;
using OpenScrape.App.Enums;
using OpenScrape.App.Helpers;

namespace OpenScrape.App.Aplication
{
    public class SetPreflopActionUseCase : ISetPreflopActionUseCase
    {
        readonly IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase _heroCallOpenRaiseAndGetSqueezeUseCase = new GetActionHeroCallOpenRaiseAndGetSqueezeUseCase();
        readonly IGetActionHero3BetAndOpenRaiser4BetUseCase _hero3BetAndOpenRaiser4BetUseCase = new GetActionHero3BetAndOpenRaiser4BetUseCase();
        readonly IGetActionVs3BetUseCaseUseCase _vs3BetUseCase = new GetActionVs3BetUseCaseUseCase();
        readonly IGetActionVs3BetAndCallUseCase _vs3BetAndCallUseCase = new GetActionVs3BetAndCallUseCase();
        readonly IGetActionSqueezeUseCase _squeezeUseCase = new GetActionSqueezeUseCase();
        readonly IGetActionOpenRaiseUseCase _openRaiseUseCase = new GetActionOpenRaiseUseCase();
        readonly IGetActionRaiseOverLimperUseCase _raiseOverLimperUseCase = new GetActionRaiseOverLimperUseCase();
        readonly IGetAction3BetUseCase _threeBetUseCase = new GetAction3BetUseCase();
        readonly IGetActionRaiseVsSBLimpUseCase _raiseVsSBLimpUseCase = new GetActionRaiseVsSBLimpUseCase();
        readonly IGetActionCold4BetUseCase _cold4BetUseCase = new GetActionCold4BetUseCase();

        public SetPreflopActionUseCase(IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase heroCallOpenRaiseAndGetSqueezeUseCase,
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

        public SetPreflopActionUseCaseResponse Execute(SetPreflopActionUseCaseRequest request)
        {
            if (request.ResponseAction.IsSecondAction)
            {
                if (request.ResponseAction.Action is not null && request.ScrapeResult.HeroAction == HeroAction.Call)
                {
                    var action = GetHeroCallOpenRaiseAndGetSqueezeAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult.DataPlayer.First(w => w.Bet > request.ScrapeResult.U0Bet && w.Position > request.ScrapeResult.P0Position).Position, request.ScrapeResult);
                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.VsSqueeze : HeroAction.None;
                        request.ResponseAction.IsSecondAction = false;
                    };
                }

                if (request.ResponseAction.Action is not null && request.ScrapeResult.HeroAction == HeroAction.ThreeBet)
                {
                    var action = GetHero3BetAndOpenRaiser4BetAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult.DataPlayer.First(w => w.Bet > request.ScrapeResult.U0Bet).Position, request.ScrapeResult);
                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.FourBet : HeroAction.None;
                        request.ResponseAction.IsSecondAction = false;
                    };
                }

                if (request.ResponseAction.Action is not null && (request.ScrapeResult.HeroAction == HeroAction.OpenRaise || request.ScrapeResult.HeroAction == HeroAction.RaiseOverLimper))
                {
                    var action = GetOpenRaiseVs3BetAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult.DataPlayer.First(w => w.Bet >= 1).Position, request.ScrapeResult);
                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.OpenRaiseVs3Bet : HeroAction.None;
                        request.ResponseAction.IsSecondAction = false;
                    };
                }

                if (request.ResponseAction.Action is not null && (request.ScrapeResult.HeroAction == HeroAction.OpenRaise || request.ScrapeResult.HeroAction == HeroAction.RaiseOverLimper) && UserHandHelper.Exist4Bet(request.ScrapeResult))
                {
                    var action = GetOpenRaiseVs3BetAndCallAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult);
                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.OpenRaiseVs3BetAndCall : HeroAction.None;
                        request.ResponseAction.IsSecondAction = false;
                    };
                }
            }


            //SQUEEZE
            if (request.ResponseAction.Action is null)
            {
                var action = GetSqueezeAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult);
                if (!string.IsNullOrEmpty(action))
                {
                    request.ResponseAction.Action = action;
                    request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.Squeeze : HeroAction.None;
                    request.ResponseAction.IsSecondAction = true;
                };
            }


            //OPEN RAISE
            if (request.ResponseAction.Action is null)
            {
                var action = GetOpenRaiseAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult);
                if (!string.IsNullOrEmpty(action))
                {
                    request.ResponseAction.Action = action;
                    request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.OpenRaise : HeroAction.None;
                    request.ResponseAction.IsSecondAction = true;
                };
            }

            //COLD 4BET
            if (request.ResponseAction.Action is null)
            {
                var action = Cold4BetAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult);
                if (!string.IsNullOrEmpty(action))
                {
                    request.ResponseAction.Action = action;
                    request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.Cold4Bet : HeroAction.None;
                    request.ResponseAction.IsSecondAction = true;
                };
            }

            //GET RAISE OVER LIMPER
            if (request.ResponseAction.Action is null)
            {
                var action = GetRaiseOverLimperAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult);
                if (!string.IsNullOrEmpty(action))
                {
                    request.ResponseAction.Action = action;
                    request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.RaiseOverLimper : HeroAction.None;
                    request.ResponseAction.IsSecondAction = true;
                };
            }

            //GET 3BET
            if (request.ResponseAction.Action is null)
            {
                if (request.ScrapeResult.DataPlayer.Count(a => a.Bet > 1) >= 1 && UserHandHelper.Exist4Bet(request.ScrapeResult))
                {
                    var action = Get3BetAction(request.PreflopHeroPosition[request.ScrapeResult.P0Position], request.ScrapeResult.DataPlayer.First(w => w.Bet > 1).Position, request.ScrapeResult);
                    if (!string.IsNullOrEmpty(action))
                    {
                        request.ResponseAction.Action = action;
                        request.ResponseAction.HeroAction = action != "Fold" ? HeroAction.ThreeBet : HeroAction.None;
                        request.ResponseAction.IsSecondAction = true;
                    };
                }
            }

            if (string.IsNullOrEmpty(request.ResponseAction?.Action))
            {

                request.ResponseAction.Action = "None";
                request.ResponseAction.HeroAction = HeroAction.None;
                request.ResponseAction.IsSecondAction = false;

            }

            if (request.ResponseAction.Action.Contains("Call"))
                request.ScrapeResult.HeroAction = HeroAction.Call;


            return new SetPreflopActionUseCaseResponse
            {
                ResponseAction = request.ResponseAction,
                ScrapeResult = request.ScrapeResult
            };
        }

        private string GetHeroCallOpenRaiseAndGetSqueezeAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, HeroPosition squeezerPosition, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetActionHeroCallOpenRaiseAndGetSqueezeResponse();

            var betsPosition = preflopHeroPosition
                                .Where(w => w.Key != HeroPosition.BigBlind && w.Key != HeroPosition.None)
                                .Select(s => s.Key).ToList();


            var count = preflopHeroPosition.Count(c => c.Value > scrapeResult.U0Bet);
            var raiserCall = false;

            if (count > 1)
                raiserCall = true;

            var request = new GetActionHeroCallOpenRaiseAndGetSqueezeRequest
            {
                Hand = UserHandHelper.SetHandValue(scrapeResult),
                Position = scrapeResult.P0Position,
                SqueezerPosition = squeezerPosition,
                RaiserCall = raiserCall
            };

            responseAction = _heroCallOpenRaiseAndGetSqueezeUseCase.Execute(request);

            return responseAction.Action;
        }

        private string GetHero3BetAndOpenRaiser4BetAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, HeroPosition raiserPosition, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetActionHero3BetAndOpenRaiser4BetResponse();

            var betsPosition = preflopHeroPosition
                                .Where(w => w.Key != HeroPosition.BigBlind && w.Key != HeroPosition.None)
                                .Select(s => s.Key).ToList();


            var request = new GetActionHero3BetAndOpenRaiser4BetRequest
            {
                Hand = UserHandHelper.SetHandValue(scrapeResult),
                Position = scrapeResult.P0Position,
                RaiserPosition = raiserPosition,
                BetSize = preflopHeroPosition.First(f => f.Key == raiserPosition).Value
            };

            responseAction = _hero3BetAndOpenRaiser4BetUseCase.Execute(request);

            return responseAction.Action;
        }

        private string GetOpenRaiseVs3BetAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, HeroPosition villainPosition, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetActionVs3BetUseCaseResponse();

            if (preflopHeroPosition.Any(a => a.Value > scrapeResult.U0Bet))
            {
                var openRaiseCommand = new GetActionVs3BetUseCaseRequest
                {
                    Hand = UserHandHelper.SetHandValue(scrapeResult),
                    Position = scrapeResult.P0Position,
                    VillainPosition = villainPosition
                };

                responseAction = _vs3BetUseCase.Execute(openRaiseCommand);
            }

            return responseAction.Action;
        }

        private string GetOpenRaiseVs3BetAndCallAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, TableScrapeResult scrapeResult)
        {

            var responseAction = new GetActionVs3BetAndCallUseCaseResponse();

            if (preflopHeroPosition.Any(a => a.Value > scrapeResult.U0Bet))
            {
                var command = new GetActionVs3BetAndCallUseCaseRequest
                {
                    Hand = UserHandHelper.SetHandValue(scrapeResult),
                    Position = scrapeResult.P0Position,
                    VillainPosition = preflopHeroPosition.First(f => f.Value >  scrapeResult.U0Bet).Key,
                    CallerPosition = preflopHeroPosition.Last(l => l.Value > scrapeResult.U0Bet).Key
                };

                responseAction = _vs3BetAndCallUseCase.Execute(command);
            }

            return responseAction.Action;
        }

        private string? GetSqueezeAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetActionSqueezeResponse();

            var bet = 2m;
            HeroPosition raiser = HeroPosition.None;
            HeroPosition caller = HeroPosition.None;

            var betsPosition = preflopHeroPosition
                                .Where(w => w.Key != HeroPosition.BigBlind && w.Key != HeroPosition.None)
                                .Select(s => s.Key).ToList();

            foreach (var item in betsPosition)
            {
                if (preflopHeroPosition.First(w => w.Key == item).Value == bet && item != raiser && raiser != HeroPosition.None)
                {
                    caller = item;
                }

                if (preflopHeroPosition.First(w => w.Key == item).Value > 1 && raiser == HeroPosition.None)
                {
                    bet = preflopHeroPosition.First(w => w.Key == item).Value;
                    raiser = item;
                }
            }

            if (raiser != HeroPosition.None && caller != HeroPosition.None)
            {
                var getSqueezeRequest = new GetActionSqueezeRequest
                {
                    Hand = UserHandHelper.SetHandValue(scrapeResult),
                    Position =  scrapeResult.P0Position,
                    RaiserPosition = raiser,
                    CallerPosition = caller
                };

                responseAction = _squeezeUseCase.Execute(getSqueezeRequest);
            }

            return responseAction.Action;
        }

        private string GetOpenRaiseAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetActionOpenRaiseUseCaseResponse();

            var cont = 0;
            var betsPosition = preflopHeroPosition
                                .Where(w => w.Key != HeroPosition.BigBlind && w.Key != HeroPosition.SmallBlind && w.Key != HeroPosition.None)
                                .Select(s => s.Key).ToList();

            foreach (var item in betsPosition)
            {
                if (preflopHeroPosition.First(w => w.Key == item).Value > 0)
                    cont++;
            }

            string? action = null;

            if (cont == 0)
            {
                if (scrapeResult.P0Position == HeroPosition.BigBlind && scrapeResult.DataPlayer.First(f => f.Name == "P5").Bet == 1)
                {
                    var openRaiseVsLimpSBCommand = new GetActionRaiseVsSBLimpRequest
                    {
                        Hand = UserHandHelper.SetHandValue(scrapeResult),
                        Position = scrapeResult.P0Position,
                        OnlyCall = true
                    };

                    action = _raiseVsSBLimpUseCase.Execute(openRaiseVsLimpSBCommand).Action;
                }

                if (scrapeResult.P0Position == HeroPosition.BigBlind && scrapeResult.DataPlayer.First(f => f.Name == "P5").Bet > 1)
                {
                    var openRaiseVsLimpSBCommand = new GetActionRaiseVsSBLimpRequest
                    {
                        Hand = UserHandHelper.SetHandValue(scrapeResult),
                        Position = scrapeResult.P0Position,
                        OnlyCall = false
                    };

                    action = _raiseVsSBLimpUseCase.Execute(openRaiseVsLimpSBCommand).Action;
                }

                if (action == null)
                {
                    var openRaiseCommand = new GetActionOpenRaiseUseCaseRequest
                    {
                        Hand = UserHandHelper.SetHandValue(scrapeResult),
                        Position = scrapeResult.P0Position
                    };

                    action = _openRaiseUseCase.Execute(openRaiseCommand).Action;
                }
            }

            return action;
        }

        private string Cold4BetAction(Dictionary<HeroPosition, decimal> dictionary, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetActionCold4BetUseCaseResponse();
            var openRaiseValue = 0m;
            var openRaisePosition = HeroPosition.None;

            var threeBetValue = 0m;
            var threeBetPosition = HeroPosition.None;

            foreach (var item in dictionary)
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
                    Hand = UserHandHelper.SetHandValue(scrapeResult),
                    Position = scrapeResult.P0Position,
                    RaiserPosition = openRaisePosition,
                    ThreeBetVillainPosition = threeBetPosition
                };

                responseAction = _cold4BetUseCase.Execute(Cold4BetCommand);
            }

            return responseAction.Action;
        }

        private string GetRaiseOverLimperAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetActionRaiseOverLimperUseCaseResponse();

            if ((preflopHeroPosition.ContainsKey(HeroPosition.Button) && preflopHeroPosition[HeroPosition.Button] == 1) ||
                (preflopHeroPosition.ContainsKey(HeroPosition.CutOff) && preflopHeroPosition[HeroPosition.CutOff] == 1) ||
                (preflopHeroPosition.ContainsKey(HeroPosition.MiddlePosition) && preflopHeroPosition[HeroPosition.MiddlePosition] == 1) ||
                (preflopHeroPosition.ContainsKey(HeroPosition.EarlyPosition) && preflopHeroPosition[HeroPosition.EarlyPosition] == 1))
            {
                var openRaiseCommand = new GetActionRaiseOverLimperUseCaseRequest
                {
                    Hand = UserHandHelper.SetHandValue(scrapeResult),
                    Position = scrapeResult.P0Position
                };

                responseAction = _raiseOverLimperUseCase.Execute(openRaiseCommand);
            }

            return responseAction.Action;
        }

        private string Get3BetAction(Dictionary<HeroPosition, decimal> preflopHeroPosition, HeroPosition villainPosition, TableScrapeResult scrapeResult)
        {
            var responseAction = new GetAction3BetUseCaseResponse();

            if (preflopHeroPosition.Any(a => a.Value > 1))
            {
                var openRaiseCommand = new GetAction3BetUseCaseRequest
                {
                    Hand = UserHandHelper.SetHandValue(scrapeResult),
                    Position = scrapeResult.P0Position,
                    VillainPosition = villainPosition
                };

                responseAction = _threeBetUseCase.Execute(openRaiseCommand);

            }

            return responseAction.Action;
        }
    }
}
