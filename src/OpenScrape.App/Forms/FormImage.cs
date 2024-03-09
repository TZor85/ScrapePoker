using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using System.Diagnostics;

namespace OpenScrape.App
{
    public partial class FormImage : Form, IAddImage
    {
        private FormListApps _formListApps;
        private int _index = 0;
        private List<string> _listaImagenes = new List<string>();

        public FormImage()
        {
            InitializeComponent();
        }

        public void Execute(IntPtr window)
        {
            Image img = CaptureWindowsHelper.CaptureWindow(window);

            this.Width = img.Width + this.Width / 11;
            this.Height = img.Height + this.Height / 4;

            pbImagen.Width = img.Width;
            pbImagen.Height = img.Height;

            pbImagen.Image = img;

        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                var folderPath = string.Empty;

                if (string.IsNullOrWhiteSpace(lbPath.Text))
                {
                    //folderPath = @"C:\Code\ScrapePoker\resources\Games";
                    //portatil
                    folderPath = @"C:\Code\Poker\ScrapePoker\resources\Games";
                }
                else
                {
                    folderPath = lbPath.Text.Split("game_")[0];
                }

                dlg.InitialDirectory = folderPath;
                dlg.Title = "Open Image";
                dlg.Filter = "Image Files (*.bmp;*.jpg;*.jpeg;*.png)|*.BMP;*.JPG;*.JPEG;*.PNG";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    lbPath.Text = dlg.FileName;

                    var bitmap = new Bitmap(dlg.FileName);

                    _listaImagenes = new List<string>(Directory.GetFiles(lbPath.Text.Split("game")[0], "*.png"));

                    this.Width = 461;
                    this.Height = 347;

                    this.Width = bitmap.Width + this.Width / 11;
                    this.Height = bitmap.Height + this.Height / 4;

                    pbImagen.Width = bitmap.Width;
                    pbImagen.Height = bitmap.Height;

                    MostrarImagenActual();
                }
            }
        }

        private void MostrarImagenActual()
        {
            if (_listaImagenes.Count > 0 && _index >= 0 && _index < _listaImagenes.Count)
            {
                // Cargar y mostrar la imagen actual
                pbImagen.Image = Image.FromFile(_listaImagenes[_index]);
                lbPath.Text = _listaImagenes[_index];
                lbTotal.Location = new Point(lbPath.Size.Width + 410, lbPath.Location.Y);
                lbTotal.Text = $"{_index}/{_listaImagenes.Count - 1}";
            }
        }

        private void btnMinus_Click(object sender, EventArgs e)
        {
            _index = (_index - 1 + _listaImagenes.Count) % _listaImagenes.Count;
            MostrarImagenActual();
        }

        private void btnPlus_Click(object sender, EventArgs e)
        {
            _index = (_index + 1) % _listaImagenes.Count;
            MostrarImagenActual();
        }
    }
}
