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
                // Realizar acciones en el formulario destino cuando el dato cambia
                ActualizarDatosEnInterfaz(); // Método para actualizar la interfaz con el nuevo dato
            }
        }

        // Método para actualizar la interfaz con el nuevo dato
        private void ActualizarDatosEnInterfaz()
        {
            // Actualizar controles, etiquetas, etc., con el nuevo dato
            // Ejemplo:
            lbAction.Text = datoRecibido;
        }

        public FormAction()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
