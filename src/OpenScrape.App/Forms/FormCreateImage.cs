using OpenScrape.App.Interfaces;
using System.ComponentModel;

namespace OpenScrape.App
{
    public partial class FormCreateImage : Form
    {
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IAddRegion LocRegion { get; set; }

        public FormCreateImage()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbImage.Text))
            {
                LocRegion.Execute(tbImage.Text, "Image");
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
