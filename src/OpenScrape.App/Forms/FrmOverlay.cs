using System.Runtime.InteropServices;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using System.Drawing.Drawing2D;

namespace OpenScrape.App.Forms
{
    public partial class FrmOverlay : Form
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;

        private Label lbPotOdds;
        private Label lbShouldCall;
        private Label lbEquity;
        private Label lbSituacion;
        private Label lbAction;
        private Label lbHandStrength;
        private Label lbBoardTexture;
        private Label lbFoldEquity;
        private Label lbEVWithFoldEquity;
        private Label lbSuggestedBetSize;
        private Label lbTableName;
        private Panel actionPanel;
        private TableLayoutPanel tableLayout;

        private readonly OverlayConfig _config;
        private Color _streetBorderColor = Color.FromArgb(60, 60, 80);

        private System.Windows.Forms.Timer animationTimer;
        private float currentOpacity = 0.0f;
        private const float MAX_OPACITY = 0.95f;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public FrmOverlay() : this(new OverlayConfig()) { }

        public FrmOverlay(OverlayConfig config)
        {
            _config = config;

            InitializeComponent();
            this.MouseDown += FrmOverlay_MouseDown;
            this.MaximumSize = new Size(500, 400);
            this.Opacity = _config.Opacity;
            this.DoubleBuffered = true;

            InitializeTableLayoutPanel();
            ApplyModernDesign();

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50;
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void InitializeTableLayoutPanel()
        {
            tableLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 9,
                Padding = new Padding(6, 4, 6, 4),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Location = new Point(3, 5),
                MaximumSize = new Size(490, 390),
                BackColor = Color.Transparent
            };

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            for (int i = 0; i < 9; i++)
            {
                tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Colores con alto contraste sobre fondo oscuro
            lbPotOdds = CreateLabel("PotOdds: --", Color.FromArgb(100, 220, 255), _config.FontSize);
            lbShouldCall = CreateLabel("---", Color.FromArgb(100, 220, 255), _config.FontSize);
            lbEquity = CreateLabel("Equity: --", Color.FromArgb(80, 255, 80), _config.FontSize, true);
            lbSituacion = CreateLabel("---", Color.FromArgb(255, 180, 255), _config.FontSize, true);

            // Panel destacado para la acción principal
            actionPanel = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(40, 40, 60),
                Padding = new Padding(6, 2, 6, 2),
                Margin = new Padding(0, 2, 0, 2)
            };
            lbAction = new Label
            {
                Text = "---",
                ForeColor = Color.White,
                AutoSize = true,
                Font = new Font("Segoe UI", _config.ActionFontSize, FontStyle.Bold),
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            actionPanel.Controls.Add(lbAction);

            lbHandStrength = CreateLabel("", Color.FromArgb(220, 200, 255), _config.FontSize);
            lbBoardTexture = CreateLabel("", Color.FromArgb(200, 220, 240), _config.FontSize);
            lbFoldEquity = CreateLabel("", Color.FromArgb(255, 220, 120), _config.FontSize);
            lbEVWithFoldEquity = CreateLabel("", Color.FromArgb(120, 255, 120), _config.FontSize);
            lbSuggestedBetSize = CreateLabel("", Color.FromArgb(255, 255, 200), _config.FontSize);
            lbTableName = CreateLabel("", Color.FromArgb(160, 160, 180), _config.FontSize - 1);

            // Fila 0: PotOdds + ShouldCall
            tableLayout.Controls.Add(lbPotOdds, 0, 0);
            tableLayout.Controls.Add(lbShouldCall, 1, 0);
            // Fila 1: Equity + Situación
            tableLayout.Controls.Add(lbEquity, 0, 1);
            tableLayout.Controls.Add(lbSituacion, 0, 1);
            tableLayout.SetColumnSpan(lbSituacion, 2);
            // Fila 2: Acción recomendada (panel destacado)
            tableLayout.Controls.Add(actionPanel, 0, 2);
            tableLayout.SetColumnSpan(actionPanel, 2);
            // Fila 3: Fuerza de mano
            tableLayout.Controls.Add(lbHandStrength, 0, 3);
            tableLayout.SetColumnSpan(lbHandStrength, 2);
            // Fila 4: Board texture
            tableLayout.Controls.Add(lbBoardTexture, 0, 4);
            tableLayout.SetColumnSpan(lbBoardTexture, 2);
            // Fila 5: Fold Equity
            tableLayout.Controls.Add(lbFoldEquity, 0, 5);
            tableLayout.SetColumnSpan(lbFoldEquity, 2);
            // Fila 6: EV
            tableLayout.Controls.Add(lbEVWithFoldEquity, 0, 6);
            tableLayout.SetColumnSpan(lbEVWithFoldEquity, 2);
            // Fila 7: Bet Size sugerido
            tableLayout.Controls.Add(lbSuggestedBetSize, 0, 7);
            tableLayout.SetColumnSpan(lbSuggestedBetSize, 2);
            // Fila 8: Nombre de mesa
            tableLayout.Controls.Add(lbTableName, 0, 8);
            tableLayout.SetColumnSpan(lbTableName, 2);

            this.Controls.Add(tableLayout);
        }

        private Label CreateLabel(string text, Color foreColor, float fontSize, bool isBold = false)
        {
            var label = new Label
            {
                Text = text,
                ForeColor = foreColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Font = isBold
                    ? new Font("Segoe UI", fontSize, FontStyle.Bold)
                    : new Font("Segoe UI", fontSize, FontStyle.Regular),
                Padding = new Padding(1, 0, 1, 0),
                Margin = new Padding(1, 1, 1, 1)
            };
            return label;
        }

        private void ApplyModernDesign()
        {
            this.TransparencyKey = Color.Magenta;
            this.BackColor = Color.Magenta;
            UpdateRoundedCorners();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRoundedCorners();
        }

        private void UpdateRoundedCorners()
        {
            if (this.Width > 0 && this.Height > 0)
            {
                int radius = 12;
                this.Region = new Region(CreateRoundedRectanglePath(
                    new Rectangle(0, 0, this.Width, this.Height), radius));
            }
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
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Fondo sólido oscuro con ligero gradiente (más opaco = más legible)
            using (var brush = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(20, 22, 40),
                Color.FromArgb(12, 12, 18),
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            // Borde completo con color de street (más grueso y visible)
            using (var pen = new Pen(_streetBorderColor, 2))
            {
                var borderRect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
                int radius = 12;
                using (var borderPath = CreateRoundedRectanglePath(borderRect, radius))
                {
                    e.Graphics.DrawPath(pen, borderPath);
                }
            }

            // Línea separadora debajo de la acción (entre fila 2 y 3)
            if (actionPanel.Visible && actionPanel.Bottom > 0)
            {
                int separatorY = actionPanel.Parent != null
                    ? tableLayout.Location.Y + actionPanel.Bottom + 2
                    : 0;
                if (separatorY > 0 && separatorY < this.Height)
                {
                    using var pen = new Pen(Color.FromArgb(60, 70, 90), 1);
                    e.Graphics.DrawLine(pen, 10, separatorY, this.Width - 10, separatorY);
                }
            }
        }

        public void UpdateAction(string action)
        {
            lbAction.Text = action;

            // Color de acción según tipo
            actionPanel.BackColor = action.ToUpperInvariant() switch
            {
                "FOLD" => Color.FromArgb(80, 30, 30),
                "CALL" => Color.FromArgb(30, 60, 30),
                "CHECK" => Color.FromArgb(30, 50, 60),
                _ when action.ToUpperInvariant().Contains("RAISE") => Color.FromArgb(70, 50, 20),
                _ when action.ToUpperInvariant().Contains("BET") => Color.FromArgb(50, 50, 20),
                _ when action.ToUpperInvariant().Contains("ALL") => Color.FromArgb(80, 20, 60),
                _ => Color.FromArgb(40, 40, 60)
            };

            lbAction.ForeColor = action.ToUpperInvariant() switch
            {
                "FOLD" => Color.FromArgb(255, 130, 130),
                "CALL" => Color.FromArgb(130, 255, 130),
                "CHECK" => Color.FromArgb(130, 220, 255),
                _ when action.ToUpperInvariant().Contains("RAISE") => Color.FromArgb(255, 220, 100),
                _ when action.ToUpperInvariant().Contains("BET") => Color.FromArgb(255, 255, 130),
                _ when action.ToUpperInvariant().Contains("ALL") => Color.FromArgb(255, 130, 220),
                _ => Color.White
            };
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

        public void UpdateStreetPhase(string phase)
        {
            if (string.IsNullOrEmpty(phase))
            {
                lbShouldCall.Text = string.Empty;
                return;
            }

            lbShouldCall.Text = phase;
            lbShouldCall.ForeColor = phase.ToUpperInvariant() switch
            {
                "PRE-FLOP" or "PREFLOP" => Color.FromArgb(180, 180, 200),
                "FLOP" => Color.FromArgb(80, 160, 255),
                "TURN" => Color.FromArgb(255, 180, 50),
                "RIVER" => Color.FromArgb(255, 80, 80),
                _ => Color.FromArgb(180, 180, 200)
            };
            lbShouldCall.Font = new Font("Segoe UI", _config.FontSize, FontStyle.Bold);
        }

        public void UpdateSituacion(string situacion)
        {
            lbSituacion.Text = situacion;
        }

        public void UpdateHandStrength(HandRank handRank, KickerStrength kicker, bool hasComboDraw)
        {
            if (handRank == 0)
            {
                SetConditionalVisibility(lbHandStrength, false);
                return;
            }

            var text = FormatHandRank(handRank);

            if (handRank == HandRank.OnePair && kicker != KickerStrength.None)
                text += $" (K: {kicker})";

            if (hasComboDraw)
                text += " + Combo Draw";

            lbHandStrength.Text = text;

            lbHandStrength.ForeColor = handRank switch
            {
                >= HandRank.Straight => Color.FromArgb(120, 255, 120),
                >= HandRank.TwoPair => Color.FromArgb(255, 255, 120),
                HandRank.OnePair when kicker == KickerStrength.Strong => Color.FromArgb(255, 255, 120),
                _ => Color.FromArgb(220, 200, 255)
            };

            SetConditionalVisibility(lbHandStrength, true);
        }

        public void UpdateBoardTexture(BoardTextureCategory? category, double wetnessScore)
        {
            if (!category.HasValue)
            {
                SetConditionalVisibility(lbBoardTexture, false);
                return;
            }

            lbBoardTexture.Text = $"Board: {FormatBoardTexture(category.Value)} ({wetnessScore:F0})";

            lbBoardTexture.ForeColor = category.Value switch
            {
                BoardTextureCategory.Dry => Color.FromArgb(160, 210, 255),
                BoardTextureCategory.SemiDry => Color.FromArgb(200, 220, 240),
                BoardTextureCategory.SemiWet => Color.FromArgb(255, 230, 160),
                BoardTextureCategory.Wet => Color.FromArgb(255, 170, 120),
                BoardTextureCategory.Paired => Color.FromArgb(220, 200, 255),
                _ => Color.FromArgb(200, 220, 240)
            };

            SetConditionalVisibility(lbBoardTexture, true);
        }

        /// <summary>
        /// Actualiza el color del borde según la street actual.
        /// Preflop=gris, Flop=azul, Turn=naranja, River=rojo.
        /// </summary>
        public void UpdateStreetIndicator(string street)
        {
            _streetBorderColor = street.ToUpperInvariant() switch
            {
                "FLOP" => Color.FromArgb(80, 160, 255),
                "TURN" => Color.FromArgb(255, 180, 50),
                "RIVER" => Color.FromArgb(255, 80, 80),
                _ => Color.FromArgb(60, 60, 80)
            };
            this.Invalidate();
        }

        public void UpdateFoldEquity(double foldEquity)
        {
            if (foldEquity <= 0)
            {
                SetConditionalVisibility(lbFoldEquity, false);
                return;
            }

            lbFoldEquity.Text = $"Fold Eq: {foldEquity:F1}%";
            SetConditionalVisibility(lbFoldEquity, true);
        }

        public void UpdateEVWithFoldEquity(double ev)
        {
            if (ev == 0)
            {
                SetConditionalVisibility(lbEVWithFoldEquity, false);
                return;
            }

            lbEVWithFoldEquity.Text = $"EV: {ev:F1}";
            SetConditionalVisibility(lbEVWithFoldEquity, true);

            if (ev > 0)
                lbEVWithFoldEquity.ForeColor = Color.FromArgb(120, 255, 120);
            else if (ev < 0)
                lbEVWithFoldEquity.ForeColor = Color.FromArgb(255, 130, 130);
            else
                lbEVWithFoldEquity.ForeColor = Color.FromArgb(255, 255, 160);
        }

        public void UpdateSuggestedBetSize(double? betSize)
        {
            if (!betSize.HasValue)
            {
                SetConditionalVisibility(lbSuggestedBetSize, false);
                return;
            }

            lbSuggestedBetSize.Text = $"Bet: {Math.Round(betSize.Value, 2)}";
            SetConditionalVisibility(lbSuggestedBetSize, true);
        }

        public void UpdateTableName(string tableName)
        {
            lbTableName.Text = tableName ?? string.Empty;
        }

        public void UpdateWithCalculationResult(PokerCalculationResult result)
        {
            UpdatePotOddsPercentage(result.PotOddsPercentage.ToString("F1"));
            UpdateEquityPercentage(result.EquityPercentage.ToString("F1"));
            UpdateStreetPhase(result.Street);
            UpdateAction(result.RecommendedAction);

            // Indicador de street (borde)
            UpdateStreetIndicator(result.Street);

            // Situación con draws
            string situacionText = result.Street;
            if (result.TotalOuts > 0)
                situacionText += $" | Outs: {result.TotalOuts}";
            if (result.DrawTypes.Any())
                situacionText += $" | {string.Join(", ", result.DrawTypes)}";
            UpdateSituacion(situacionText);

            // Fuerza de mano
            UpdateHandStrength(result.HeroHandRank, result.HeroKickerStrength, result.HasComboDraw);

            // Board texture
            UpdateBoardTexture(result.BoardTexture, result.BoardWetnessScore);

            // Métricas avanzadas
            UpdateFoldEquity(result.FoldEquity);
            UpdateEVWithFoldEquity(result.EVWithFoldEquity);
            UpdateSuggestedBetSize(result.SuggestedBetSize);

            // Animación fade-in para métricas avanzadas
            currentOpacity = 0.0f;
            animationTimer.Start();
        }

        public void ClearAll()
        {
            lbPotOdds.Text = string.Empty;
            lbEquity.Text = string.Empty;
            lbShouldCall.Text = string.Empty;
            lbAction.Text = string.Empty;
            lbSituacion.Text = "Esperando datos...";
            lbHandStrength.Text = string.Empty;
            lbBoardTexture.Text = string.Empty;
            lbFoldEquity.Text = string.Empty;
            lbEVWithFoldEquity.Text = string.Empty;
            lbSuggestedBetSize.Text = string.Empty;
            actionPanel.BackColor = Color.FromArgb(40, 40, 60);
            lbAction.ForeColor = Color.White;

            _streetBorderColor = Color.FromArgb(60, 60, 80);
            this.Invalidate();
        }

        /// <summary>
        /// Muestra u oculta un label condicional colapsando su fila en el TableLayoutPanel.
        /// </summary>
        private static void SetConditionalVisibility(Label label, bool visible)
        {
            label.Visible = visible;
            label.Text = visible ? label.Text : string.Empty;
            label.Margin = visible ? new Padding(1, 1, 1, 1) : new Padding(0);
        }

        private static string FormatHandRank(HandRank rank) => rank switch
        {
            HandRank.HighCard => "High Card",
            HandRank.OnePair => "One Pair",
            HandRank.TwoPair => "Two Pair",
            HandRank.ThreeOfAKind => "Three of a Kind",
            HandRank.Straight => "Straight",
            HandRank.Flush => "Flush",
            HandRank.FullHouse => "Full House",
            HandRank.FourOfAKind => "Four of a Kind",
            HandRank.StraightFlush => "Straight Flush",
            HandRank.RoyalFlush => "Royal Flush",
            _ => rank.ToString()
        };

        private static string FormatBoardTexture(BoardTextureCategory category) => category switch
        {
            BoardTextureCategory.Dry => "Dry",
            BoardTextureCategory.SemiDry => "Semi-Dry",
            BoardTextureCategory.SemiWet => "Semi-Wet",
            BoardTextureCategory.Wet => "Wet",
            BoardTextureCategory.Paired => "Paired",
            _ => category.ToString()
        };

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            currentOpacity += 0.15f;
            if (currentOpacity >= MAX_OPACITY)
            {
                currentOpacity = MAX_OPACITY;
                animationTimer.Stop();
            }

            int alpha = (int)(currentOpacity * 255);
            if (lbHandStrength != null)
                lbHandStrength.ForeColor = Color.FromArgb(alpha, lbHandStrength.ForeColor.R, lbHandStrength.ForeColor.G, lbHandStrength.ForeColor.B);
            if (lbBoardTexture != null)
                lbBoardTexture.ForeColor = Color.FromArgb(alpha, lbBoardTexture.ForeColor.R, lbBoardTexture.ForeColor.G, lbBoardTexture.ForeColor.B);
            if (lbFoldEquity != null)
                lbFoldEquity.ForeColor = Color.FromArgb(alpha, lbFoldEquity.ForeColor.R, lbFoldEquity.ForeColor.G, lbFoldEquity.ForeColor.B);
            if (lbEVWithFoldEquity != null)
                lbEVWithFoldEquity.ForeColor = Color.FromArgb(alpha, lbEVWithFoldEquity.ForeColor.R, lbEVWithFoldEquity.ForeColor.G, lbEVWithFoldEquity.ForeColor.B);
            if (lbSuggestedBetSize != null)
                lbSuggestedBetSize.ForeColor = Color.FromArgb(alpha, lbSuggestedBetSize.ForeColor.R, lbSuggestedBetSize.ForeColor.G, lbSuggestedBetSize.ForeColor.B);

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
