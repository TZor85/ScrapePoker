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
using AutoItX3Lib;
using OpenScrape.App.Forms;

namespace OpenScrape.App
{
    public partial class Form1 : Form, IAddRegion
    {
        FormRegions _formRegions;
        FormCreateImage _formCreateImage;
        FormCreateFont _formCreateFont;
        FormImage _formImage;
        FormPlaying _formPlaying;
        Graphics _papel;
        

        List<Regions> _regions = new List<Regions>();
        List<ImageRegion> _images = new List<ImageRegion>();
        List<ImageRegion> _imagesBoard = new List<ImageRegion>();
        List<FontRegion> _fonts = new List<FontRegion>();


        List<KeyValuePair<string, string>> _imageList = new List<KeyValuePair<string, string>>();

        Regions _locRegion = new Regions();
        ImageRegion _locImage = new ImageRegion();

        TableScrapeResult _scrapeResult = new TableScrapeResult();
        BoardPlayerData _boardPlayerData = new BoardPlayerData();

        Image _img = null;

        private int _speed = 1;
        public string _newRegion = string.Empty;
        private string Key = "8UHjPgXZzXCGkhxV2QCnooyJexUzvJrO";

        private List<int> _colorDealer = new List<int> { 250, 251, 252, 253, 254, 255 };
        private List<int> _colorActive = new List<int> {33, 34, 35, 36, 37};
        private List<int> _colorSit = new List<int> { 0, 9, 11, 12 };

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

        private void btnPlay_Click(object sender, EventArgs e)
        {
            _formPlaying = new FormPlaying();

            _formPlaying.Checked = cbTest.Checked;
            _formPlaying.FormImage = _formImage;
            _formPlaying.Regions = _regions;
            _formPlaying.Images = _images;
            _formPlaying.Fonts = _fonts;
            _formPlaying.ImagesBoard= _imagesBoard;


            _formPlaying.Show();
            this.WindowState = FormWindowState.Minimized;
        }

       

