using System.ComponentModel;

namespace OpenScrape.App.Forms
{
    public partial class FormAction : Form
    {
        private string? datoRecibido;

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string DatoRecibido
        {
            get { return datoRecibido ?? string.Empty; }
            set
            {
                datoRecibido = value;
                ActualizarDatosEnInterfaz(); 
            }
        }

        

        private void ActualizarDatosEnInterfaz()
        {
            //label1.Text = pruebaTexto;
            lbAction.Text = datoRecibido;
        }

        public FormAction()
        {
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
