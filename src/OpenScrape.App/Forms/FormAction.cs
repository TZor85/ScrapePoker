using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenScrape.App.Forms
{
    public partial class FormAction : Form
    {
        private string? datoRecibido;

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
