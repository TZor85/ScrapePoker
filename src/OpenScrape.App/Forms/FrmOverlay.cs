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

        // Alto del header arrastrable en píxeles lógicos (96 DPI)
        private const int HEADER_HEIGHT = 12;

        // Additional labels for enhanced metrics
        private Label lbFoldEquity;
        private Label lbEVWithFoldEquity;
        private Label lbSuggestedBetSize;
        private Label lbTableName;

        // Animation timer for smooth updates
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

            // Add new labels for enhanced metrics
            InitializeAdditionalLabels();

            // Apply modern design: rounded corners and gradient
            ApplyModernDesign();

            // Initialize animation timer
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50; // 50ms ticks
            animationTimer.Tick += AnimationTimer_Tick;
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

            lbFoldEquity.Text = $"♠ Fold Eq: {foldEquity:F1}%";
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

            lbEVWithFoldEquity.Text = $"💰 EV: {ev:F1}";
            lbEVWithFoldEquity.Visible = true;

            // Dynamic color based on EV value
            if (ev > 0)
            {
                lbEVWithFoldEquity.ForeColor = Color.LimeGreen; // Bright green for positive
            }
            else if (ev < 0)
            {
                lbEVWithFoldEquity.ForeColor = Color.Red; // Red for negative
            }
            else
            {
                lbEVWithFoldEquity.ForeColor = Color.Yellow; // Yellow for zero
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

        private void InitializeAdditionalLabels()
        {
            // Create panels for new metrics
            var panel6 = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Black,
                Location = new Point(0, 75), // Below existing panels
                Name = "panel6"
            };

            lbFoldEquity = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Orange,
                Location = new Point(0, 0),
                Name = "lbFoldEquity",
                Size = new Size(50, 19),
                TabIndex = 11,
                Text = string.Empty,
                Visible = false
            };
            panel6.Controls.Add(lbFoldEquity);

            var panel7 = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Black,
                Location = new Point(0, 95),
                Name = "panel7"
            };

            lbEVWithFoldEquity = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Green,
                Location = new Point(0, 0),
                Name = "lbEVWithFoldEquity",
                Size = new Size(50, 19),
                TabIndex = 12,
                Text = string.Empty,
                Visible = false
            };
            panel7.Controls.Add(lbEVWithFoldEquity);

            var panel8 = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Black,
                Location = new Point(0, 115),
                Name = "panel8"
            };

            lbSuggestedBetSize = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Location = new Point(0, 0),
                Name = "lbSuggestedBetSize",
                Size = new Size(50, 19),
                TabIndex = 13,
                Text = string.Empty,
                Visible = false
            };
            panel8.Controls.Add(lbSuggestedBetSize);

            // Add panels to form
            this.Controls.Add(panel6);
            this.Controls.Add(panel7);
            this.Controls.Add(panel8);

            // Add table name panel
            var panel9 = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Black,
                Location = new Point(0, 135), // Below panel8
                Name = "panel9"
            };

            lbTableName = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, 0),
                Name = "lbTableName",
                Size = new Size(50, 19),
                TabIndex = 14,
                Text = string.Empty
            };
            panel9.Controls.Add(lbTableName);

            this.Controls.Add(panel9);

            // Increase form height to accommodate new panels
            this.ClientSize = new Size(180, 150);
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            currentOpacity += 0.1f;
            if (currentOpacity >= MAX_OPACITY)
            {
                currentOpacity = MAX_OPACITY;
                animationTimer.Stop();
            }

            // Apply opacity to new labels (simulate fade-in)
            lbFoldEquity.ForeColor = Color.FromArgb((int)(currentOpacity * 255), lbFoldEquity.ForeColor);
            lbEVWithFoldEquity.ForeColor = Color.FromArgb((int)(currentOpacity * 255), lbEVWithFoldEquity.ForeColor);
            lbSuggestedBetSize.ForeColor = Color.FromArgb((int)(currentOpacity * 255), lbSuggestedBetSize.ForeColor);

            this.Invalidate(); // Redraw
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