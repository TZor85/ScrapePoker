using System.Runtime.InteropServices;

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

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public FrmOverlay()
        {
            InitializeComponent();
            this.MouseDown += FrmOverlay_MouseDown;
        }

        public void UpdateAction(string action)
        {
            lbAction.Text = action;
        }

        public void UpdatePotOddsPercentage(string potOdds)
        {
            lbPotOdds.Text = $"PotOdds: {Math.Round(decimal.Parse(potOdds), 2)}%";
        }

        public void UpdateEquityPercentage(string equity)
        {
            lbEquity.Text = $"Equity: {equity}%";
        }

        public void UpdateShouldCall(bool shouldCall)
        {
            lbShouldCall.Text = shouldCall ? "Pagar" : "No Pagar";
        }

        public void UpdateSituacion(string situacion)
        {
            lbSituacion.Text = situacion;
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