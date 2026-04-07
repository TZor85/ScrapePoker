using OpenScrape.App.Helpers;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Forms;

public class FrmHandDetail : Form
{
    private readonly RichTextBox _rtbDetail;

    public FrmHandDetail(HandRecord hand, decimal bigBlind)
    {
        Text = $"Hand #{hand.HandNumber} — {hand.HeroCard1} {hand.HeroCard2}";
        Size = new Size(520, 480);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(400, 300);
        BackColor = Color.FromArgb(30, 33, 45);
        Icon = null;
        ShowInTaskbar = false;

        _rtbDetail = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(30, 33, 45),
            ForeColor = Color.White,
            Font = new Font("Consolas", 10F),
            BorderStyle = BorderStyle.None,
            Text = ""
        };

        Controls.Add(_rtbDetail);

        FormatHandHistory(hand, bigBlind);
    }

    private void FormatHandHistory(HandRecord hand, decimal bigBlind)
    {
        var separator = "═══════════════════════════════════════════";
        var thinSeparator = "───────────────────────────────────────────";

        // Header
        Append(separator + "\n", AppThemeHelper.Accent);
        Append($"Hand #{hand.HandNumber} — {hand.Timestamp:yyyy-MM-dd HH:mm:ss} UTC\n", AppThemeHelper.Accent);
        Append($"Mesa: NL Holdem (${bigBlind / 2:0.00}/${bigBlind:0.00})\n", AppThemeHelper.Accent);
        Append($"Situación: {hand.Situation} | Oponentes: {hand.NumOpponents}\n", AppThemeHelper.Accent);
        Append(separator + "\n\n", AppThemeHelper.Accent);

        // Hero
        Append("Hero ", Color.White);
        Append($"[{hand.HeroCard1} {hand.HeroCard2}]", Color.White, bold: true);
        Append($" — {FormatPosition(hand.HeroPosition)} — Stack: ${hand.HeroStackStart:0.00}\n\n", Color.White);

        // Decisiones por street
        foreach (var decision in hand.Decisions)
        {
            string boardStr = FormatBoardForStreet(hand, decision.Street);
            Append($"*** {decision.Street.ToString().ToUpper()} *** {boardStr}\n", Color.DodgerBlue);

            // Stats línea 1: equity, pot odds, EV, SPR, outs
            var statsLine = $"  Equity: {decision.EquityPercent:0.0}% | Pot Odds: {decision.PotOddsPercent:0.0}% | EV: {decision.ExpectedValue:+0.00;-0.00}";
            if (decision.SPR > 0)
                statsLine += $" | SPR: {decision.SPR:0.0}";
            if (decision.TotalOuts > 0)
                statsLine += $" | Outs: {decision.TotalOuts}";
            Append(statsLine + "\n", AppThemeHelper.PrimaryLight);

            // Board texture si disponible
            if (!string.IsNullOrEmpty(decision.BoardTexture))
                Append($"  Board: {decision.BoardTexture}\n", AppThemeHelper.PrimaryLight);

            Append($"  Recomendado: {decision.RecommendedAction}\n", Color.White);

            // Reason si disponible y diferente de la acción recomendada
            if (!string.IsNullOrEmpty(decision.Reason) && decision.Reason != decision.RecommendedAction)
                Append($"  Razón: {decision.Reason}\n", Color.FromArgb(180, 180, 200));

            Append($"  Acción: {decision.ActionTaken} ${decision.BetSize:0.00} | Pot: ${decision.PotSizeAtDecision:0.00}\n\n", Color.DarkGoldenrod);
        }

        // Resultado
        Append(thinSeparator + "\n", AppThemeHelper.PrimaryLight);
        decimal profitLoss = hand.HeroStackEnd - hand.HeroStackStart;
        Color resultColor = hand.Result switch
        {
            HandResult.Won => AppThemeHelper.Success,
            HandResult.Lost => AppThemeHelper.Danger,
            _ => AppThemeHelper.PrimaryLight
        };
        Append($"RESULTADO: {hand.Result} ({profitLoss:+$0.00;-$0.00})\n", resultColor);
        Append($"Stack final: ${hand.HeroStackEnd:0.00}\n", Color.White);
        Append(separator + "\n", AppThemeHelper.Accent);

        _rtbDetail.Select(0, 0);
    }

    private static string FormatPosition(TablePosition pos) => pos switch
    {
        TablePosition.Button => "BTN",
        TablePosition.CutOff => "CO",
        TablePosition.Middle => "MP",
        TablePosition.Early => "EP",
        TablePosition.SmallBlind => "SB",
        TablePosition.BigBlind => "BB",
        _ => pos.ToString()
    };

    private static string FormatBoardForStreet(HandRecord hand, BoardPosition street)
    {
        string flop = hand.FlopCards.Count >= 3
            ? $"[{string.Join(" ", hand.FlopCards)}]"
            : "";

        return street switch
        {
            BoardPosition.Flop => flop,
            BoardPosition.Turn => $"{flop} [{hand.TurnCard}]",
            BoardPosition.River => hand.TurnCard != null
                ? $"[{string.Join(" ", hand.FlopCards)} {hand.TurnCard}] [{hand.RiverCard}]"
                : $"{flop} [{hand.RiverCard}]",
            _ => ""
        };
    }

    private void Append(string text, Color color, bool bold = false)
    {
        int start = _rtbDetail.TextLength;
        _rtbDetail.AppendText(text);
        _rtbDetail.Select(start, text.Length);
        _rtbDetail.SelectionColor = color;
        if (bold)
            _rtbDetail.SelectionFont = new Font(_rtbDetail.Font, FontStyle.Bold);
        _rtbDetail.SelectionLength = 0;
    }
}
