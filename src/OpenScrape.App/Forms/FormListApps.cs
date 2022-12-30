using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;

namespace OpenScrape.App
{
    public partial class FormListApps : Form
    {
        private IEnumerable<KeyValuePair<string, IntPtr>> _windows = new List<KeyValuePair<string, IntPtr>>();
        public IntPtr windowsValue;
        public IAddImage addImage { get; set; }

        
        public FormListApps()
        {
            InitializeComponent();
        }

        private void FormListApps_Load(object sender, EventArgs e)
        {

            List<string> windowsName = new List<string>();

            _windows = WindowsInformationHelper.FindWindows();

            foreach (var item in _windows)
            {
                lbApps.Items.Add(item.Key);
            }

        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lbApps_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAceptar.Enabled = true;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            var selectItem = lbApps.SelectedItem.ToString();
            addImage.Execute(_windows.FirstOrDefault(x => x.Key == selectItem).Value);

            this.Close();

        }
    }
}
