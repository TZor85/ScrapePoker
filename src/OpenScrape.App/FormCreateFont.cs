using OpenScrape.App.Interfaces;
using OpenScrape.App.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenScrape.App
{
    public partial class FormCreateFont : Form
    {
        public IAddRegion region { get; set; }
        public List<FontRegion> _fonts { get; set; } = new List<FontRegion>();

        public FormCreateFont()
        {
            InitializeComponent();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tbFont.Text))
            {
                if (lbFonts.Items.Count == 1)
                {
                    _fonts.FirstOrDefault(x => x.Id == lbFonts.SelectedItem.ToString().Split("-")[1].Trim()).Name = tbFont.Text;
                    lbFonts.Items.Remove(lbFonts.SelectedItem);
                    region.Execute(_fonts);
                    this.Close();
                }
                else
                {
                    _fonts.FirstOrDefault(x => x.Id == lbFonts.SelectedItem.ToString().Split("-")[1].Trim()).Name = tbFont.Text;
                    tbFont.Text = string.Empty;
                    lbFonts.Items.Remove(lbFonts.SelectedItem);
                }
            }
        }

        private void FormCreateFont_Load(object sender, EventArgs e)
        {
            foreach (var item in _fonts)
            {
                lbFonts.Items.Add($"{item.Name} - {item.Id}");
            }
        }

        private void lbFonts_DoubleClick(object sender, EventArgs e)
        {
            var region = _fonts.FirstOrDefault(x => x.Id == lbFonts.SelectedItem.ToString().Split("-")[1].Trim());
            var locRegion = region.Value;
            var count = region.Value.Count();

            string text = string.Empty;

            for (int i = 0; i < count / 24; i++)
            {
                text += locRegion.Substring(0, 24).Replace("0", "-") + "\r\n";
                locRegion = locRegion.Substring(24);
            }

            rtDraw.Text = text;
        }
    }
}
