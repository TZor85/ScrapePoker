using System.Runtime.InteropServices;
using OpenScrape.App.Aplication.UseCases;
using System.Drawing.Drawing2D;

namespace OpenScrape.App.Forms
{
    public partial class FrmOverlay : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;

        private const int HEADER_HEIGHT = 12;

        private Label lbPotOdds;
        private Label lbShouldCall;
        private Label lbEquity;
        private Label lbSituacion;
        private Label lbAction;
        private Label lbFoldEquity;
        private Label lbEVWithFoldEquity;
        private Label lbSuggestedBetSize;
        private Label lbTableName;

        private System.Windows.Forms.Timer animationTimer;
        private float currentOpacity = 0.0f;
        private const float MAX_OPACITY = 0.8f;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public FrmOverlay()
        {
            InitializeComponent();
            this.MouseDown += FrmOverlay_MouseDown;

            InitializeTableLayoutPanel();

            ApplyModernDesign();

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50;
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void InitializeTableLayoutPanel()
        {
            var tableLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(8),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Location = new Point(5, 5)
            };

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            for (int i = 0; i < 6; i++)
            {
                tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            lbPotOdds = CreateLabel("PotOdds: --", Color.FromArgb(0, 255, 255), 10);
            lbShouldCall = CreateLabel("---", Color.FromArgb(0, 255, 255), 10);
            lbEquity = CreateLabel("Equity: --", Color.FromArgb(50, 255, 50), 10);
            lbSituacion = CreateLabel("---", Color.FromArgb(255, 150, 255), 12, true);
            lbAction = CreateLabel("---", Color.FromArgb(255, 100, 100), 14, true);
            lbFoldEquity = CreateLabel("", Color.FromArgb(255, 200, 100), 10);
            lbEVWithFoldEquity = CreateLabel("", Color.FromArgb(100, 255, 100), 10);
            lbSuggestedBetSize = CreateLabel("", Color.FromArgb(255, 255, 180), 10);
            lbTableName = CreateLabel("", Color.White, 9);

            tableLayout.Controls.Add(lbPotOdds, 0, 0);
            tableLayout.Controls.Add(lbShouldCall, 1, 0);
            tableLayout.Controls.Add(lbEquity, 0, 1);
            tableLayout.Controls.Add(lbSituacion, 1, 1);
            tableLayout.SetColumnSpan(lbSituacion, 2);
            tableLayout.Controls.Add(lbAction, 0, 2);
            tableLayout.SetColumnSpan(lbAction, 2);
            tableLayout.Controls.Add(lbFoldEquity, 0, 3);
            tableLayout.SetColumnSpan(lbFoldEquity, 2);
            tableLayout.Controls.Add(lbEVWithFoldEquity, 0, 4);
            tableLayout.SetColumnSpan(lbEVWithFoldEquity, 2);
            tableLayout.Controls.Add(lbSuggestedBetSize, 0, 5);
            tableLayout.SetColumnSpan(lbSuggestedBetSize, 2);

            this.Controls.Add(tableLayout);
        }

        private Label CreateLabel(string text, Color foreColor, float fontSize, bool isBold = false)
        {
            var label = new Label
            {
                Text = text,
                ForeColor = foreColor,
                AutoSize = true,
                Font = isBold 
                    ? new Font("Segoe UI", fontSize, FontStyle.Bold) 
                    : new Font("Segoe UI", fontSize, FontStyle.Regular),
                Padding = new Padding(2),
                Margin = new Padding(2)
            };
            return label;
        }

        private void ApplyModernDesign()
        {
            // Set rounded corners
            int radius = 15;
            this.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, this.Width, this.Height), radius));

            // Change transparency key to a color not in gradient
            this.TransparencyKey = Color.Magenta;
            this.BackColor = Color.Magenta; // Will be overridden by gradient
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw gradient background
            using (LinearGradientBrush brush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(26, 26, 46), // Dark blue
                Color.FromArgb(15, 15, 15), // Dark
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }

        public void UpdateAction(string action)
        {
            lbAction.Text = action;
        }

        public void UpdatePotOddsPercentage(string potOdds)
        {
            if (string.IsNullOrEmpty(potOdds))
            {
                lbPotOdds.Text = string.Empty;
                return;
            }

            lbPotOdds.Text = $"PotOdds: {Math.Round(decimal.Parse(potOdds), 2)}%";
        }

        public void UpdateEquityPercentage(string equity)
        {
            if (string.IsNullOrEmpty(equity))
            {
                lbEquity.Text = string.Empty;
                return;
            }

            lbEquity.Text = $"Equity: {equity}%";
        }

        public void UpdateShouldCall(bool? shouldCall)
        {
            if (shouldCall == null)
            {
                lbShouldCall.Text = string.Empty;
                return;
            }

            lbShouldCall.Text = shouldCall.HasValue == true ? "Pagar" : "No Pagar";
        }

        public void UpdateSituacion(string situacion)
        {
            lbSituacion.Text = situacion;
        }

        public void UpdateFoldEquity(double foldEquity)
        {
            if (foldEquity <= 0)
            {
                lbFoldEquity.Text = string.Empty;
                lbFoldEquity.Visible = false;
                return;
            }

            lbFoldEquity.Text = $"Fold Eq: {foldEquity:F1}%";
            lbFoldEquity.Visible = true;
        }

        public void UpdateEVWithFoldEquity(double ev)
        {
            if (ev == 0)
            {
                lbEVWithFoldEquity.Text = string.Empty;
                lbEVWithFoldEquity.Visible = false;
                return;
            }

            lbEVWithFoldEquity.Text = $"EV: {ev:F1}";
            lbEVWithFoldEquity.Visible = true;

            if (ev > 0)
            {
                lbEVWithFoldEquity.ForeColor = Color.FromArgb(100, 255, 100);
            }
            else if (ev < 0)
            {
                lbEVWithFoldEquity.ForeColor = Color.FromArgb(255, 120, 120);
            }
            else
            {
                lbEVWithFoldEquity.ForeColor = Color.FromArgb(255, 255, 150);
            }
        }

        public void UpdateSuggestedBetSize(double? betSize)
        {
            if (!betSize.HasValue)
            {
                lbSuggestedBetSize.Text = string.Empty;
                lbSuggestedBetSize.Visible = false;
                return;
            }

            lbSuggestedBetSize.Text = $"Bet: {Math.Round(betSize.Value, 2)}";
            lbSuggestedBetSize.Visible = true;
        }

        public void UpdateTableName(string tableName)
        {
            lbTableName.Text = tableName ?? string.Empty;
        }

        // Nuevo método para actualizar con el resultado unificado
        public void UpdateWithCalculationResult(PokerCalculationResult result)
        {
            UpdatePotOddsPercentage(result.PotOddsPercentage.ToString("F1"));
            UpdateEquityPercentage(result.EquityPercentage.ToString("F1"));
            UpdateShouldCall(result.ShouldCall);
            UpdateAction(result.RecommendedAction);

            // Actualizar situación con información adicional
            string situacionText = result.Street;
            if (result.TotalOuts > 0)
            {
                situacionText += $" | Outs: {result.TotalOuts}";
            }
            if (result.DrawTypes.Any())
            {
                situacionText += $" | {string.Join(", ", result.DrawTypes)}";
            }
            UpdateSituacion(situacionText);

            // Nuevas métricas avanzadas
            UpdateFoldEquity(result.FoldEquity);
            UpdateEVWithFoldEquity(result.EVWithFoldEquity);
            UpdateSuggestedBetSize(result.SuggestedBetSize);

            // Start fade-in animation for new metrics
            currentOpacity = 0.0f;
            animationTimer.Start();
        }

        // Método para limpiar todos los datos
        public void ClearAll()
        {
            lbPotOdds.Text = string.Empty;
            lbEquity.Text = string.Empty;
            lbShouldCall.Text = string.Empty;
            lbAction.Text = string.Empty;
            lbSituacion.Text = "Esperando datos...";
            lbFoldEquity.Text = string.Empty;
            lbEVWithFoldEquity.Text = string.Empty;
            lbSuggestedBetSize.Text = string.Empty;
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            currentOpacity += 0.1f;
            if (currentOpacity >= MAX_OPACITY)
            {
                currentOpacity = MAX_OPACITY;
                animationTimer.Stop();
            }

            if (lbFoldEquity != null)
                lbFoldEquity.ForeColor = Color.FromArgb((int)(currentOpacity * 255), lbFoldEquity.ForeColor);
            if (lbEVWithFoldEquity != null)
                lbEVWithFoldEquity.ForeColor = Color.FromArgb((int)(currentOpacity * 255), lbEVWithFoldEquity.ForeColor);
            if (lbSuggestedBetSize != null)
                lbSuggestedBetSize.ForeColor = Color.FromArgb((int)(currentOpacity * 255), lbSuggestedBetSize.ForeColor);

            this.Invalidate();
        }

        private void FrmOverlay_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
    }
}