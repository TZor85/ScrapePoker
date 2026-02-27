using System.Runtime.InteropServices;
using OpenScrape.App.Aplication.UseCases;

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
        }

        public void UpdateSuggestedBetSize(double? betSize)
        {
            if (!betSize.HasValue || betSize <= 0)
            {
                lbSuggestedBetSize.Text = string.Empty;
                lbSuggestedBetSize.Visible = false;
                return;
            }

            lbSuggestedBetSize.Text = $"Bet: {betSize:F1}x";
            lbSuggestedBetSize.Visible = true;
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
                Location = new Point(0, 92), // Below existing panels
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
                Location = new Point(0, 111),
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
                Location = new Point(0, 130),
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

            // Increase form height to accommodate new panels
            this.ClientSize = new Size(214, 150);
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