        public void Execute(List<FontRegion> fonts)
        {
            var node = twRegions.Nodes.Find("Nodo3", true).FirstOrDefault() as TreeNode;

            _fonts.AddRange(fonts);

            foreach (var item in fonts)
            {
                node.Nodes.Add(item.Name);
            }
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
                    _locImage = new ImageRegion { Name = texto, Image = img, IsBoard = true };
                    _imagesBoard.Add(_locImage);
                    break;
                case "Nodo2":
                    texto = $"{texto} ({_locRegion.Width}x{_locRegion.Height})";
                    img = CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height));// CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), _umbral);
                    _locImage = new ImageRegion { Name = texto, Image = img, IsBoard = false, Value = GetValueImage()};
                    _images.Add(_locImage);
                    break;
                case "Nodo3":
                    _fonts.Add(new FontRegion { Name = texto, Value = GetHashFont(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), 200), true) });
                    break;

                default:
                    break;
            }

            node.Nodes.Add(texto);
            twRegions.ExpandAll();

        }

        private string GetValueImage()
        {
            return GetHashImage(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), 130));
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
                tbX.Text = _locRegion.X.ToString();
                tbY.Text = _locRegion.Y.ToString();
                lbXY.Text = $"X: {_locRegion.X.ToString()} Y:{_locRegion.Y.ToString()}";
            }

        }

 
        private void cbSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            int.TryParse(cbSpeed.Text, out _speed);
        }

        private void btnSaveMap_Click(object sender, EventArgs e)
        {
            _saveUseCase.Execute(new SaveTableMapUseCaseRequest { Regions = _regions, Images = _images, Key = Key, Board = _imagesBoard, Fonts = _fonts });            
        }

        private void btnLoadMap_Click(object sender, EventArgs e)
        {
            var response = _loadUseCase.Execute(Key);

            _regions.Clear();
            _images.Clear();
            _fonts.Clear();
            _imagesBoard.Clear();
            
            _regions.AddRange(response.Regions);
            _images.AddRange(response.Images);
            _imagesBoard.AddRange(response.Board);
            _fonts.AddRange(response.Fonts);

            foreach (var item in response.Tree)
            {
                var node = twRegions.Nodes.Find(item.Key, true).FirstOrDefault() as TreeNode;
                
                if(node != null)
                    node.Nodes.Add(item.Value);

            }

            foreach (var item in _images)
            {
                _imageList.Add(new KeyValuePair<string, string>(item.Name, item.Value));
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

            //AutoItX3 au3 = new AutoItX3();
            //au3.MouseMove(0, 0, 10);


            //TODO: hacer las comparaciones de las imagenes / OCR

            _scrapeResult = new TableScrapeResult();

            var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), _umbral));

            foreach (var item in _regions.Where(x => x.IsHash))
            {
                var maxEqual = 0;
                var max = 0;

                string iHash1 = GetHashImage(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height)), 130));

                foreach (var immg in _images)
                {
                    //List<bool> iHash2 = GetHash(new Bitmap(immg.Image));

                    int equalElements = iHash1.Zip(immg.Value, (i, j) => i == j).Count(eq => eq);

                    if (equalElements > maxEqual)
                        maxEqual = equalElements;

                    if (maxEqual > max && maxEqual >= (1375 * 0.9))
                    {
                        switch (item.Name)
                        {
                            case "u0cardface0":
                                _scrapeResult.U0CardFace0 = immg.Name.Split(" ")[0];
                                _scrapeResult.U0CardForce0 = immg.Force;
                                _scrapeResult.U0CardSuit0 = immg.Suit;
                                max = maxEqual;
                                break;
                            case "u0cardface1":
                                _scrapeResult.U0CardFace1 = immg.Name.Split(" ")[0];
                                _scrapeResult.U0CardForce1 = immg.Force;
                                _scrapeResult.U0CardSuit1 = immg.Suit;
                                max = maxEqual;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            foreach (var item in _regions.Where(x => x.IsColor))
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
                            if (_colorSit.Contains(color.B))
                                _scrapeResult.P1Sit = true;
                            break;
                        case "p2sit":
                            if (_colorSit.Contains(color.B))
                                _scrapeResult.P2Sit = true;
                            break;
                        default:
                            break;
                    }
                }

            }

            foreach (var item in _regions.Where(x => !x.IsColor && !x.IsHash))
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

                        text = text.Replace('.', ',');

                        double.TryParse(text, out double p2Bet);
                        if (p2Bet == 50)
                            p2Bet = 0.5f;
                        _scrapeResult.P2Bet = p2Bet;
                        break;
                    default:
                        break;
                }

            }


            SetEmptyValues();
            SetBoardValues();

            GetActionsResponse getAction = new GetActionsResponse();

            if (_scrapeResult.P1Sit && _scrapeResult.P2Sit)
                getAction = GetAction3Handed().Data;
            else
                getAction = GetAction2Handed().Data;


            lbAction.Text = getAction.Action;
            _boardPlayerData.Aggressor = getAction.Style == Enums.Styles.Agresive;
            _boardPlayerData.InPosition = getAction.Position == Enums.Positions.InPosition;
            _boardPlayerData.PreflopAction = getAction.PreflopAction;

            lbPosition.Text = _boardPlayerData.InPosition ? "IP" : "OOP";


        }

        private void SetBoardValues()
        {
            lbP0Chips.Text = $"Chips: {_scrapeResult.P0Chips}";
            lbP1Chips.Text = $"Chips: {_scrapeResult.P1Chips}";
            lbP2Chips.Text = $"Chips: {_scrapeResult.P2Chips}";

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
        }

        private void btnFlop_Click(object sender, EventArgs e)
        {
            if (!cbTest.Checked)
            {
                GetImageWhilePlaying();
            }
            



            _scrapeResult.B0Card1 = string.Empty;
            _scrapeResult.B0Card2 = string.Empty;
            _scrapeResult.B0Card3 = string.Empty;

            _scrapeResult.B0Card5 = string.Empty;
            _scrapeResult.B0CardForce1 = 0;
            _scrapeResult.B0CardForce2 = 0;
            _scrapeResult.B0CardForce3 = 0;

            _scrapeResult.B0CardForce5 = 0;

            foreach (var item in _regions.Where(x => x.IsBoard))
            {
                var maxEqual = 0;
                var max = 0;


                string iHash1 = GetHashImage(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height)), 130));

                foreach (var immg in _images)
                {                    
                    int equalElements = iHash1.Zip(immg.Value, (i, j) => i == j).Count(eq => eq);

                    if (equalElements > maxEqual)
                        maxEqual = equalElements;

                    if (maxEqual > max && maxEqual >= (1375 * 0.9))
                    {
                        switch (item.Name)
                        {
                            case "b0card1":
                                _scrapeResult.B0Card1 = immg.Name.Split(" ")[0];
                                _scrapeResult.B0CardForce1 = immg.Force;
                                _scrapeResult.B0CardSuit1 = immg.Suit;
                                max = maxEqual;
                                break;
                            case "b0card2":
                                _scrapeResult.B0Card2 = immg.Name.Split(" ")[0];
                                _scrapeResult.B0CardForce2 = immg.Force;
                                _scrapeResult.B0CardSuit2 = immg.Suit;
                                max = maxEqual;
                                break;
                            case "b0card3":
                                _scrapeResult.B0Card3 = immg.Name.Split(" ")[0];
                                _scrapeResult.B0CardForce3 = immg.Force;
                                _scrapeResult.B0CardSuit3 = immg.Suit;
                                max = maxEqual;
                                break;
                            default:
                                break;
                        }
                    }
                }

            }

            var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), _umbral));

            foreach (var item in _regions.Where(x => !x.IsColor && !x.IsHash))
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

                        text = text.Replace('.', ',');

                        double.TryParse(text, out double p2Bet);
                        if (p2Bet == 50)
                            p2Bet = 0.5f;
                        _scrapeResult.P2Bet = p2Bet;
                        break;
                    default:
                        break;
                }

            }


            SetBoardValues();


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

            Dictionary<string, bool> diccionario = new Dictionary<string, bool>();
            List<Dictionary<string, bool>> listaDiccionarios = new List<Dictionary<string, bool>>();


            List<int> forceCards = new List<int>();
            List<int> suitCards = new List<int>();

            //FLOP
            if(_scrapeResult.B0CardForce4 == 0 && _scrapeResult.B0CardForce5 == 0)
            {
                if (_scrapeResult.B0CardForce1 > 0)
                {
                    forceCards.Add(_scrapeResult.B0CardForce1);
                    suitCards.Add(_scrapeResult.B0CardSuit1);
                }

                if (_scrapeResult.B0CardForce2 > 0)
                {
                    forceCards.Add(_scrapeResult.B0CardForce2);
                    suitCards.Add(_scrapeResult.B0CardSuit2);
                }                    

                if (_scrapeResult.B0CardForce3 > 0)
                {
                    forceCards.Add(_scrapeResult.B0CardForce3);
                    suitCards.Add(_scrapeResult.B0CardSuit3);
                }


                SetJugadaFlop(forceCards, suitCards);

            }
            
            //Pasar a sus métodos
            //TURN
            //if(_scrapeResult.B0CardForce5 == 0)
            //{
            //    if (_scrapeResult.B0CardForce4 > 0)
            //        forceCards.Add(_scrapeResult.B0CardForce4);

            //    SetJugadaTurn(forceCards);

            //}//RIVER
            //else
            //{
            //    if (_scrapeResult.B0CardForce5 > 0)
            //        forceCards.Add(_scrapeResult.B0CardForce5);
            //}



            //if (_scrapeResult.U0CardForce0 == _scrapeResult.U0CardForce1) ;
              
        }

        private void btnTurn_Click(object sender, EventArgs e)
        {
            _scrapeResult.B0Card4 = string.Empty;
            _scrapeResult.B0CardForce4 = 0;

            foreach (var item in _regions.Where(x => x.Name == "b0card4"))
            {
                var maxEqual = 0;
                var max = 0;

                string iHash1 = GetHashImage(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height)), 130));

                foreach (var immg in _images)
                {                   
                    //List<bool> iHash2 = GetHash(new Bitmap(immg.Image));

                    int equalElements = iHash1.Zip(immg.Value, (i, j) => i == j).Count(eq => eq);

                    if (equalElements > maxEqual)
                        maxEqual = equalElements;

                    if (maxEqual > max && maxEqual >= (1375 * 0.9))
                    {
                        _scrapeResult.B0Card4 = immg.Name.Split(" ")[0];
                        _scrapeResult.B0CardForce4 = immg.Force;
                        _scrapeResult.B0CardSuit4 = immg.Suit;
                        max = maxEqual;
                    }
                }

            }

            if (!string.IsNullOrWhiteSpace(_scrapeResult.B0Card4))
                pbTurn.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.B0Card4))!.Image;
            else
                pbTurn.Image = null;
        }

        private void btnRiver_Click(object sender, EventArgs e)
        {
            _scrapeResult.B0Card5 = string.Empty;
            _scrapeResult.B0CardForce5 = 0;

            foreach (var item in _regions.Where(x => x.Name == "b0card5"))
            {
                var maxEqual = 0;
                var max = 0;

                string iHash1 = GetHashImage(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height)), 130));

                foreach (var immg in _images)
                {                   
                    //List<bool> iHash2 = GetHash(new Bitmap(immg.Image));

                    int equalElements = iHash1.Zip(immg.Value, (i, j) => i == j).Count(eq => eq);

                    if (equalElements > maxEqual)
                        maxEqual = equalElements;

                    if (maxEqual > max && maxEqual >= (1375 * 0.9))
                    {
                        _scrapeResult.B0Card5 = immg.Name.Split(" ")[0];
                        _scrapeResult.B0CardForce5 = immg.Force;
                        _scrapeResult.B0CardSuit5 = immg.Suit;
                        max = maxEqual;
                    }
                }

            }

            if (!string.IsNullOrWhiteSpace(_scrapeResult.B0Card5))
                pbRiver.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.B0Card5))!.Image;
            else
                pbRiver.Image = null;
        }

        private void SetJugadaTurn(List<int> forceCards)
        {
            var distintcsForceCards = forceCards.Distinct().Count();

            try
            {
                if (distintcsForceCards == 3)
                {
                    //int distanciaMaxima = forceCards.SelectMany((x, i) => forceCards.Skip(i + 1), (x, y) => Math.Abs(x - y)).Max();

                    forceCards.Add(_scrapeResult.U0CardForce0);
                    forceCards.Add(_scrapeResult.U0CardForce1);

                    var pairsFlop = forceCards.GroupBy(x => x)
                            .SelectMany(g => g.Skip(1))
                            .Distinct()
                            .ToList();

                    if(pairsFlop.Count() > 0)
                    {
                        lbPair.Text = $"Pareja de {_images.FirstOrDefault(x => x.Force == pairsFlop.First())!.Name.Split(" ")[0].Substring(0, 1)}";
                    }

                 //TODO: Mirar la carta alta si entra aquí y no hay pareja con ninguna de nuestra mano
                    
                }
                else if (distintcsForceCards == 2)
                {
                    var value = forceCards.GroupBy(x => x)
                            .SelectMany(g => g.Skip(1))
                            .Distinct()
                            .ToList();

                    var parejas = $"Pareja de {_images.FirstOrDefault(x => x.Force == value.First())!.Name.Split(" ")[0].Substring(0, 1)} y {_images.FirstOrDefault(x => x.Force == value.Last())!.Name.Split(" ")[0].Substring(0, 1)}";

                    lbPair.Text = parejas;
                }
                else if (distintcsForceCards == 1)
                {
                    lbPair.Text = $"Póker de {_images.FirstOrDefault(x => x.Force == forceCards.First())!.Name.Split(" ")[0].Substring(0, 1)}";
                }
                else
                    lbPair.Text = $"Carta alta {_images.FirstOrDefault(x => x.Force == forceCards.Max())!.Name.Split(" ")[0].Substring(0, 1)}";
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        private void SetJugadaFlop(List<int> forceCards, List<int> suitCards)
        {
            var distintcsCards = forceCards.Distinct().Count();
            var posibleEscalera = false;

            var existeAs = forceCards.Any(x => x == 14);
            
            forceCards.Sort();

            if (forceCards[1] - forceCards[0] <= 3 && forceCards[2] - forceCards[0] <= 4)
            {
                posibleEscalera = true;
            }

            if(existeAs)
            {
                _boardPlayerData.Jugada = Enums.JugadasEnum.AsOnTable;
                List<int> auxForceCards = new List<int>();
                auxForceCards.AddRange(forceCards);
                auxForceCards.Remove(14);
                auxForceCards.Add(1);

                auxForceCards.Sort();

                if (auxForceCards[1] - auxForceCards[0] <= 3 && auxForceCards[2] - auxForceCards[0] <= 4)
                {
                    posibleEscalera = true;
                }



            }

            var distintcSuits = suitCards.Distinct().Count();

            if(distintcSuits == 1 || distintcsCards <= 2  || (posibleEscalera && distintcsCards == 3) || forceCards.Intersect(new List<int> {14, 2, 3 }).Count() == 3)
            {
                //Coordinado
                lbBoard.Text = "Board Coordinado";
                _boardPlayerData.BoardCoordinado = true;
            }
            else
            {
                //Seco
                lbBoard.Text = "Board Seco";
                _boardPlayerData.BoardCoordinado = false;
            }


            //Pareja en mesa
            if (distintcsCards == 2)
            {                
                var pairOnTable = forceCards.GroupBy(x => x)
                            .SelectMany(g => g.Skip(1))
                            .Distinct()
                            .ToList();

                var cardNoPaired = forceCards.GroupBy(x => x).FirstOrDefault(g => g.Count() == 1)!.FirstOrDefault();

                foreach (var item in pairOnTable)
                {
                    if(item == _scrapeResult.U0CardForce0 || item == _scrapeResult.U0CardForce1)
                    {
                        //Tendríamos un trío con 1 de las cartas de la mano
                        lbPair.Text = $"Trucha de {_images.FirstOrDefault(x => x.Force == pairOnTable.First())!.Name.Split(" ")[0].Substring(0, 1)}";
                        _boardPlayerData.Jugada = Enums.JugadasEnum.TopPairOrPlus;
                    }                    

                    //Dobles parejas, una en la mesa y otra con una de nuestra mano
                    if(cardNoPaired == _scrapeResult.U0CardForce0 || cardNoPaired == _scrapeResult.U0CardForce1)
                    {
                        if(cardNoPaired > item)
                        {
                            _boardPlayerData.Jugada = Enums.JugadasEnum.TopPairOrPlus;
                        }
                        else
                        {
                            _boardPlayerData.Jugada = Enums.JugadasEnum.MiddlePair;
                        }

                        lbPair.Text = $"Doble pareja {_images.FirstOrDefault(x => x.Force == pairOnTable.First())!.Name.Split(" ")[0].Substring(0, 1)} y {_images.FirstOrDefault(x => x.Force == cardNoPaired)!.Name.Split(" ")[0].Substring(0, 1)}";
                        
                    }

                }                           


                if (string.IsNullOrWhiteSpace(lbPair.Text))
                {
                    lbPair.Text = $"Pareja de {_images.FirstOrDefault(x => x.Force == pairOnTable.First())!.Name.Split(" ")[0].Substring(0, 1)}";
                    _boardPlayerData.Jugada = Enums.JugadasEnum.PairOnTable;
                }


            }
            else if (distintcsCards == 1)
            {
                //Añadimos nuestras cartas a la lista para comprobar si hemos conectado
                forceCards.Add(_scrapeResult.U0CardForce0);
                forceCards.Add(_scrapeResult.U0CardForce1);

                var cartasDistintas = forceCards.Distinct().Count();

                if(cartasDistintas == 2)
                {
                    var groups = forceCards.GroupBy(x => x);
                    int repetidos = groups.Where(g => g.Count() > 1).Count();

                    if(repetidos == 3)
                    {
                        //Full
                        _boardPlayerData.Jugada = Enums.JugadasEnum.TopPairOrPlus;
                        lbPair.Text = "Full";
                    }
                    else
                    {
                        //poker
                        _boardPlayerData.Jugada = Enums.JugadasEnum.TopPairOrPlus;
                        lbPair.Text = "Póker";
                    }
                }
                else 
                {
                    //trio en mesa
                    lbPair.Text = "Trio en mesa";
                }
                                
            }                
            else
            {
                var maxValueFlop = forceCards.Max();
                var minValueFlop = forceCards.Min();

                //Añadimos nuestras cartas a la lista para comprobar si hemos conectado
                forceCards.Add(_scrapeResult.U0CardForce0);
                forceCards.Add(_scrapeResult.U0CardForce1);

                var cartasDistintas = forceCards.Distinct().Count();

                if(cartasDistintas == 3)
                {
                    if(_scrapeResult.U0CardForce0 == _scrapeResult.U0CardForce1)
                    {
                        lbPair.Text = "Trío";
                        _boardPlayerData.Jugada = Enums.JugadasEnum.TopPairOrPlus;
                    }
                    else
                    {
                        lbPair.Text = "Doble pareja";
                        _boardPlayerData.Jugada = Enums.JugadasEnum.TopPairOrPlus;
                    }
                    
                }
                else if(cartasDistintas == 4)
                {
                    var pairValue = forceCards.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
                    var kicker = 0;

                    if (_scrapeResult.U0CardForce0 == pairValue)
                        kicker = _scrapeResult.U0CardForce1;
                    else
                        kicker = _scrapeResult.U0CardForce0;

                    if(pairValue == maxValueFlop)
                    {
                        _boardPlayerData.Jugada = Enums.JugadasEnum.TopPairOrPlus;
                        lbPair.Text = "Top Pair";
                    }
                    else if(pairValue == minValueFlop)
                    {
                        _boardPlayerData.Jugada = Enums.JugadasEnum.BottonPair;
                        lbPair.Text = "Botton Pair";
                    }
                    else
                    {
                        _boardPlayerData.Jugada = Enums.JugadasEnum.MiddlePair;
                        lbPair.Text = "Middle Pair";
                    }
                    
                }
                else
                {
                    lbPair.Text = $"Carta alta {_images.FirstOrDefault(x => x.Force == forceCards.Max())!.Name.Split(" ")[0].Substring(0, 1)}";
                    _boardPlayerData.Jugada = Enums.JugadasEnum.Nothing;
                }
                                
            }

            if (_boardPlayerData.Aggressor)
            {
                if (_boardPlayerData.BoardCoordinado)
                {
                    if (_boardPlayerData.InPosition)
                    {
                        switch (_boardPlayerData.Jugada)
                        {
                            case Enums.JugadasEnum.TopPairOrPlus:
                            case Enums.JugadasEnum.BottonPair:
                                lbAction.Text = "BET 3/4";
                                break;
                            case Enums.JugadasEnum.MiddlePair:
                                lbAction.Text = "CHECK";
                                break;
                            case Enums.JugadasEnum.PairOnTable:
                            case Enums.JugadasEnum.AsOnTable:
                                lbAction.Text = "C BET";
                                break;
                            default:
                                break;
                        }

                    }
                    else //Sin posicion
                    {
                        if(_scrapeResult.P1Bet > 0 || _scrapeResult.P2Bet > 0)
                        {
                            lbAction.Text = "CALL";
                        }
                        else
                        {
                            lbAction.Text = "BET 3/4";
                        }
                    }
                }
                else //Board Seco
                {
                    if(_boardPlayerData.InPosition)
                    {
                        switch (_boardPlayerData.Jugada)
                        {
                            case Enums.JugadasEnum.TopPairOrPlus:
                            case Enums.JugadasEnum.BottonPair:
                                lbAction.Text = "BET 1/2";
                                break;
                            case Enums.JugadasEnum.MiddlePair:
                                lbAction.Text = "CHECK";
                                break;
                            case Enums.JugadasEnum.PairOnTable:
                            case Enums.JugadasEnum.AsOnTable:
                                lbAction.Text = "C BET";
                                break;
                            default:
                                break;
                        }
                    }
                    else //Sin posicion
                    {
                        if (_scrapeResult.P1Bet > 0 || _scrapeResult.P2Bet > 0)
                        {
                            lbAction.Text = "CALL";
                        }
                        else
                        {
                            lbAction.Text = "BET 1/2";
                        }
                    }
                }
            }
            else //Sin iniciativa
            {
                if (_boardPlayerData.BoardCoordinado)
                {
                    if (_boardPlayerData.InPosition)
                    {
                        switch (_boardPlayerData.Jugada)
                        {
                            case Enums.JugadasEnum.TopPairOrPlus:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.MiddlePair:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.BottonPair:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.Nothing:
                                lbAction.Text = "CHECK/PRO?";
                                break;
                            default:
                                lbAction.Text = "CHECK/PRO?";
                                break;
                        }
                    }
                    else //Sin posicion
                    {

                        //Seguir por aquí
                        switch (_boardPlayerData.Jugada)
                        {
                            case Enums.JugadasEnum.TopPairOrPlus:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.MiddlePair:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.BottonPair:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.Nothing:
                                lbAction.Text = "CHECK/PRO?";
                                break;
                            default:
                                lbAction.Text = "CHECK/PRO?";
                                break;
                        }

                    }
                }
                else //Board Seco
                {
                    if (_boardPlayerData.InPosition)
                    {
                        switch (_boardPlayerData.Jugada)
                        {
                            case Enums.JugadasEnum.TopPairOrPlus:
                                switch (_boardPlayerData.PreflopAction)
                                {
                                    case Enums.PreflopAction.LimpedPot:
                                        if (_boardPlayerData.KickerValue >= 8)
                                        {
                                            lbAction.Text = "CHECK/RAISE";
                                        }
                                        else
                                        {
                                            lbAction.Text = "CHECK/CALL";
                                        }
                                        break;
                                    case Enums.PreflopAction.RaisedPot:
                                        if (_boardPlayerData.KickerValue >= 10)
                                        {
                                            lbAction.Text = "CHECK/RAISE";
                                        }
                                        else
                                        {
                                            lbAction.Text = "CHECK/CALL";
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case Enums.JugadasEnum.MiddlePair:
                            case Enums.JugadasEnum.BottonPair:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.Nothing:
                                lbAction.Text = "CHECK/FOLD?";
                                break;
                            default:
                                break;
                        }
                    }                    
                    else //Sin posicion
                    {
                        switch (_boardPlayerData.Jugada)
                        {
                            case Enums.JugadasEnum.TopPairOrPlus:
                                switch (_boardPlayerData.PreflopAction)
                                {
                                    case Enums.PreflopAction.LimpedPot:
                                        if (_boardPlayerData.KickerValue >= 8)
                                        {
                                            lbAction.Text = "CHECK/RAISE";
                                        }
                                        else
                                        {
                                            lbAction.Text = "CHECK/CALL";
                                        }
                                        break;
                                    case Enums.PreflopAction.RaisedPot:
                                        if (_boardPlayerData.KickerValue >= 10)
                                        {
                                            lbAction.Text = "CHECK/RAISE";
                                        }
                                        else
                                        {
                                            lbAction.Text = "CHECK/CALL";
                                        }
                                        break;
                                    default:
                                        lbAction.Text = "CHECK/FOLD";
                                        break;
                                }
                                break;
                            case Enums.JugadasEnum.MiddlePair:
                            case Enums.JugadasEnum.BottonPair:
                                lbAction.Text = "CHECK/CALL";
                                break;
                            case Enums.JugadasEnum.Nothing:
                                lbAction.Text = "CHECK/FOLD?";
                                break;
                            default:
                                lbAction.Text = "CHECK/FOLD";
                                break;
                        }
                    }
                }
            }

            

                
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

            Thread.Sleep(100);
        }



        private GetActions2HandedResponse GetAction2Handed()
        {
            try
            {
                if (_scrapeResult.P1Bet != 0.5 && _scrapeResult.P1Bet.ToString().Count() == 3 && !_scrapeResult.P1Bet.ToString().Contains(',') && !_scrapeResult.P1Bet.ToString().Contains('.'))
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]},{_scrapeResult.P1Bet.ToString()[1]}{_scrapeResult.P1Bet.ToString()[2]}");
                else if (_scrapeResult.P1Bet.ToString().Count() > 3 && !_scrapeResult.P1Bet.ToString().Contains(',') && !_scrapeResult.P1Bet.ToString().Contains('.'))
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]}{_scrapeResult.P1Bet.ToString()[1]},{_scrapeResult.P1Bet.ToString()[2]}{_scrapeResult.P1Bet.ToString()[3]}");

                if (_scrapeResult.P2Bet != 0.5 && _scrapeResult.P2Bet.ToString().Count() == 3 && !_scrapeResult.P2Bet.ToString().Contains(',') && !_scrapeResult.P2Bet.ToString().Contains('.'))
                    _scrapeResult.P2Bet = double.Parse($"{_scrapeResult.P2Bet.ToString()[0]},{_scrapeResult.P2Bet.ToString()[1]}{_scrapeResult.P2Bet.ToString()[2]}");
                else if (_scrapeResult.P2Bet.ToString().Count() > 3 && !_scrapeResult.P2Bet.ToString().Contains(',') && !_scrapeResult.P2Bet.ToString().Contains('.'))
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
                        });
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
                        });
            }
            catch
            {
                return new GetActions2HandedResponse { Data = new GetActionsResponse { Action = "Error"} };
            }

        }

        private GetActions3HandedResponse GetAction3Handed()
        {
            try
            {
                var response = new GetActions3HandedResponse();

                if (_scrapeResult.P1Bet != 0.5 && _scrapeResult.P1Bet.ToString().Count() == 3 && !_scrapeResult.P1Bet.ToString().Contains(',') && !_scrapeResult.P1Bet.ToString().Contains('.'))
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]},{_scrapeResult.P1Bet.ToString()[1]}{_scrapeResult.P1Bet.ToString()[2]}");
                else if (_scrapeResult.P1Bet.ToString().Count() > 3 && !_scrapeResult.P1Bet.ToString().Contains(',') && !_scrapeResult.P1Bet.ToString().Contains('.'))
                    _scrapeResult.P1Bet = double.Parse($"{_scrapeResult.P1Bet.ToString()[0]}{_scrapeResult.P1Bet.ToString()[1]},{_scrapeResult.P1Bet.ToString()[2]}{_scrapeResult.P1Bet.ToString()[3]}");

                if (_scrapeResult.P2Bet != 0.5 && _scrapeResult.P2Bet.ToString().Count() == 3 && !_scrapeResult.P2Bet.ToString().Contains(',') && !_scrapeResult.P2Bet.ToString().Contains('.'))
                    _scrapeResult.P2Bet = double.Parse($"{_scrapeResult.P2Bet.ToString()[0]},{_scrapeResult.P2Bet.ToString()[1]}{_scrapeResult.P2Bet.ToString()[2]}");
                else if (_scrapeResult.P2Bet.ToString().Count() > 3 && !_scrapeResult.P2Bet.ToString().Contains(',') && !_scrapeResult.P2Bet.ToString().Contains('.'))
                    _scrapeResult.P2Bet = double.Parse($"{_scrapeResult.P2Bet.ToString()[0]}{_scrapeResult.P2Bet.ToString()[1]},{_scrapeResult.P2Bet.ToString()[2]}{_scrapeResult.P2Bet.ToString()[3]}");


                if (_scrapeResult.P0Dealer)
                {
                    response = _actions3HandedUseCase.ExecuteButtonAction(
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
                        });

                    return response;
                }
                else if (_scrapeResult.P1Dealer)
                {
                    response = _actions3HandedUseCase.ExecuteBigBlindAction(
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
                        });

                    return response;
                }
                else if (_scrapeResult.P2Dealer)
                {
                    response = _actions3HandedUseCase.ExecuteSmallBlindAction(
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
                        });

                    return response;
                }
            }
            catch
            {
                return new GetActions3HandedResponse();
            }

            return new GetActions3HandedResponse();
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

            _scrapeResult.B0Card1 = string.Empty;
            _scrapeResult.B0Card2 = string.Empty;
            _scrapeResult.B0Card3 = string.Empty;
            _scrapeResult.B0Card4 = string.Empty;
            _scrapeResult.B0Card5 = string.Empty;
            _scrapeResult.B0CardForce1 = 0;
            _scrapeResult.B0CardForce2 = 0;
            _scrapeResult.B0CardForce3 = 0;
            _scrapeResult.B0CardForce4 = 0;
            _scrapeResult.B0CardForce5 = 0;

            pbFlop1.Image = null;
            pbFlop2.Image = null;
            pbFlop3.Image = null;
            pbTurn.Image = null;
            pbRiver.Image = null;

            lbPair.Text = string.Empty;
            lbBoard.Text = "Board ";
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
            //_useCase.GetWindow();
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
            string iHash1 = GetHashFont(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), 200));
            //pbImageRegion.Image = CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), 100);
            var hashCount = iHash1.Length;
            var hash = string.Empty;
            var texto = string.Empty;
           
            bool firstOne = false;
            List<KeyValuePair<string, string>> locFonts = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> fonts = new List<KeyValuePair<string, string>>();

            List<FontRegion> locFontsRegion = new List<FontRegion>();

            for (int i = 0; i < hashCount / 24; i++)
            {
                if (!iHash1.Substring(0, 24).Contains('1'))
                {
                    iHash1 = iHash1.Substring(24);

                    if (firstOne)
                    {
                        var maxEqual = 0;
                        var max = 0;
                        bool exist = false;

                        if(_fonts.Count <= 0)
                        {
                            locFontsRegion.Add(new FontRegion { Name = "?", Value = hash });
                            //locFonts.Add(new KeyValuePair<string, string>("?", hash));
                        }
                        else
                        {
                            foreach (var item in _fonts)
                            {
                                //int equalElements = item.Value.Zip(hash, (i, j) => i == j).Count(eq => eq);

                                //if (equalElements > maxEqual)
                                //    maxEqual = equalElements;

                                //if (equalElements >= (hash.Length * 0.9))
                                //{
                                //    exist = true;
                                //    break;                                   

                                //}

                                if (hash.IndexOf(item.Value) == 0)
                                {
                                    exist = true;
                                    break;
                                }
                            }

                            if(!exist)
                                locFontsRegion.Add(new FontRegion { Name = "?", Value = hash });
                        }

                        hash = string.Empty;
                        firstOne = false;

                    }
                }
                else
                {
                    hash += iHash1.Substring(0, 24);
                    iHash1 = iHash1.Substring(24);
                    firstOne = true;
                }
            }

            if (locFontsRegion.Count > 0)
            {
                _formCreateFont = new FormCreateFont();
                _formCreateFont._fonts = locFontsRegion;
                _formCreateFont.region = this;
                _formCreateFont.Show();
            }

        }

        public string GetHashFontTest(Bitmap bmpSource)
        {
            List<bool> lResult = new List<bool>();
            var textHash = string.Empty;
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(_locRegion.Width, _locRegion.Height));
            for (int j = 0; j < bmpMin.Width; j++)
            {
                for (int i = 0; i < bmpMin.Height; i++)
                {
                    var brillo = bmpMin.GetPixel(j, i).GetBrightness();
                    if (bmpMin.GetPixel(j, i).GetBrightness() < 0.99f)
                    {
                        //bmpMin.SetPixel(j, i, bl)
                        textHash += "0";
                    }
                    else
                    {
                        textHash += "1";
                    }
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(j, i).GetBrightness() < 0.5f);
                }
                textHash += "\r\n";
            }

            //foreach (var item in lResult)
            //{
            //    if (item)
            //        textHash += " ";
            //    else
            //        textHash += "x";
            //}

            return textHash;
        }

        public string GetHashFont(Bitmap bmpSource, bool substring = false)
        {
            List<bool> lResult = new List<bool>();
            var textHash = string.Empty;
            var texto = string.Empty;
            
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(_locRegion.Width, _locRegion.Height));
            for (int j = 0; j < bmpMin.Width; j++)
            {
                for (int i = 0; i < bmpMin.Height; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(j, i).GetBrightness() < 0.99f);
                }
            }
                      
            foreach (var item in lResult)
            {
                if (item)
                    textHash += "0";
                else
                    textHash += "1";
            }

            if (substring)
            {
                for (int i = 0; i < lResult.Count / 24; i++)
                {
                    if (!textHash.Substring(0, 24).Contains('1'))
                    {
                        textHash = textHash.Substring(24);
                    }
                    else
                    {
                        texto += textHash.Substring(0, 24);
                        textHash = textHash.Substring(24);
                    }
                }
            }
            else
            {
                texto = textHash;
            }

            return texto;
        }


        public string GetHashImage(Bitmap bmpSource)
        {
            List<bool> lResult = new List<bool>();
            var textHash = string.Empty;
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(25, 55));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }

            foreach (var item in lResult)
            {
                if (item)
                    textHash += "1";
                else
                    textHash += "0";
            }

            return textHash;
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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
            tbY.Text = _locRegion.Y.ToString();
            tbX.Text = _locRegion.X.ToString();
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

        private void tbWidth_Leave(object sender, EventArgs e)
        {            
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.Width = int.Parse(tbWidth.Text);
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;
        }

        private void tbHeight_Leave(object sender, EventArgs e)
        {   
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.Width = int.Parse(tbWidth.Text);
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;
            
        }

        private void tbX_Leave(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.X = int.Parse(tbX.Text);
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
        }

        private void tbY_Leave(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            _locRegion.Y = int.Parse(tbY.Text);
            _papel.DrawRectangle(lapiz, _locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height);

            _img = _formImage.pbImagen.Image;
            lbXY.Text = $"X: {_locRegion.X} Y:{_locRegion.Y}";
        }

        
    }
}



