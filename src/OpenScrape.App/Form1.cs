using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using OpenScrape.App.Models;
using Tesseract;
using System.Linq;
using Image = System.Drawing.Image;
using System.Drawing.Drawing2D;
using OpenScrape.App.UseCases;

namespace OpenScrape.App
{
    public partial class Form1 : Form, IAddRegion
    {
        FormRegions _formRegions;
        FormCreateImage _formCreateImage;
        FormImage _formImage;
        Graphics _papel;
        

        List<RectangleRegion> _regions = new List<RectangleRegion>();
        RectangleRegion _locRegion = null;

        Image _img = null;

        private int _speed = 1;
        public string _newRegion = string.Empty;

        private readonly GetWindowsScreenUseCase _useCase = new GetWindowsScreenUseCase();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _formImage = new FormImage();
            cbSpeed.SelectedIndex = 0;
            
            _formImage.Location = new Point(this.Width, this.Location.Y);
            _formImage.Show();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            _formRegions = new FormRegions();

            _formRegions.Location = new Point(_formImage.Location.X, _formImage.Location.Y + 100);
            _formRegions.region = this;
            _formRegions.Show();

        }


        public void Execute(string texto, string nodo)
        {
            var node = twRegions.Nodes.Find(nodo, true).FirstOrDefault() as TreeNode;
            var type = string.Empty;

            switch (nodo)
            {
                case "Nodo0":
                    type = "r";
                    break;
                case "Nodo2":
                    type = "i";
                    break;

                default:
                    break;
            }

            if (nodo == "Nodo2")
                texto = $"{texto} ({_locRegion.Width}x{_locRegion.Height})"; 

            node.Nodes.Add(texto);
            twRegions.ExpandAll();

            _regions.Add(new RectangleRegion { Name = texto, Type = type });

        }

        

        private void twRegions_Click_1(object sender, EventArgs e)
        {
            //var pp = twRegions.SelectedNode.NextVisibleNode.Text;
        }

        private void twRegions_DoubleClick(object sender, EventArgs e)
        {
            EnableButtons();

            _formImage.pbImagen.Refresh();

            _locRegion = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

            if (_locRegion != null)
            {
                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);

                _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
                tbWidth.Text = _locRegion.Width.ToString();
                tbHeight.Text = _locRegion.Height.ToString();
            }

