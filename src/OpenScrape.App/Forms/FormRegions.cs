using Marten;
using OpenScrape.App.Interfaces;
using OpenScrape.Domain.Enums;
using OpenScrape.Features.ActionScenario;
using OpenScrape.Features.Card;
using System.ComponentModel;

namespace OpenScrape.App
{
    public partial class FormRegions : Form
    {
        FrmMain form;
        public string itemSelected = string.Empty;

        //TODO: Ver mejor forma de pasar los datos
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IAddRegion LocRegion { get; set; }

        private readonly IDocumentStore _dataBase;
        private ActionScenarioUseCases _actionScenarioUseCases;
        private CardUseCases _cardUseCases;

        public FormRegions()
        {
            InitializeComponent();
        }

        public FormRegions(IDocumentStore dataBase, ActionScenarioUseCases actionScenarioUseCases, CardUseCases cardUseCases)
        {
            _dataBase = dataBase;
            InitializeComponent();
            _actionScenarioUseCases = actionScenarioUseCases;
            _cardUseCases = cardUseCases;
        }

        private void FormRegions_Load(object sender, EventArgs e)
        {
            form = new FrmMain(_dataBase, _actionScenarioUseCases, _cardUseCases);
            cbRegions.Items.AddRange(ListRegions.Regions.ToArray());
        }

        public string GetRegionSelected()
        {            
            return itemSelected;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            LocRegion.Execute(cbRegions.SelectedItem.ToString(), "Nodo0");
           
            this.Close();
        }

        private void cbRegions_SelectedIndexChanged(object sender, EventArgs e)
        {            
            btnAdd.Enabled = true;
        }
    }
}
