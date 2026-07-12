using Microsoft.Extensions.Logging;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Forms.Controls;

/// <summary>
/// UserControl que encapsula la pestaña Métricas: telemetría de rendimiento del pipeline.
/// </summary>
public sealed class MetricsTabControl : UserControl
{
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<MetricsTabControl> _logger;

    private DataGridView? _dgvMetrics;
    private Button? _btnResetMetrics;
    private Label? _lblCurrentHand;
    private Label? _lblCycleCount;
    private Label? _lblLastUpdate;
    private Func<int>? _cycleCounterProvider;
    private System.Windows.Forms.Timer? _metricsRefreshTimer;

    public MetricsTabControl(
        IMetricsCollector metrics,
        ILogger<MetricsTabControl> logger)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize(
        DataGridView dgvMetrics,
        Button btnResetMetrics,
        Label lblCurrentHand,
        Label lblCycleCount,
        Label lblLastUpdate,
        TabControl tbControl,
        TabPage tabMetrics,
        Func<int>? cycleCounterProvider = null)
    {
        _dgvMetrics = dgvMetrics ?? throw new ArgumentNullException(nameof(dgvMetrics));
        _btnResetMetrics = btnResetMetrics ?? throw new ArgumentNullException(nameof(btnResetMetrics));
        _lblCurrentHand = lblCurrentHand ?? throw new ArgumentNullException(nameof(lblCurrentHand));
        _lblCycleCount = lblCycleCount ?? throw new ArgumentNullException(nameof(lblCycleCount));
        _lblLastUpdate = lblLastUpdate ?? throw new ArgumentNullException(nameof(lblLastUpdate));
        _cycleCounterProvider = cycleCounterProvider;

        ConfigureColumns();
        ConfigureTimer();
        SubscribeEvents(tbControl, tabMetrics);
    }

    private void ConfigureColumns()
    {
        _dgvMetrics!.Columns.Clear();
        _dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Fase",
            HeaderText = "Fase",
            FillWeight = 180f,
            ReadOnly = true,
        });
        foreach (var prefix in new[] { "Last", "Session" })
            foreach (var suffix in new[] { "P50", "P95", "Max", "Count" })
            {
                _dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = $"{prefix}{suffix}",
                    HeaderText = prefix == "Last" ? $"Últ. {suffix}" : $"Ses. {suffix}",
                    FillWeight = 60f,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
                });
            }

        foreach (var cat in TelemetryCategories.DisplayOrder)
        {
            int rowIdx = _dgvMetrics.Rows.Add(cat, "—", "—", "—", "0", "—", "—", "—", "0");
            _dgvMetrics.Rows[rowIdx].Tag = cat;
        }
    }

    private void ConfigureTimer()
    {
        _metricsRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _metricsRefreshTimer.Tick += (s, e) => RefreshMetricsGrid();
    }

    private void SubscribeEvents(TabControl tbControl, TabPage tabMetrics)
    {
        tbControl.Selected += (s, e) =>
        {
            if (e.TabPage == tabMetrics)
            {
                RefreshMetricsGrid();
                _metricsRefreshTimer?.Start();
            }
            else
            {
                _metricsRefreshTimer?.Stop();
            }
        };

        _btnResetMetrics!.Click += (s, e) =>
        {
            _metrics.ResetSession();
            RefreshMetricsGrid();
            _logger.LogInformation("Telemetría: sesión reseteada por el usuario");
        };
    }

    private void RefreshMetricsGrid()
    {
        if (_metrics == null) return;

        var snap = _metrics.SnapshotSession();

        _lblCurrentHand!.Text = $"Mano actual: {snap.CurrentHandId ?? "—"}";
        _lblCycleCount!.Text = _cycleCounterProvider != null
            ? $"Ciclos: {_cycleCounterProvider():N0}"
            : "Ciclos: —";
        _lblLastUpdate!.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";

        foreach (DataGridViewRow row in _dgvMetrics!.Rows)
        {
            if (row.Tag is not string category) continue;

            FillCells(row, 1, snap.LastHand.TryGetValue(category, out var last) ? last : null);
            FillCells(row, 5, snap.Session.TryGetValue(category, out var session) ? session : null);
        }
    }

    private static void FillCells(DataGridViewRow row, int startCol, CategoryStats? stats)
    {
        if (stats is null)
        {
            row.Cells[startCol + 0].Value = "—";
            row.Cells[startCol + 1].Value = "—";
            row.Cells[startCol + 2].Value = "—";
            row.Cells[startCol + 3].Value = "0";
            return;
        }
        row.Cells[startCol + 0].Value = stats.P50Ms.ToString("N0");
        row.Cells[startCol + 1].Value = stats.P95Ms.ToString("N0");
        row.Cells[startCol + 2].Value = stats.MaxMs.ToString("N0");
        row.Cells[startCol + 3].Value = stats.Count.ToString("N0");
    }
}