            //Image im = _formImage.pbImagen.Image;
            //var pp = im.ToString();

        }

        

        private void btnSave_Click(object sender, EventArgs e)
        {
            var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

            if (region != null)
            {
                if (region.Name.Contains("dealer"))
                {
                    Color color = new Bitmap(_formImage.pbImagen.Image).GetPixel(_locRegion.X, _locRegion.Y);
                    var col = color.Name.Substring(2, 6);

                }
                else
                {

                    var ocrengine = new TesseractEngine(@".\tessdata\", "eng", EngineMode.TesseractAndLstm);
                
                    var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(@"C:\Code\OpenScrape\OpenScrape.App\resources\1.jpg"), 61));
    
                    Rect area = new Rect(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
                    var res = ocrengine.Process(img, area);
                    _locRegion.Value = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
                }

            }
        }        

        private void cbSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            int.TryParse(cbSpeed.Text, out _speed);
        }

        private void btnSaveMap_Click(object sender, EventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text|*.txt";
            saveFileDialog.Title = "Save an Text File";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                
                if(saveFileDialog.FileName != "")
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.OpenFile()))
                    {
                                              
                        foreach (var item in _regions)
                        {
                            writer.WriteLine($"{item.Type}${item.Name} # {item.X} - {item.Y} - {item.Width} - {item.Height}");
                        }


                        writer.Flush();
                        writer.Close();
                    }
                }
                                
            }
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog theDialog = new OpenFileDialog();

            var nodo = string.Empty;
            
            theDialog.Title = "Open Text File";
            theDialog.Filter = "TXT files|*.txt";
            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = theDialog.OpenFile()) != null)
                    {
                        string text = "";
                        using (StreamReader sr = new StreamReader(myStream))
                        {
                            int counter = 0;
                            
                            while ((text = sr.ReadLine()) != null)
                            {
                                RectangleRegion region = new RectangleRegion();
                                
                                var type = text.Split('$')[0].Trim();

                                if (type == "r")
                                    nodo = "Nodo0";
                                else if (type == "i")
                                    nodo = "Nodo2";

                                var node = twRegions.Nodes.Find(nodo, true).FirstOrDefault() as TreeNode;

                                var name = text.Split('#')[0].Trim();
                                var coord = text.Split('#')[1].Trim();

                                var coordenadas = coord.Split('-');

                                region.Name = name;
                                region.X = int.Parse(coordenadas[0].Trim());
                                region.Y = int.Parse(coordenadas[1].Trim());
                                region.Width = int.Parse(coordenadas[2].Trim());
                                region.Height = int.Parse(coordenadas[3].Trim());


                                node.Nodes.Add(name);

                                _regions.Add(region);
                                counter++;
                            }
                        }
                    }

                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }


        public Image PictureBoxZoom(Image img, Size size)
        {
           
            Bitmap bm = new Bitmap(img, Convert.ToInt32(img.Width * size.Width), Convert.ToInt32(img.Height * size.Height));
            Graphics grap = Graphics.FromImage(bm);
            grap.InterpolationMode = InterpolationMode.HighQualityBicubic;
            return bm;
        }

        public Bitmap CropImage(Bitmap source, Rectangle section)
        {
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            Graphics g = Graphics.FromImage(bmp);

            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);

            return bmp;
        }

        private void btnZoom_Click(object sender, EventArgs e)
        {
            var bitmap = CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height));

            pbZoom.Image = PictureBoxZoom(bitmap, new Size(50, 50));
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(twRegions.SelectedNode.Text))
            {
                var node = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                twRegions.Nodes.Remove(twRegions.SelectedNode);
                _regions.Remove(node);

            }
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            var img = _useCase.Execute();

            _formImage.Width = img.Width + _formImage.Width / 11;
            _formImage.Height = img.Height + _formImage.Height / 4;

            _formImage.pbImagen.Width = img.Width;
            _formImage.pbImagen.Height = img.Height;

            _formImage.pbImagen.Image = img;
        }

        private void btnCreateImage_Click(object sender, EventArgs e)
        {
            _formCreateImage = new FormCreateImage();
            _formCreateImage.region = this;
            _formCreateImage.Show();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            _formCreateImage.Show();
        }

        #region Tamaño Region


        private void btnPlusWidth_Click(object sender, EventArgs e)
        {
            
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.Width += _speed;
            tbWidth.Text = _locRegion.Width.ToString();
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;
            
        }

        private void btnMinusWidth_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.Width -= _speed;
            tbWidth.Text = _locRegion.Width.ToString();
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;            
        }

        private void btnPlusHeight_Click(object sender, EventArgs e)
        {
            
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.Height += _speed;
            tbHeight.Text = _locRegion.Height.ToString();
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;
            
        }

        private void btnMinusHeight_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.Height -= _speed;
            tbHeight.Text = _locRegion.Height.ToString();
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;
            
        }


        #endregion

        #region Movimiento Region

        private void btnRigth_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.X += _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;

        }

        private void btnLeft_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.X -= _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;

        }

        private void btnDown_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y += _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;

        }

        private void btnUp_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y -= _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;
        }

        private void btnUpLeft_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y -= _speed;
            _locRegion.X -= _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;
        }

        private void btnUpRight_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y -= _speed;
            _locRegion.X += _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;
        }

        private void btnDownLeft_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y += _speed;
            _locRegion.X -= _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;
        }

        private void btnDownRight_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y += _speed;
            _locRegion.X += _speed;
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;
        }



        #endregion

        private void EnableButtons()
        {
            btnPlusHeight.Enabled = true;
            btnPlusWidth.Enabled = true;
            btnMinusHeight.Enabled = true;
            btnMinusWidth.Enabled = true;
            btnUp.Enabled = true;
            btnUpRight.Enabled = true;
            btnRigth.Enabled = true;
            btnDownRight.Enabled = true;
            btnDown.Enabled = true;
            btnDownLeft.Enabled = true;
            btnLeft.Enabled = true;
            btnUpLeft.Enabled = true;
            btnZoom.Enabled = true;
            btnCreateImage.Enabled = true;
        }

    }
}



