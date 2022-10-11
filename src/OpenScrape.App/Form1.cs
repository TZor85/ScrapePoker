using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using OpenScrape.App.Models;
using Tesseract;
using System.Linq;
using Image = System.Drawing.Image;
using System.Drawing.Drawing2D;
using OpenScrape.App.UseCases;
using System.Security.Cryptography;
using System.Text;

namespace OpenScrape.App
{
    public partial class Form1 : Form, IAddRegion
    {
        FormRegions _formRegions;
        FormCreateImage _formCreateImage;
        FormImage _formImage;
        Graphics _papel;
        

        List<Regions> _regions = new List<Regions>();
        List<ImageRegion> _images = new List<ImageRegion>();
        List<HashRegion> _hashes = new List<HashRegion>();

        Regions _locRegion = new Regions();
        ImageRegion _locImage = new ImageRegion();
        HashRegion _locHash = new HashRegion();

        Image _img = null;

        private int _speed = 1;
        public string _newRegion = string.Empty;
        private string Key = "8UHjPgXZzXCGkhxV2QCnooyJexUzvJrO";

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
            Image img = null;

            switch (nodo)
            {                
                case "Nodo0":
                    _locRegion = new Regions { Name = texto, Type = "r" };
                    _regions.Add(_locRegion);
                    break;
                case "Nodo2":
                    texto = $"{texto} ({_locRegion.Width}x{_locRegion.Height})";
                    img = CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height));
                    _locImage = new ImageRegion { Name = texto, Image = img };
                    _images.Add(_locImage);
                    break;

