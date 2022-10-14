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
using System.Net;
using OpenScrape.App.Entities;
using System.Globalization;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

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

        TableScrapeResult _scrapeResult = new TableScrapeResult();

        Image _img = null;

        private int _speed = 1;
        public string _newRegion = string.Empty;
        private string Key = "8UHjPgXZzXCGkhxV2QCnooyJexUzvJrO";
        private string _p0ColorDealer = "ffd800";
        private string _p1ColorDealer = "ffe005";
        private string _p2ColorDealer = "ffcf03";
        private string _p1ColorActive = "bd2d22";
        private string _p2ColorActive = "bc2c21";

        private readonly GetWindowsScreenUseCase _useCase = new GetWindowsScreenUseCase();
        private readonly ISaveTableMapUseCase _saveUseCase = new SaveTableMapUseCase();
        private readonly ILoadTableMapUseCase _loadUseCase = new LoadTableMapUseCase();

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
            Bitmap img = null;

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
                return string.Concat(Convert.ToBase64String(hash).ToCharArray().Where(x => char.IsLetterOrDigit(x))).ToLower();
            }
        }

        private void twRegions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var sen = (TreeView)sender;
            var name = sen.SelectedNode.Text;

            if (twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo0")
            {
                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                _locRegion = region;

                ckHash.Enabled = true;
                ckColor.Enabled = true;
                ckHash.Checked = region.IsHash;
                ckColor.Checked = region.IsColor;
                pbImageRegion.Image = null;
                tbResult.Text = string.Empty;
            }

            if (twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo1")
            {
                _locHash = _hashes.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                ckHash.Enabled = false;
                ckColor.Enabled = false;
                pbImageRegion.Image = null;
                tbResult.Text = _hashes.FirstOrDefault(x => x.Name == name)?.Value;
            }

            if (twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo2")
            {
                _locImage = _images.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                pbImageRegion.Image = _images.FirstOrDefault(x => x.Name == name).Image;
                btnCreateHash.Enabled = true;
                ckHash.Enabled = false;
                ckColor.Enabled = false;
                tbResult.Text = string.Empty;
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

 
        private void cbSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            int.TryParse(cbSpeed.Text, out _speed);
        }

        private void btnSaveMap_Click(object sender, EventArgs e)
        {
            _saveUseCase.Execute(new SaveTableMapUseCaseRequest { Regions = _regions, Hashes = _hashes, Images = _images, Key = Key });            
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            var response = _loadUseCase.Execute(Key);

            _regions.AddRange(response.Regions);
            _images.AddRange(response.Images);
            _hashes.AddRange(response.Hashes);

            foreach (var item in response.Tree)
            {
                var node = twRegions.Nodes.Find(item.Key, true).FirstOrDefault() as TreeNode;
                
                if(node != null)
                    node.Nodes.Add(item.Value);

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
            //var img = _useCase.Execute();

            //_formImage.Width = img.Width + _formImage.Width / 11;
            //_formImage.Height = img.Height + _formImage.Height / 4;

            //_formImage.pbImagen.Width = img.Width;
            //_formImage.pbImagen.Height = img.Height;

            //_formImage.pbImagen.Image = img;


            //TODO: hacer las comparaciones de las imagenes / OCR

            foreach (var item in _regions)
            {
                if (item.IsHash)
                {

                    
                    var img = CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height));

                    pbZoom.Image = img;
                    pbZoom.Refresh();


                    var locHash = GetImageHash(EncrypterHelper.GetImageEncrypted(img, Key));

                    var hashName = _hashes.Any(x => x.Value == locHash) ? _hashes.FirstOrDefault(x => x.Value == locHash)?.Name : string.Empty;



                    switch (item.Name)
                    {
                        case "u0cardface0":
                            _scrapeResult.U0CardFace0 = hashName;
                            break;
                        case "u0cardface1":
                            _scrapeResult.U0CardFace1 = hashName;
                            break;
                        default:
                            break;
                    }
                    
                    
                }
                else if (item.IsColor)
                {
                    Color color = new Bitmap(_formImage.pbImagen.Image).GetPixel(item.X, item.Y);
                    var rgbColor = color.Name.Substring(2, 6);

                    if (rgbColor == _p0ColorDealer)
                        _scrapeResult.P0Dealer = true;
                    else if (rgbColor == _p1ColorDealer)
                        _scrapeResult.P1Dealer = true;
                    else if (rgbColor == _p2ColorDealer)
                        _scrapeResult.P2Dealer = true;

                    if (rgbColor == _p1ColorActive)
                        _scrapeResult.P1Active = true;

                    if (rgbColor == _p2ColorActive)
                        _scrapeResult.P2Active = true;



                    //if (rgbColor == _p0ColorDealer || rgbColor == _p1ColorDealer || rgbColor == _p2ColorDealer)
                    //{
                    //    switch (item.Name)
                    //    {
                    //        case "p0dealer":
                    //            _scrapeResult.P0Dealer = true;
                    //            break;
                    //        case "p1dealer":
                    //            _scrapeResult.P1Dealer = true;
                    //            break;
                    //        case "p2dealer":
                    //            _scrapeResult.P2Dealer = true;
                    //            break;
                    //        case "p1active":
                    //            _scrapeResult.P1Active = true;
                    //            break;
                    //        case "p2active":
                    //            _scrapeResult.P2Active = true;
                    //            break;
                    //        default:
                    //            break;
                    //    }
                    //}                    
                    
                }
                else
                {
                    var ocrengine = new TesseractEngine(@".\tessdata\", "eng", EngineMode.TesseractAndLstm);

                    var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), 100));

                    Rect area = new Rect(item.X, item.Y, item.Width, item.Height);
                    var res = ocrengine.Process(img, area);

                    switch (item.Name)
                    {
                        case "p0name":
                            _scrapeResult.P0Name = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
                            break;
                        case "p1name":
                            _scrapeResult.P1Name = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
                            break;
                        case "p2name":
                            _scrapeResult.P2Name = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
                            break;
                        case "p0chips":
                            _scrapeResult.P0Chips = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
                            break;
                        case "p1chips":
                            _scrapeResult.P1Chips = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
                            break;
                        case "p2chips":
                            _scrapeResult.P2Chips = string.IsNullOrWhiteSpace(res.GetText()) ? string.Empty : res.GetText();
                            break;
                        default:
                            break;
                    }

                    
                }

            }

            var ppp = _scrapeResult;

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

                _locHash.Value = GetImageHash(EncrypterHelper.GetImageEncrypted(_locImage.Image, Key));
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
                var node = twRegions.Nodes.Find("Nodo0", true).FirstOrDefault() as TreeNode;

                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                if (region != null)
                    region.IsHash = true;

            }                
            else
            {
                var node = twRegions.Nodes.Find("Nodo0", true).FirstOrDefault() as TreeNode;

                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                if (region != null)
                    region.IsHash = false;
            } 
        }

        private void ckColor_CheckedChanged(object sender, EventArgs e)
        {
            if (ckColor.Checked)
            {
                var node = twRegions.Nodes.Find("Nodo0", true).FirstOrDefault() as TreeNode;

                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                var rgbRequest = new GetRGBColorRequest
                {
                    Image = _formImage.pbImagen.Image,
                    X = _locRegion.X,
                    Y = _locRegion.Y
                };

                var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

                if (ckColor.Checked)
                {
                    tbR.Text = rgbResponse.RColor;
                    tbG.Text = rgbResponse.GColor;
                    tbB.Text = rgbResponse.BColor;
                }

                if (region != null)
                {
                    region.IsColor = true;
                    _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";
                }
                    

            }
            else
            {
                var node = twRegions.Nodes.Find("Nodo0", true).FirstOrDefault() as TreeNode;

                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                tbR.Text = string.Empty;
                tbG.Text = string.Empty;
                tbB.Text = string.Empty;

                if (region != null)
                    region.IsColor = false;
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

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.X += _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";

            _img = _formImage.pbImagen.Image;

        }

        private void btnLeft_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.X -= _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";


            _img = _formImage.pbImagen.Image;

        }

        private void btnDown_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.Y += _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";

            _img = _formImage.pbImagen.Image;

        }

        private void btnUp_Click(object sender, EventArgs e)
        {

            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.Y -= _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";


            _img = _formImage.pbImagen.Image;
        }

        private void btnUpLeft_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.Y -= _speed;
            _locRegion.X -= _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";


            _img = _formImage.pbImagen.Image;
        }

        private void btnUpRight_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.Y -= _speed;
            _locRegion.X += _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";


            _img = _formImage.pbImagen.Image;
        }

        private void btnDownLeft_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.Y += _speed;
            _locRegion.X -= _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";


            _img = _formImage.pbImagen.Image;
        }

        private void btnDownRight_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            var rgbRequest = new GetRGBColorRequest
            {
                Image = _formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (ckColor.Checked)
            {
                tbR.Text = rgbResponse.RColor;
                tbG.Text = rgbResponse.GColor;
                tbB.Text = rgbResponse.BColor;
            }

            _locRegion.Y += _speed;
            _locRegion.X += _speed;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);
            _locRegion.Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";


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



