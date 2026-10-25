using Microsoft.Extensions.Logging;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Forms.Controls;

/// <summary>
/// UserControl que encapsula la pestaña Historial: sesiones recientes y manos.
/// El backtest A/B quedó fuera de servicio al retirar el motor local del cliente.
/// </summary>
public sealed class HistorialTabControl : UserControl
{
    private readonly GameLoggerService _gameLogger;
    private readonly ILogger<HistorialTabControl> _logger;

    // Controls recibidos via Initialize
    private DataGridView? _dgvSessions;
    private DataGridView? _dgvSessionHands;
    private Button? _btnBacktest;
    private Label? _lblSessionStats;

    // Estado
    private bool _historialLoaded;
    private List<SessionStatsDto>? _loadedSessions;
    private List<HandRecord>? _loadedHands;

    public HistorialTabControl(
        GameLoggerService gameLogger,
        ILogger<HistorialTabControl> logger)
    {
        _gameLogger = gameLogger ?? throw new ArgumentNullException(nameof(gameLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize(
        DataGridView dgvSessions,
        DataGridView dgvSessionHands,
        Button btnBacktest,
        Label lblSessionStats)
    {
        _dgvSessions = dgvSessions ?? throw new ArgumentNullException(nameof(dgvSessions));
        _dgvSessionHands = dgvSessionHands ?? throw new ArgumentNullException(nameof(dgvSessionHands));
        _btnBacktest = btnBacktest ?? throw new ArgumentNullException(nameof(btnBacktest));
        _lblSessionStats = lblSessionStats ?? throw new ArgumentNullException(nameof(lblSessionStats));

        ConfigureColumns();
        SubscribeEvents();
    }

    private void ConfigureColumns()
    {
        // Configurar columnas de sesiones
        _dgvSessions!.AutoGenerateColumns = false;
        _dgvSessions.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "Mesa", DataPropertyName = "TableName", Width = 120 },
            new DataGridViewTextBoxColumn { Name = "StartTime", HeaderText = "Inicio", DataPropertyName = "StartTime", Width = 130 },
            new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Duración", DataPropertyName = "Duration", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "TotalHands", HeaderText = "Manos", DataPropertyName = "TotalHands", Width = 60 }
        );

        // Configurar columnas de manos
        _dgvSessionHands!.AutoGenerateColumns = false;
        _dgvSessionHands.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "HandNumber", HeaderText = "Hand#", DataPropertyName = "HandNumber", Width = 70 },
            new DataGridViewTextBoxColumn { Name = "Cards", HeaderText = "Cartas", DataPropertyName = "Cards", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "Position", HeaderText = "Pos", DataPropertyName = "Position", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "LastStreet", HeaderText = "Street", DataPropertyName = "LastStreet", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "Resultado", DataPropertyName = "Result", Width = 70 },
            new DataGridViewTextBoxColumn { Name = "ProfitLoss", HeaderText = "P/L", DataPropertyName = "ProfitLoss", Width = 70 }
        );
    }

    private void SubscribeEvents()
    {
        // tpHistorial.Enter equivalente — el UserControl se suscribe al Enter del TabPage
        // desde FrmMain o podemos usar Parent.Enter
        if (Parent is TabPage tabPage)
        {
            // Evento de inicialización
        }

        _dgvSessions!.SelectionChanged += DgvSessions_SelectionChanged;
        _dgvSessionHands!.CellDoubleClick += DgvSessionHands_CellDoubleClick;
        _dgvSessions.CellFormatting += DgvSessions_CellFormatting;
        _dgvSessionHands.CellFormatting += DgvSessionHands_CellFormatting;

        // El backtest A/B replayaba manos contra el motor local, ya retirado del cliente.
        _btnBacktest!.Enabled = false;
        _btnBacktest.Text = "Backtest A/B (no disponible)";
    }

    /// <summary>
    /// Debe llamarse desde el evento Enter del TabPage que contiene este control.
    /// </summary>
    public async Task OnTabEnterAsync()
    {
        if (!_historialLoaded)
            await LoadSessionsWithStatsAsync();
    }

    /// <summary>
    /// Carga las sesiones recientes con sus estadísticas
    /// </summary>
    private async Task LoadSessionsWithStatsAsync()
    {
        try
        {
            _loadedSessions = await _gameLogger.GetRecentSessionsWithStatsAsync(50);

            var displayData = _loadedSessions.Select(s => new
            {
                s.TableName,
                StartTime = s.StartTime.ToString("dd/MM HH:mm"),
                Duration = FormatDuration(s.EndTime - s.StartTime),
                s.TotalHands,
                TotalProfit = s.TotalProfit.ToString("+0.00;-0.00"),
                BBPer100 = s.BBPer100.ToString("+0.0;-0.0")
            }).ToList();

            _dgvSessions!.DataSource = displayData;
            _historialLoaded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando sesiones");
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1)
            return "< 1m";
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
            : $"{duration.Minutes}m";
    }

    private async void DgvSessions_SelectionChanged(object? sender, EventArgs e)
    {
        if (_dgvSessions!.SelectedRows.Count == 0 || _loadedSessions == null)
            return;

        int idx = _dgvSessions.SelectedRows[0].Index;
        if (idx < 0 || idx >= _loadedSessions.Count)
            return;

        var session = _loadedSessions[idx];

        try
        {
            _loadedHands = await _gameLogger.GetHandsForSessionAsync(session.Id);

            var displayData = _loadedHands.Select(h => new
            {
                h.HandNumber,
                Cards = $"{h.HeroCard1} {h.HeroCard2}",
                Position = FormatPosition(h.HeroPosition),
                LastStreet = h.LastStreetPlayed.ToString(),
                Result = h.Result.ToString(),
                ProfitLoss = (h.HeroStackEnd - h.HeroStackStart).ToString("+0.00;-0.00")
            }).ToList();

            _dgvSessionHands!.DataSource = displayData;

            _lblSessionStats!.Text = $"Sesión: {session.TableName} | {session.TotalHands} manos | " +
                $"{session.TotalProfit:+0.00;-0.00} | {session.BBPer100:+0.0;-0.0} BB/100";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando manos");
        }
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

    private void DgvSessionHands_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _loadedHands == null || e.RowIndex >= _loadedHands.Count)
            return;

        var hand = _loadedHands[e.RowIndex];
        decimal bigBlind = _loadedSessions != null && _dgvSessions!.SelectedRows.Count > 0
            ? _loadedSessions[_dgvSessions.SelectedRows[0].Index].BigBlind
            : 0.50m;

        using var frm = new FrmHandDetail(hand, bigBlind);
        frm.ShowDialog(this);
    }

    /// <summary>
    /// Aplica colores condicionales verde/rojo a las columnas Profit y BB/100
    /// </summary>
    private void DgvSessions_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.Value == null) return;

        string colName = _dgvSessions!.Columns[e.ColumnIndex].Name;
        if (colName is "TotalProfit" or "BBPer100")
        {
            string val = e.Value.ToString() ?? "";
            if (val.StartsWith('+'))
                e.CellStyle.ForeColor = AppThemeHelper.Success;
            else if (val.StartsWith('-'))
                e.CellStyle.ForeColor = AppThemeHelper.Danger;
        }
    }

    /// <summary>
    /// Aplica colores condicionales a Result y P/L en la grilla de manos
    /// </summary>
    private void DgvSessionHands_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.Value == null) return;

        string colName = _dgvSessionHands!.Columns[e.ColumnIndex].Name;
        string val = e.Value.ToString() ?? "";

        if (colName == "Result")
        {
            e.CellStyle.ForeColor = val switch
            {
                "Won" => AppThemeHelper.Success,
                "Lost" => AppThemeHelper.Danger,
                _ => AppThemeHelper.PrimaryLight
            };
        }
        else if (colName == "ProfitLoss")
        {
            if (val.StartsWith('+'))
                e.CellStyle.ForeColor = AppThemeHelper.Success;
            else if (val.StartsWith('-'))
                e.CellStyle.ForeColor = AppThemeHelper.Danger;
        }
    }
}
