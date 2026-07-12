using System.Text;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

public sealed class UiUpdateService : IUiUpdateService
{
    private readonly IActionFormatter _actionFormatter;
    private readonly IPokerCalculator _pokerCalculator;
    private readonly Func<decimal> _bigBlindProvider;
    private readonly GameLoopStateMachine _gameLoopStateMachine;
    private readonly ImageCropperService _imageCropper;
    private readonly IGameCoordinator _coordinator;
    private IFrmOverlay? _overlay;
    private UiUpdateControls? _controls;
    private List<Card> _cards = new();

    public UiUpdateService(
        IActionFormatter actionFormatter,
        IPokerCalculator pokerCalculator,
        GameLoggerService gameLogger,
        GameLoopStateMachine gameLoopStateMachine,
        ImageCropperService imageCropper,
        IGameCoordinator coordinator)
        : this(actionFormatter, pokerCalculator,
              gameLogger is not null ? () => gameLogger.CurrentBigBlind : () => 0.50m,
              gameLoopStateMachine, imageCropper, coordinator)
    {
    }

    public UiUpdateService(
        IActionFormatter actionFormatter,
        IPokerCalculator pokerCalculator,
        Func<decimal> bigBlindProvider,
        GameLoopStateMachine gameLoopStateMachine,
        ImageCropperService imageCropper,
        IGameCoordinator coordinator)
    {
        _actionFormatter = actionFormatter ?? throw new ArgumentNullException(nameof(actionFormatter));
        _pokerCalculator = pokerCalculator ?? throw new ArgumentNullException(nameof(pokerCalculator));
        _bigBlindProvider = bigBlindProvider ?? (() => 0.50m);
        _gameLoopStateMachine = gameLoopStateMachine ?? throw new ArgumentNullException(nameof(gameLoopStateMachine));
        _imageCropper = imageCropper ?? throw new ArgumentNullException(nameof(imageCropper));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    public void Attach(UiUpdateControls controls) => _controls = controls;
    public void Detach() => _controls = null;
    public void SetCardList(List<Card> cards) => _cards = cards ?? new List<Card>();
    public void SetOverlay(IFrmOverlay overlay) => _overlay = overlay;

    public void UpdateResults(PlayerGameState state, PokerCalculationResult result,
        ResponseAction responseAction, string tableHand, string tableName)
    {
        var controls = _controls;
        if (controls is null) return;
        if (state is null) return;

        state.HandSituation = responseAction.HandSituation;

        if (_gameLoopStateMachine.IsPreflop)
            UpdateResumeTextForPreflop(controls, state, result, responseAction, tableHand);

        if (_gameLoopStateMachine.IsFlop)
            UpdateResumeTextForFlop(controls, state);

        UpdatePlayerIndicators(controls, state, tableHand, tableName);
        UpdateCardImages(controls, state);

        controls.LbAction.Text = responseAction?.Action ?? string.Empty;
        controls.LbPositionAction.Text = state.HandSituation.ToString() ?? string.Empty;

        if (_overlay is not null)
        {
            _overlay.UpdateSituacion(controls.LbPositionAction.Text);

            var actionText = responseAction?.Action ?? string.Empty;
            var villains = state.Players.Where(p => p.Name != "P0").ToList();
            _overlay.UpdateAction(
                _actionFormatter.EnrichActionWithBBAmount(actionText, villains, _bigBlindProvider()));
        }
    }

    private void UpdateResumeTextForPreflop(UiUpdateControls controls, PlayerGameState state,
        PokerCalculationResult result, ResponseAction responseAction, string tableHand)
    {
        double preflopEquity = 0;
        if (state.HoleCard1Rank > 0 && state.HoleCard2Rank > 0)
        {
            try
            {
                var heroCards = new List<CardDataOuts>
                {
                    new((Suit)state.HoleCard1Suit, (Rank)state.HoleCard1Rank),
                    new((Suit)state.HoleCard2Suit, (Rank)state.HoleCard2Rank)
                };
                int voluntaryBettors = state.Players.Count(p => p.Active && p.Bet > 1m);
                var numOpp = Math.Max(1, voluntaryBettors);
                var preflopResult = _pokerCalculator.Calculate(
                    heroCards, new List<CardDataOuts>(),
                    state.PotSize,
                    state.Players.Max(m => m.Bet),
                    numOpponents: numOpp,
                    heroStack: state.HeroStack,
                    villainStack: GetVillainStack(state),
                    handSituation: state.HandSituation.ToString(),
                    opponentProfile: _coordinator.GetActiveVillainProfile(state));
                preflopEquity = preflopResult.EquityPercentage;

                double positionAdjust = state.Position switch
                {
                    TablePosition.Button => 3.0,
                    TablePosition.CutOff => 1.0,
                    TablePosition.Middle => 0.0,
                    TablePosition.Early => -2.0,
                    TablePosition.SmallBlind => -3.0,
                    TablePosition.BigBlind => -1.0,
                    _ => 0.0
                };
                preflopEquity = Math.Max(0, Math.Min(100, preflopEquity + positionAdjust));
            }
            catch { }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Hand #{tableHand}: Hold'em No Limit");
        sb.AppendLine($"#{state.Players.FirstOrDefault(d => d.Dealer)?.Name ?? "Hero"} is the Dealer");
        sb.AppendLine($"{state.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "Hero"}: posts small blind");
        sb.AppendLine($"{state.Players.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "Hero"}: posts big blind");
        sb.AppendLine($"Pot: {state.PotSize}");
        sb.AppendLine($"*** STATISTICS ***");
        sb.AppendLine($"PotOdds: {(decimal)result.PotOddsPercentage}%");
        sb.AppendLine($"Equity: {preflopEquity:F1}%");
        sb.AppendLine($"Should Call: {result.ShouldCall}");
        sb.AppendLine("*** HOLE CARDS ***");
        sb.AppendLine($"Dealt to Hero [{state.HoleCard1Face} {state.HoleCard2Face}]");

        foreach (TablePosition position in Enum.GetValues(typeof(TablePosition)))
        {
            if (position != TablePosition.None && position <= state.Position)
            {
                var player = state.Players.FirstOrDefault(f => f.Position == position);
                string name = player?.Name ?? "Hero";
                string actionLine = string.Empty;

                if (name != "Hero")
                    actionLine = player?.Bet == null ? "folds" : $"bets/calls {player.Bet}";
                else
                {
                    if (position == state.Position)
                    {
                        sb.AppendLine($"Hand Situation: {state.HandSituation}");
                        actionLine = responseAction?.Action ?? string.Empty;
                    }
                }

                if (!string.IsNullOrWhiteSpace(actionLine))
                    sb.AppendLine($"{name}: {actionLine}");
            }
        }

        var heroAction = responseAction?.Action ?? "No action";
        var heroSituation = state.HandSituation;
        sb.AppendLine($"*** HERO ACTION ***");
        sb.AppendLine($"Position: {state.Position}  |  Situation: {heroSituation}");
        sb.AppendLine($"Equity preflop: {preflopEquity:F1}%");

        var villainsSnapshot = state.Players.Where(p => p.Name != "P0").ToList();
        var displayAction = _actionFormatter.EnrichActionWithBBAmount(heroAction, villainsSnapshot, _bigBlindProvider());
        sb.AppendLine($"▶ DECISIÓN: {displayAction}");

        controls.TbResume.AppendText(sb.ToString() + Environment.NewLine);
    }

    private void UpdateResumeTextForFlop(UiUpdateControls controls, PlayerGameState state)
    {
        controls.TbResume.Text += "*** FLOP *** [";
        var countFlop = 0;
        foreach (var card in state.BoardCards.Where(w => w.Position == BoardPosition.Flop))
        {
            countFlop++;
            if (countFlop == 3)
                controls.TbResume.Text += $"{card.Name}]";
            else
                controls.TbResume.Text += $"{card.Name} ";
        }
    }

    private void UpdatePlayerIndicators(UiUpdateControls controls, PlayerGameState state,
        string tableHand, string tableName)
    {
        controls.PbButtonPlayerOne.BackColor = Color.Transparent;
        controls.PbButtonPlayerTwo.BackColor = Color.Transparent;
        controls.PbButtonPlayerThree.BackColor = Color.Transparent;
        controls.PbButtonPlayerFour.BackColor = Color.Transparent;
        controls.PbButtonPlayerFive.BackColor = Color.Transparent;
        controls.PbButtonHero.BackColor = Color.Transparent;

        controls.LbNamePlayerOne.Text = state.Players.FirstOrDefault(f => f.Name == "P1")?.Alias;
        controls.LbNamePlayerTwo.Text = state.Players.FirstOrDefault(f => f.Name == "P2")?.Alias;
        controls.LbNamePlayerThree.Text = state.Players.FirstOrDefault(f => f.Name == "P3")?.Alias;
        controls.LbNamePlayerFour.Text = state.Players.FirstOrDefault(f => f.Name == "P4")?.Alias;
        controls.LbNamePlayerFive.Text = state.Players.FirstOrDefault(f => f.Name == "P5")?.Alias;

        controls.LbBetPlayerOne.Text = state.Players.FirstOrDefault(f => f.Name == "P1")?.Bet.ToString();
        controls.LbBetPlayerTwo.Text = state.Players.FirstOrDefault(f => f.Name == "P2")?.Bet.ToString();
        controls.LbBetPlayerThree.Text = state.Players.FirstOrDefault(f => f.Name == "P3")?.Bet.ToString();
        controls.LbBetPlayerFour.Text = state.Players.FirstOrDefault(f => f.Name == "P4")?.Bet.ToString();
        controls.LbBetPlayerFive.Text = state.Players.FirstOrDefault(f => f.Name == "P5")?.Bet.ToString();

        controls.LbBetHero.Text = state.CurrentBet.ToString();
        controls.LbPot.Text = state.PotSize.ToString();

        if (state.Players.FirstOrDefault(f => f.Name == "P1")?.Dealer == true)
            controls.PbButtonPlayerOne.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P2")?.Dealer == true)
            controls.PbButtonPlayerTwo.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P3")?.Dealer == true)
            controls.PbButtonPlayerThree.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P4")?.Dealer == true)
            controls.PbButtonPlayerFour.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P5")?.Dealer == true)
            controls.PbButtonPlayerFive.BackColor = Color.Red;
        if (state.IsDealer == true)
            controls.PbButtonHero.BackColor = Color.Red;

        controls.LbTableHand.Text = tableHand;
        controls.LbTableName.Text = tableName;
    }

    private void UpdateCardImages(UiUpdateControls controls, PlayerGameState state)
    {
        controls.PbHeroCard0.Image = GetCardImage(state.HoleCard1Face);
        controls.PbHeroCard1.Image = GetCardImage(state.HoleCard2Face);

        controls.PbBoard1.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 1)?.Name);
        controls.PbBoard2.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 2)?.Name);
        controls.PbBoard3.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 3)?.Name);
        controls.PbBoard4.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 4)?.Name);
        controls.PbBoard5.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 5)?.Name);
    }

    private Image? GetCardImage(string? cardName)
    {
        if (string.IsNullOrEmpty(cardName))
            return null;

        var card = _cards.FirstOrDefault(x => x.Id.Contains(cardName));
        return card != null ? _imageCropper.Base64ToImage(card.ImageBase64) : null;
    }

    private static decimal GetVillainStack(PlayerGameState state)
    {
        var activeVillains = state.Players
            .Where(p => p.Active && !string.IsNullOrEmpty(p.Name) && p.Name != "P0");
        var villainStack = activeVillains.Any() ? activeVillains.Max(p => p.Stack) : 0;

        if (villainStack <= 0 && state.HeroStack > 0)
            return state.HeroStack;

        return villainStack;
    }
}