                default:
                    break;
            }

            node.Nodes.Add(texto);
            twRegions.ExpandAll();

        }

        private string GetImageHash(string input)
        {   
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                return string.Concat(Convert.ToBase64String(hash).ToCharArray().Where(x => char.IsLetterOrDigit(x)).Take(10));
            }
        }

        private void twRegions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var sen = (TreeView)sender;
            var name = sen.SelectedNode.Text;

            if (twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo2")
            {
                pbImageRegion.Image = _images.FirstOrDefault(x => x.Name == name).Image;
                btnCreateHash.Enabled = true; 
            }

            if(twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo0")
            {
                ckHash.Enabled = true;
            }
            else
            {
                ckHash.Enabled = false;
            }
        }

        private void twRegions_DoubleClick(object sender, EventArgs e)
        {
            EnableButtons();

            _formImage.pbImagen.Refresh();

            if (_locRegion != null)
            {
                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);

                _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
                tbWidth.Text = _locRegion.Width.ToString();
                tbHeight.Text = _locRegion.Height.ToString();
                lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
            }

        }

        

        private void btnSave_Click(object sender, EventArgs e)
        {
            //var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

            //if (region != null)
            //{
            //    if (region.Name.Contains("dealer"))
            //    {
            //        Color color = new Bitmap(_formImage.pbImagen.Image).GetPixel(_locRegion.X, _locRegion.Y);
            //        var col = color.Name.Substring(2, 6);

            //    }
            //    else
            //    {

            //        var ocrengine = new TesseractEngine(@".\tessdata\", "eng", EngineMode.TesseractAndLstm);
                
            //        var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(@"C:\Code\OpenScrape\OpenScrape.App\resources\1.jpg"), 61));
    
            //        Rect area = new Rect(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            //        var res = ocrengine.Process(img, area);
            //        _locRegion.Value = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
            //    }

            //}
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
                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Regions");
                        writer.WriteLine("//");
                        writer.WriteLine();

                        foreach (var item in _regions)
                        {
                            if(!string.IsNullOrEmpty(item.Name))
                                writer.WriteLine($"r${item.Name} # {item.X} - {item.Y} - {item.Width} - {item.Height}");
                        }

                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Hashes");
                        writer.WriteLine("//");
                        writer.WriteLine();

                        foreach (var item in _hashes)
                        {
                            if(!string.IsNullOrEmpty(item.Name))
                                writer.WriteLine($"h${item.Name} - {item.Value}");
                        }

                        writer.WriteLine();
                        writer.WriteLine("//");
                        writer.WriteLine("// Images");
                        writer.WriteLine("//");
                        writer.WriteLine();

                        foreach (var item in _images)
                        {
                            if (item.Image != null)
                            {
                                string imgText = GetImageEncrypted(item.Image);

                                writer.WriteLine($"i${item.Name} - {imgText}");
                            }
                        }

                        writer.Flush();
                        writer.Close();
                    }
                }
                                
            }
        }

        private string GetImageEncrypted(Image? imagen)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                imagen.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] imageBytes = ms.ToArray();

                var base64String = Convert.ToBase64String(imageBytes);

                SHA256 mySHA256 = SHA256Managed.Create();
                byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(Key));

                byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };

                return EncrypterHelper.Encrypt(base64String, key, iv);
            }
        }

        private Image GetImageDecrypted(string base64String)
        {

            SHA256 mySHA256 = SHA256Managed.Create();
            byte[] key = mySHA256.ComputeHash(Encoding.ASCII.GetBytes(Key));


            byte[] iv = new byte[16] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };

            //string encrypted = EncrypterHelper.Encrypt(base64String, key, iv);
            string decrypted = EncrypterHelper.Decrypt(base64String, key, iv);

            byte[] byteImage = Convert.FromBase64String(decrypted);

            var mss = new MemoryStream(byteImage, 0, byteImage.Length);

            return Image.FromStream(mss, true);
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog theDialog = new OpenFileDialog();
                       
            theDialog.Title = "Open Text File";
            theDialog.Filter = "TXT files|*.txt";
            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = theDialog.OpenFile()) != null)
                    {
                        var text = string.Empty;                        
                        using (StreamReader sr = new StreamReader(myStream))
                        {
                            int counter = 0;
                            
                            while ((text = sr.ReadLine()) != null)
                            {
                                var nodo = string.Empty;
                                var name = string.Empty;
                                Regions region = new Regions();
                                ImageRegion imageRegion = new ImageRegion();
                                HashRegion hashRegion = new HashRegion();
                                
                                var type = text.Split('$')[0].Trim();

                                if (type == "r")
                                    nodo = "Nodo0";
                                else if (type == "h")
                                    nodo = "Nodo1";
                                else if (type == "i")
                                    nodo = "Nodo2";

                                if (!string.IsNullOrEmpty(nodo))
                                {
                                    var node = twRegions.Nodes.Find(nodo, true).FirstOrDefault() as TreeNode;

                                    if (node.Name == "Nodo0")
                                    {
                                        name = text.Split('$')[1].Split("#")[0].Trim();
                                        var coord = text.Split('#')[1].Trim();

                                        var coordenadas = coord.Split('-');

                                        region.Name = name;
                                        region.X = int.Parse(coordenadas[0].Trim());
                                        region.Y = int.Parse(coordenadas[1].Trim());
                                        region.Width = int.Parse(coordenadas[2].Trim());
                                        region.Height = int.Parse(coordenadas[3].Trim());

                                        _regions.Add(region);
                                        node.Nodes.Add(name);
                                    }         
                                    else if(node.Name == "Nodo1")
                                    {
                                        name = text.Split('$')[1].Split("-")[0].Trim();
                                        _hashes.Add(new HashRegion { Name = name, Value = text.Split("-")[1].Trim() });
                                        node.Nodes.Add(name);
                                    }
                                    else if(node.Name == "Nodo2")
                                    {
                                        name = text.Split('$')[1].Split("-")[0].Trim();
                                        var hashImage = text.Split("-")[1].Trim();
                                        var image = GetImageDecrypted(hashImage);

                                        _images.Add(new ImageRegion { Name = name, Image = (Image)image });
                                        node.Nodes.Add(name);
                                    }                                    
                                }
                                
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
            //if (!string.IsNullOrWhiteSpace(twRegions.SelectedNode.Text))
            //{
            //    var node = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

            //    twRegions.Nodes.Remove(twRegions.SelectedNode);
            //    _regions.Remove(node);

            //}
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            var img = _useCase.Execute();

            _formImage.Width = img.Width + _formImage.Width / 11;
            _formImage.Height = img.Height + _formImage.Height / 4;

            _formImage.pbImagen.Width = img.Width;
            _formImage.pbImagen.Height = img.Height;

            _formImage.pbImagen.Image = img;

            //TODO: hacer las comparaciones de las imagenes / OCR

            //foreach (var item in _regions)
            //{
            //    if(item.Type == "i")
            //    {
            //        img = CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height));
            //    }
            //}



        }

        private void btnWindow_Click(object sender, EventArgs e)
        {
            Thread.Sleep(2000);
            _useCase.GetWindow();
        }

        private void btnCreateImage_Click(object sender, EventArgs e)
        {
            _formCreateImage = new FormCreateImage();
            _formCreateImage.region = this;
            _formCreateImage.Show();
        }

        private void btnCrateHash_Click(object sender, EventArgs e)
        {   
            var node = twRegions.Nodes.Find("Nodo1", true).FirstOrDefault() as TreeNode;


            if(node != null)
            {
                var name = _locImage.Name.Split(" ")[0].Trim();

                _locHash.Value = GetImageHash(GetImageEncrypted(_locImage.Image));
                _locHash.Name = name;

                _hashes.Add(new HashRegion { Name = _locHash.Name, Value = _locHash.Value });

                node.Nodes.Add(name);
                twRegions.ExpandAll();
            }
                
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            _formCreateImage.Show();
            twRegions.ExpandAll();
        }

        private void ckHash_CheckedChanged(object sender, EventArgs e)
        {
            if (ckHash.Checked)
            {
                _locRegion.IsHash = true;
            }                
            else
            {
                _locRegion.IsHash = false;
            } 
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
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;

        }

        private void btnLeft_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.X -= _speed;
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);


            _img = _formImage.pbImagen.Image;

        }

        private void btnDown_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y += _speed;
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;

        }

        private void btnUp_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);


            _locRegion.Y -= _speed;
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
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
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
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
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
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
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
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
            lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
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
            ckHash.Enabled = true;
        }

       
    }
}



