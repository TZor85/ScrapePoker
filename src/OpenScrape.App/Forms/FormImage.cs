using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using System.Diagnostics;

namespace OpenScrape.App
{
    public partial class FormImage : Form, IAddImage
    {
        private FormListApps _formListApps;
        

        public FormImage()
        {
            InitializeComponent();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            _formListApps = new FormListApps();

            _formListApps.Location = new Point(this.Location.X, this.Location.Y + 100);
            _formListApps.addImage = this;
            _formListApps.Show();

        }
                
        private void FormImage_Load(object sender, EventArgs e)
        {

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
                    folderPath = @"C:\Code\ScrapePoker\resources\Games";
                    //portatil
                    //folderPath = @"C:\Code\Poker\ScrapePoker\resources\Games";
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
                    this.Width = 461;
                    this.Height = 327;

                    lbPath.Text = dlg.FileName;

                    var bitmap = new Bitmap(dlg.FileName);

                    this.Width = bitmap.Width + this.Width / 11;
                    this.Height = bitmap.Height + this.Height / 4;

                    pbImagen.Width = bitmap.Width;
                    pbImagen.Height = bitmap.Height;

                    pbImagen.Image = bitmap;
                }
            }
        }
    }
}
