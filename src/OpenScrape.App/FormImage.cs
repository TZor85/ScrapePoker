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

            //pbImagen.Image.Save(@"C:\Code\Poker\OpenScrape\OpenScrape.App\resources\img_" + DateTime.UtcNow.Ticks + ".jpg");
            //pbImagen.Image.Save(@"C:\Code\OpenScrape\OpenScrape.App\resources\1.jpg");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                var folderPath = @"C:\Code\ScrapePoker\resources\Games\Game_" + new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                dlg.InitialDirectory = folderPath;
                dlg.Title = "Open Image";
                dlg.Filter = "Image Files (*.bmp;*.jpg;*.jpeg;*.png)|*.BMP;*.JPG;*.JPEG;*.PNG";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.Width = 461;
                    this.Height = 327;

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
