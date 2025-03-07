namespace OpenScrape.App.Forms
{
    public partial class FrmOverlay : Form
    {
        public FrmOverlay()
        {
            InitializeComponent();
        }

        public void UpdateAction(string action)
        {
            lbAction.Text = action;
        }

        public void UpdatePotOddsPercentage(string potOdds)
        {
            lbPotOdds.Text = $"PotOdds: {potOdds}%";
        }

        public void UpdateEquityPercentage(string equity)
        {
            lbEquity.Text = $"Equity: {equity}%";
        }

        public void UpdateShouldCall(bool shouldCall)
        {
            lbShouldCall.Text = shouldCall ? "Pagar" : "No Pagar";
        }
    }

}