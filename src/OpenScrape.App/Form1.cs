using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using OpenScrape.App.Models;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using Tesseract;
using Image = System.Drawing.Image;
using IronOcr;

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
        List<ImageRegion> _imagesBoard = new List<ImageRegion>();

        Regions _locRegion = new Regions();
        ImageRegion _locImage = new ImageRegion();

        TableScrapeResult _scrapeResult = new TableScrapeResult();

        Image _img = null;

        private int _speed = 1;
        public string _newRegion = string.Empty;
        private string Key = "8UHjPgXZzXCGkhxV2QCnooyJexUzvJrO";

        private List<int> _colorDealer = new List<int> { 250, 251, 252, 253, 254, 255 };
        private List<int> _colorActive = new List<int> {33, 34, 35, 36, 37};


        private string _p1ColorSit = "000000";
        private string _p2ColorSit = "000000";
        private string _p1ColorSit1 = "0b0b0d";
        private string _p2ColorSit1 = "090909";

        private string? _effectiveStack = string.Empty;

        private int _umbral = 86;

        private readonly GetWindowsScreenUseCase _useCase = new GetWindowsScreenUseCase();
        private readonly GetActions3HandedUseCase _actions3HandedUseCase = new GetActions3HandedUseCase();
        private readonly GetActions2HandedUseCase _actions2HandedUseCase = new GetActions2HandedUseCase();
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
            if (nodo == "Image")
                nodo = _locRegion.IsBoard ? "Nodo1" : "Nodo2";

            var node = twRegions.Nodes.Find(nodo, true).FirstOrDefault() as TreeNode;
            var type = string.Empty;
            Bitmap img = null;

            switch (nodo)
            {                
                case "Nodo0":
                    _locRegion = new Regions { Name = texto, Type = "r" };
                    _regions.Add(_locRegion);
                    break;
                case "Nodo1":
                    texto = $"{texto} ({_locRegion.Width}x{_locRegion.Height})";
                    img = CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height));// CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), _umbral);
                    _locImage = new ImageRegion { Name = texto, Image = img, isBoard = true };
                    _imagesBoard.Add(_locImage);
                    break;
                case "Nodo2":
                    texto = $"{texto} ({_locRegion.Width}x{_locRegion.Height})";
                    img = CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height));// CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), _umbral);
                    _locImage = new ImageRegion { Name = texto, Image = img, isBoard = false };
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

                ckColor.Enabled = true;
                ckColor.Checked = region.IsColor;
                ckBoard.Enabled = true;
                ckBoard.Checked = region.IsBoard;
                pbImageRegion.Image = null;
            }

            if (twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo2")
            {
                _locImage = _images.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                pbImageRegion.Image = _images.FirstOrDefault(x => x.Name == name).Image;
                ckColor.Enabled = false;
                ckBoard.Enabled = false;

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
            _saveUseCase.Execute(new SaveTableMapUseCaseRequest { Regions = _regions, Images = _images, Key = Key, Board = _imagesBoard });            
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            var response = _loadUseCase.Execute(Key);

            _regions.Clear();
            _images.Clear();

            _regions.AddRange(response.Regions);
            _images.AddRange(response.Images);

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

        public static List<bool> GetHash(Bitmap bmpSource)
        {
            List<bool> lResult = new List<bool>();
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(55, 55));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }
            return lResult;
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {

            if (!cbTest.Checked)
            {
                GetImageWhilePlaying();
            }

            //TODO: hacer las comparaciones de las imagenes / OCR

            _scrapeResult = new TableScrapeResult();

            var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), _umbral));
            

            foreach (var item in _regions)
            {
                if (item.IsHash)
                {
                    var maxEqual = 0;
                    var max = 0;

                    foreach (var immg in _images)
                    {
                        List<bool> iHash1 = GetHash(new Bitmap(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height))));
                        List<bool> iHash2 = GetHash(new Bitmap(immg.Image));

                        int equalElements = iHash1.Zip(iHash2, (i, j) => i == j).Count(eq => eq);

                        if(equalElements > maxEqual)
                            maxEqual = equalElements;

                        if (maxEqual > max && maxEqual >= (3025 * 0.96))
                        {
                            switch (item.Name)
                            {
                                case "u0cardface0":
                                    _scrapeResult.U0CardFace0 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                case "u0cardface1":
                                    _scrapeResult.U0CardFace1 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                case "b0card1":
                                    _scrapeResult.B0Card1 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                else if (item.IsColor)
                {
                    if (_formImage.pbImagen.Image != null)
                    {
                        Color color = new Bitmap(_formImage.pbImagen.Image).GetPixel(item.X, item.Y);
                        var rgbColor = color.Name.Substring(2, 6);

                        switch (item.Name)
                        {
                            case "p0dealer":
                                if (_colorDealer.Contains(color.R))
                                    _scrapeResult.P0Dealer = true;
                                break;
                            case "p1dealer":
                                if (_colorDealer.Contains(color.R))
                                    _scrapeResult.P1Dealer = true;
                                break;
                            case "p2dealer":
                                if (_colorDealer.Contains(color.R))
                                    _scrapeResult.P2Dealer = true;
                                break;
                            case "p1active":
                                if (_colorActive.Contains(color.B))
                                    _scrapeResult.P1Active = true;
                                break;
                            case "p2active":
                                if (_colorActive.Contains(color.B))
                                    _scrapeResult.P2Active = true;
                                break;
                            case "p1sit":
                                if (rgbColor == _p1ColorSit || rgbColor == _p1ColorSit1)
                                    _scrapeResult.P1Sit = true;
                                break;
                            case "p2sit":
                                if (rgbColor == _p2ColorSit || rgbColor == _p2ColorSit1)
                                    _scrapeResult.P2Sit = true;
                                break;
                            default:
                                break;
                        }
                    }

                }
                else
                {
                    var ocrengine = new TesseractEngine(@".\tessdata\", "eng", EngineMode.TesseractOnly);
                    Rect area = new Rect(item.X, item.Y, item.Width, item.Height);
                    var res = ocrengine.Process(img, area, PageSegMode.SingleLine);

                    var text = string.Empty;
                    var resultText = string.Empty;

                    switch (item.Name)
                    {
                        case "p0chips":
                            _scrapeResult.P0Chips = res.GetText();
                            break;
                        case "p1chips":
                            _scrapeResult.P1Chips = res.GetText();
                            break;
                        case "p2chips":
                            _scrapeResult.P2Chips = res.GetText();
                            break;
                        case "p1bet":
                            resultText = res.GetText();
                            if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("Z"))
                                text = "2";
                            else if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("S"))
                                text = "3";
                            else
                                text = resultText.Replace("BB", "").Trim().Replace(" ", ",");

                            double.TryParse(text, out double p1Bet);
                            if (p1Bet == 50)
                                p1Bet = 0.5f;
                            _scrapeResult.P1Bet = p1Bet;
                            break;
                        case "p2bet":
                            resultText = res.GetText();
                            if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("Z"))
                                text = "2";
                            else if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("S"))
                                text = "3";
                            else
                                text = resultText.Replace("BB", "").Trim().Replace(" ", ",");
                                                        
                            double.TryParse(text, out double p2Bet);
                            if (p2Bet == 50)
                                p2Bet = 0.5f;
                            _scrapeResult.P2Bet = p2Bet;
                            break;
                        default:
                            break;
                    }
                }
            }

            SetEmptyValues();

            lbP0Chips.Text += _scrapeResult.P0Chips;
            lbP1Chips.Text += _scrapeResult.P1Chips;
            lbP2Chips.Text += _scrapeResult.P2Chips;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace0))
                pbCard0.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.U0CardFace0))!.Image;
            else
                pbCard0.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace1))
                pbCard1.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.U0CardFace1))!.Image;
            else
                pbCard1.Image = null;            

            lbP1Bet.Text = _scrapeResult.P1Bet.ToString();
            lbP2Bet.Text = _scrapeResult.P2Bet.ToString();

            if (_scrapeResult.P0Dealer)
                lbDealer0.Text = "Dealer";
            else
                lbDealer0.Text = string.Empty;

            if (_scrapeResult.P1Dealer)
                lbDealer1.Text = "Dealer";
            else
                lbDealer1.Text = string.Empty;

            if (_scrapeResult.P2Dealer)
                lbDealer2.Text = "Dealer";
            else
                lbDealer2.Text = string.Empty;

            lbEfective.Text = "Ef BB: ";
            _effectiveStack = GetEffectiveBB(_scrapeResult.P0Chips, _scrapeResult.P1Chips, _scrapeResult.P2Chips);
            lbEfective.Text += _effectiveStack;

            if (_scrapeResult.P1Sit && _scrapeResult.P2Sit)
                lbAction.Text = GetAction3Handed();
            else
                lbAction.Text = GetAction2Handed();
        }

        private void btnBoard_Click(object sender, EventArgs e)
        {
            if (!cbTest.Checked)
            {
                GetImageWhilePlaying();
            }

            _scrapeResult.B0Card1 = string.Empty;
            _scrapeResult.B0Card2 = string.Empty;
            _scrapeResult.B0Card3 = string.Empty;
            _scrapeResult.B0Card4 = string.Empty;
            _scrapeResult.B0Card5 = string.Empty;

            foreach (var item in _regions)
            {
                if (item.IsBoard)
                {
                    var maxEqual = 0;
                    var max = 0;

                    foreach (var immg in _imagesBoard)
                    {
                        List<bool> iHash1 = GetHash(new Bitmap(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height))));
                        List<bool> iHash2 = GetHash(new Bitmap(immg.Image));

                        int equalElements = iHash1.Zip(iHash2, (i, j) => i == j).Count(eq => eq);

                        if (equalElements > maxEqual)
                            maxEqual = equalElements;

                        if (maxEqual > max && maxEqual >= (3025 * 0.91))
                        {
                            switch (item.Name)
                            {
                                case "b0card1":
                                    _scrapeResult.B0Card1 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                case "b0card2":
                                    _scrapeResult.B0Card2 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                case "b0card3":
                                    _scrapeResult.B0Card3 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                case "b0card4":
                                    _scrapeResult.B0Card4 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                case "b0card5":
                                    _scrapeResult.B0Card5 = immg.Name.Split(" ")[0];
                                    max = maxEqual;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(_scrapeResult.B0Card1))
                pbFlop1.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.B0Card1))!.Image;
            else
                pbFlop1.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.B0Card2))
                pbFlop2.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.B0Card2))!.Image;
            else
                pbFlop2.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.B0Card3))
                pbFlop3.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.B0Card3))!.Image;
            else
                pbFlop3.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.B0Card4))
                pbTurn.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.B0Card4))!.Image;
            else
                pbTurn.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.B0Card5))
                pbRiver.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.B0Card5))!.Image;
            else
                pbRiver.Image = null;
        }

        private void GetImageWhilePlaying()
        {
            var folderPath = @"C:\Code\ScrapePoker\resources\Games\Game_" + new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var path = folderPath + @"\game_" + DateTime.Now.Ticks + ".png";

            _useCase.ExecuteImage(path);

            var windowImg = Image.FromFile(path);

            _formImage.Width = 461;
            _formImage.Height = 327;

            _formImage.Width = windowImg.Width + _formImage.Width / 11;
            _formImage.Height = windowImg.Height + _formImage.Height / 4;

            _formImage.pbImagen.Width = windowImg.Width;
            _formImage.pbImagen.Height = windowImg.Height;

            _formImage.pbImagen.Image = windowImg;
            _formImage.pbImagen.Refresh();

            Thread.Sleep(1000);
        }

        

        private string GetAction2Handed()
        {
            try
            {
                if (_scrapeResult.P1Bet != 0.5 && _scrapeResult.P1Bet.ToString().Count() == 3)
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]},{_scrapeResult.P1Bet.ToString()[1]}{_scrapeResult.P1Bet.ToString()[2]}");
                else if (_scrapeResult.P1Bet.ToString().Count() > 3)
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]}{_scrapeResult.P1Bet.ToString()[1]},{_scrapeResult.P1Bet.ToString()[2]}{_scrapeResult.P1Bet.ToString()[3]}");

                if (_scrapeResult.P2Bet != 0.5 && _scrapeResult.P2Bet.ToString().Count() == 3)
                    _scrapeResult.P2Bet = double.Parse($"{_scrapeResult.P2Bet.ToString()[0]},{_scrapeResult.P2Bet.ToString()[1]}{_scrapeResult.P2Bet.ToString()[2]}");
                else if (_scrapeResult.P2Bet.ToString().Count() > 3)
                    _scrapeResult.P2Bet = double.Parse($"{_scrapeResult.P2Bet.ToString()[0]}{_scrapeResult.P2Bet.ToString()[1]},{_scrapeResult.P2Bet.ToString()[2]}{_scrapeResult.P2Bet.ToString()[3]}");

                if (_scrapeResult.P0Dealer)
                    return _actions2HandedUseCase.ExecuteOpenRaise(
                        new GetActions2HandedRequest
                        {
                            Card0 = _scrapeResult.U0CardFace0,
                            Card1 = _scrapeResult.U0CardFace1,
                            EffectiveStack = double.Parse(_effectiveStack),
                            BetP1 = _scrapeResult.P1Bet,
                            BetP2 = _scrapeResult.P2Bet,
                            ChipsP1 = _scrapeResult.P1Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P1Chips) && _scrapeResult.P1Chips.Contains("BB") ? _scrapeResult.P1Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            ChipsP2 = _scrapeResult.P2Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P2Chips) && _scrapeResult.P2Chips.Contains("BB") ? _scrapeResult.P2Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            P1Active = _scrapeResult.P1Active,
                            P2Active = _scrapeResult.P2Active
                        }).Data;
                else
                    return _actions2HandedUseCase.ExecuteVsPlayer(
                        new GetActions2HandedRequest
                        {
                            Card0 = _scrapeResult.U0CardFace0,
                            Card1 = _scrapeResult.U0CardFace1,
                            EffectiveStack = double.Parse(_effectiveStack),
                            BetP1 = _scrapeResult.P1Bet,
                            BetP2 = _scrapeResult.P2Bet,
                            ChipsP1 = _scrapeResult.P1Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P1Chips) && _scrapeResult.P1Chips.Contains("BB") ? _scrapeResult.P1Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            ChipsP2 = _scrapeResult.P2Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P2Chips) && _scrapeResult.P2Chips.Contains("BB") ? _scrapeResult.P2Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            P1Active = _scrapeResult.P1Active,
                            P2Active = _scrapeResult.P2Active
                        }).Data;
            }
            catch
            {
                return "Error";
            }

        }
        
        private string GetAction3Handed()
        {
            try
            {

                if (_scrapeResult.P1Bet != 0.5 && _scrapeResult.P1Bet.ToString().Count() == 3)
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]},{_scrapeResult.P1Bet.ToString()[1]}{_scrapeResult.P1Bet.ToString()[2]}");
                else if (_scrapeResult.P1Bet.ToString().Count() > 3)
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]}{_scrapeResult.P1Bet.ToString()[1]},{_scrapeResult.P1Bet.ToString()[2]}{_scrapeResult.P1Bet.ToString()[3]}");

                if (_scrapeResult.P2Bet != 0.5 && _scrapeResult.P2Bet.ToString().Count() == 3)
                    _scrapeResult.P2Bet = double.Parse($"{_scrapeResult.P2Bet.ToString()[0]},{_scrapeResult.P2Bet.ToString()[1]}{_scrapeResult.P2Bet.ToString()[2]}");
                else if (_scrapeResult.P2Bet.ToString().Count() > 3)
                    _scrapeResult.P2Bet = double.Parse($"{_scrapeResult.P2Bet.ToString()[0]}{_scrapeResult.P2Bet.ToString()[1]},{_scrapeResult.P2Bet.ToString()[2]}{_scrapeResult.P2Bet.ToString()[3]}");


                if (_scrapeResult.P0Dealer)
                    return _actions3HandedUseCase.ExecuteButtonAction(
                        new GetActions3HandedRequest
                        {
                            Card0 = _scrapeResult.U0CardFace0,
                            Card1 = _scrapeResult.U0CardFace1,
                            EffectiveStack = double.Parse(_effectiveStack),
                            BetP1 = _scrapeResult.P1Bet,
                            BetP2 = _scrapeResult.P2Bet,
                            ChipsP1 = _scrapeResult.P1Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P1Chips) && _scrapeResult.P1Chips.Contains("BB") ? _scrapeResult.P1Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9").Replace(".", ",") : "0") : 0,
                            ChipsP2 = _scrapeResult.P2Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P2Chips) && _scrapeResult.P2Chips.Contains("BB") ? _scrapeResult.P2Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9").Replace(".", ",") : "0") : 0,
                            P1Active = _scrapeResult.P1Active,
                            P2Active = _scrapeResult.P2Active
                        }).Data;
                else if (_scrapeResult.P1Dealer)
                    return _actions3HandedUseCase.ExecuteBigBlindAction(
                        new GetActions3HandedRequest
                        {
                            Card0 = _scrapeResult.U0CardFace0,
                            Card1 = _scrapeResult.U0CardFace1,
                            EffectiveStack = double.Parse(_effectiveStack),
                            BetP1 = _scrapeResult.P1Bet,
                            BetP2 = _scrapeResult.P2Bet,
                            ChipsP1 = _scrapeResult.P1Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P1Chips) && _scrapeResult.P1Chips.Contains("BB") ? _scrapeResult.P1Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            ChipsP2 = _scrapeResult.P2Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P2Chips) && _scrapeResult.P2Chips.Contains("BB") ? _scrapeResult.P2Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            P1Active = _scrapeResult.P1Active,
                            P2Active = _scrapeResult.P2Active
                        }).Data;
                else if (_scrapeResult.P2Dealer)
                    return _actions3HandedUseCase.ExecuteSmallBlindAction(
                        new GetActions3HandedRequest
                        {
                            Card0 = _scrapeResult.U0CardFace0,
                            Card1 = _scrapeResult.U0CardFace1,
                            EffectiveStack = double.Parse(_effectiveStack),
                            BetP1 = _scrapeResult.P1Bet,
                            BetP2 = _scrapeResult.P2Bet,
                            ChipsP1 = _scrapeResult.P1Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P1Chips) && _scrapeResult.P1Chips.Contains("BB") ? _scrapeResult.P1Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            ChipsP2 = _scrapeResult.P2Active ? double.Parse(!string.IsNullOrWhiteSpace(_scrapeResult.P2Chips) && _scrapeResult.P2Chips.Contains("BB") ? _scrapeResult.P2Chips.Replace("BB", " ").Trim().Replace(" ", ".").Replace("O", "0").Replace("B", "9") : "0") : 0,
                            P1Active = _scrapeResult.P1Active,
                            P2Active = _scrapeResult.P2Active
                        }).Data;
            }
            catch
            {
                return "Error";
            }

            return string.Empty;
        }

        private void SetEmptyValues()
        {
            lbDealer0.Text = string.Empty;
            lbDealer1.Text = string.Empty;
            lbDealer2.Text = string.Empty;

            lbP1Bet.Text = string.Empty;
            lbP2Bet.Text = string.Empty;

            lbP0Chips.Text = "Chips: ";
            lbP1Chips.Text = "Chips: ";
            lbP2Chips.Text = "Chips: ";

            lbAction.Text = string.Empty;
        }

        private string GetEffectiveBB(string p0BB, string p1BB, string p2BB)
        {
            double medio = 0;

            double.TryParse(p0BB.Split(" ")[0].Replace(".", ",").Replace("O", "0"), out double p0chips);
            double.TryParse(p1BB.Split(" ")[0].Replace(".", ",").Replace("O", "0"), out double p1chips);
            double.TryParse(p2BB.Split(" ")[0].Replace(".", ",").Replace("O", "0"), out double p2chips);


            if (!_scrapeResult.P1Active)
                p1chips = 0;
            
            if(!_scrapeResult.P2Active)
                p2chips = 0;

            if (_scrapeResult.P1Active && _scrapeResult.P1Bet > 1 && p1chips <= 1)
                p1chips = _scrapeResult.P1Bet;

            if (_scrapeResult.P2Active && _scrapeResult.P2Bet > 1 && p2chips <= 1)
                p2chips = _scrapeResult.P2Bet;


            if (p0chips < p1chips && p0chips < p2chips)
            {
                medio = p0chips;
            }
            else
            {
                if (p0chips > p1chips && p0chips < p2chips)
                    medio = p0chips;
                else if (p0chips > p1chips && p0chips > p2chips)
                {
                    if (p1chips > p2chips)
                        medio = p1chips;
                    else
                        medio = p2chips;
                }
                else if (p0chips < p1chips && p0chips > p2chips)
                    medio = p0chips;
                else
                    medio = p1chips;
            }

            return medio.ToString();
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

      
        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            _formCreateImage.Show();
            twRegions.ExpandAll();
        }
               

        private void ckColor_CheckedChanged(object sender, EventArgs e)
        {
            if (ckColor.Checked)
            {
                var node = twRegions.Nodes.Find("Nodo0", true).FirstOrDefault() as TreeNode;

                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                var rgbRequest = new GetRGBColorRequest
                {
                    Image = (Bitmap)_formImage.pbImagen.Image,
                    X = _locRegion.X,
                    Y = _locRegion.Y,
                    IsColor = ckColor.Checked                    
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

        private void ckBoard_CheckedChanged(object sender, EventArgs e)
        {
            if (ckBoard.Checked)
            {
                var node = twRegions.Nodes.Find("Nodo0", true).FirstOrDefault() as TreeNode;

                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                if(region != null)
                    region.IsBoard = true;

            }
            else
            {
                var node = twRegions.Nodes.Find("Nodo0", true).FirstOrDefault() as TreeNode;

                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                if (region != null)
                    region.IsBoard = false;
            }
        }

        private void btnCreateFont_Click(object sender, EventArgs e)
        {
            //CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), _umbral)
            List<bool> iHash1 = GetHashFont(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), 110));
        }
        public List<bool> GetHashFont(Bitmap bmpSource)
        {
            var con = 0;
            List<bool> lResult = new List<bool>();
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(_locRegion.Width, _locRegion.Height));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    if (bmpMin.GetPixel(i, j).GetBrightness() < 0.5f)
                        richTextBox1.Text += " ";
                    else
                    {
                        richTextBox1.Text += "x";
                        con++;
                    }

                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
                richTextBox1.Text += "\r\n";
            }
            return lResult;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _locRegion.X,
                Y = _locRegion.Y,
                IsColor = ckColor.Checked
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
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        
    }
}



