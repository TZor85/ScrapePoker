using Microsoft.Extensions.Logging;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services.Bankroll;
using OpenScrape.DecisionMaker.Interfaces;

namespace OpenScrape.App.Forms.Controls;

/// <summary>
/// UserControl que encapsula la pestaña Bankroll: historial de bankroll y métricas.
/// </summary>
public sealed class BankrollTabControl : UserControl
{
    private readonly IBankrollTrackerService _bankrollTracker;
    private readonly ILogger<BankrollTabControl> _logger;

    private DataGridView? _dgvBankrollHistory;
    private bool _bankrollLoaded;

    // Labels que se actualizan desde UpdateBankrollDashboard
    private Label? _lblBankrollCurrent;
    private Label? _lblBankrollPeak;
    private Label? _lblBankrollMaxDD;
    private Label? _lblWinRate;
    private Label? _lblStdDev;
    private Label? _lblTotalSessions;
    private Label? _lblTotalHands;
    private Label? _lblAvgSessionProfit;
    private Label? _lblRiskOfRuin;
    private Label? _lblRecommendation;

    public BankrollTabControl(
        IBankrollTrackerService bankrollTracker,
        ILogger<BankrollTabControl> logger)
    {
        _bankrollTracker = bankrollTracker ?? throw new ArgumentNullException(nameof(bankrollTracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize(
        DataGridView dgvBankrollHistory,
        TabPage tpBankroll,
        Label lblBankrollCurrent,
        Label lblBankrollPeak,
        Label lblBankrollMaxDD,
        Label lblWinRate,
        Label lblStdDev,
        Label lblTotalSessions,
        Label lblTotalHands,
        Label lblAvgSessionProfit,
        Label lblRiskOfRuin,
        Label lblRecommendation)
    {
        _dgvBankrollHistory = dgvBankrollHistory ?? throw new ArgumentNullException(nameof(dgvBankrollHistory));
        _lblBankrollCurrent = lblBankrollCurrent ?? throw new ArgumentNullException(nameof(lblBankrollCurrent));
        _lblBankrollPeak = lblBankrollPeak ?? throw new ArgumentNullException(nameof(lblBankrollPeak));
        _lblBankrollMaxDD = lblBankrollMaxDD ?? throw new ArgumentNullException(nameof(lblBankrollMaxDD));
        _lblWinRate = lblWinRate ?? throw new ArgumentNullException(nameof(lblWinRate));
        _lblStdDev = lblStdDev ?? throw new ArgumentNullException(nameof(lblStdDev));
        _lblTotalSessions = lblTotalSessions ?? throw new ArgumentNullException(nameof(lblTotalSessions));
        _lblTotalHands = lblTotalHands ?? throw new ArgumentNullException(nameof(lblTotalHands));
        _lblAvgSessionProfit = lblAvgSessionProfit ?? throw new ArgumentNullException(nameof(lblAvgSessionProfit));
        _lblRiskOfRuin = lblRiskOfRuin ?? throw new ArgumentNullException(nameof(lblRiskOfRuin));
        _lblRecommendation = lblRecommendation ?? throw new ArgumentNullException(nameof(lblRecommendation));

        ConfigureColumns();

        // Suscribir eventos de la tab page
        tpBankroll.Enter += (s, e) =>
        {
            if (!_bankrollLoaded)
            {
                UpdateBankrollDashboard();
                _bankrollLoaded = true;
            }
        };

        _dgvBankrollHistory.CellFormatting += DgvBankrollHistory_CellFormatting;
    }

    private void ConfigureColumns()
    {
        _dgvBankrollHistory!.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Fecha", DataPropertyName = "Date", Width = 100 },
            new DataGridViewTextBoxColumn { Name = "Hands", HeaderText = "Manos", DataPropertyName = "Hands", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "Profit", HeaderText = "Profit", DataPropertyName = "Profit", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "BBPer100", HeaderText = "BB/100", DataPropertyName = "BBPer100", Width = 70 },
            new DataGridViewTextBoxColumn { Name = "BankrollAfter", HeaderText = "Bankroll", DataPropertyName = "BankrollAfter", Width = 90 }
        );
    }

    /// <summary>
    /// Actualiza el dashboard de bankroll con las métricas actuales.
    /// Puede llamarse desde FrmMain cuando sea necesario (ej. FrmMain_Load).
    /// </summary>
    public void UpdateBankrollDashboard()
    {
        try
        {
            var stats = _bankrollTracker.GetBankrollStats();

            // Panel Bankroll
            _lblBankrollCurrent!.Text = $"Bankroll: €{stats.CurrentBankroll:N2}";
            _lblBankrollPeak!.Text = $"Peak: €{stats.PeakBankroll:N2}";
            _lblBankrollMaxDD!.Text = $"Max Drawdown: -{stats.MaxDrawdownPercent:F1}%";

            // Color MaxDD según spec
            _lblBankrollMaxDD.ForeColor = stats.MaxDrawdownPercent switch
            {
                < 10 => AppThemeHelper.Success,
                < 20 => AppThemeHelper.Warning,
                _ => AppThemeHelper.Danger
            };

            // Panel Rendimiento
            _lblWinRate!.Text = $"Win Rate: {stats.WinRateBB100:F1} BB/100";
            _lblWinRate.ForeColor = stats.WinRateBB100 switch
            {
                > 5 => AppThemeHelper.Success,
                >= 0 => AppThemeHelper.Warning,
                _ => AppThemeHelper.Danger
            };

            _lblStdDev!.Text = $"Std Dev: {stats.StdDeviation:F1} BB/100";

            var winPct = stats.TotalSessions > 0
                ? (double)stats.WinningSessions / stats.TotalSessions * 100
                : 0;
            _lblTotalSessions!.Text = $"Sesiones: {stats.TotalSessions} ({stats.WinningSessions} ganadas, {winPct:F0}%)";
            _lblTotalHands!.Text = $"Manos: {stats.TotalHands}";
            _lblAvgSessionProfit!.Text = $"Media: €{stats.AverageSessionProfit:N2} / sesión";

            // Panel Riesgo
            var rorPercent = stats.RiskOfRuin * 100;
            _lblRiskOfRuin!.Text = $"Risk of Ruin: {rorPercent:F1}%";
            _lblRiskOfRuin.ForeColor = stats.RiskLevel switch
            {
                "Green" => AppThemeHelper.Success,
                "Yellow" => AppThemeHelper.Warning,
                _ => AppThemeHelper.Danger
            };

            _lblRecommendation!.Text = stats.LimitRecommendation;
            _lblRecommendation.ForeColor = stats.LimitRecommendation switch
            {
                var r when r.StartsWith("SUBIR") => AppThemeHelper.Success,
                var r when r.StartsWith("BAJAR") => AppThemeHelper.Danger,
                _ => AppThemeHelper.Warning
            };

            // Historial en DataGridView
            var history = _bankrollTracker.GetHistory(50);
            var displayData = history.Select(h => new
            {
                Date = h.Date.ToString("dd/MM/yyyy"),
                h.Hands,
                Profit = h.Profit.ToString("+0.00;-0.00"),
                BBPer100 = h.BBPer100.ToString("+0.0;-0.0"),
                BankrollAfter = h.BankrollAfter.ToString("N2")
            }).ToList();

            _dgvBankrollHistory!.DataSource = displayData;
        }
        catch
        {
            _lblBankrollCurrent!.Text = "Bankroll: €0.00";
            _lblRecommendation!.Text = "Error al cargar stats";
        }
    }

    private void DgvBankrollHistory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        var colName = _dgvBankrollHistory!.Columns[e.ColumnIndex].Name;
        if (colName == "Profit" && e.Value is string val)
        {
            if (val.StartsWith('+'))
                e.CellStyle.ForeColor = AppThemeHelper.Success;
            else if (val.StartsWith('-'))
                e.CellStyle.ForeColor = AppThemeHelper.Danger;
        }
    }
}
