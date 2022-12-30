using OpenScrape.App.Enums;
using OpenScrape.App.Interfaces;

namespace OpenScrape.App
{
    public partial class FormRegions : Form
    {
        Form1 form;
        public string itemSelected = string.Empty;
        public IAddRegion region { get; set; }

        public FormRegions()
        {
            InitializeComponent();
        }

        private void FormRegions_Load(object sender, EventArgs e)
        {
            form = new Form1();
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
            region.Execute(cbRegions.SelectedItem.ToString(), "Nodo0");
           
            this.Close();
        }

        private void cbRegions_SelectedIndexChanged(object sender, EventArgs e)
        {            
            btnAdd.Enabled = true;
        }
    }
}
