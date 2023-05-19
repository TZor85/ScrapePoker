using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using OpenScrape.App.Models;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using Tesseract;
using Image = System.Drawing.Image;

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
        private List<int> _colorActive = new List<int> { 33, 34, 35, 36, 37 };
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
            _formPlaying.ImagesBoard = _imagesBoard;


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
                    _locImage = new ImageRegion { Name = texto, Image = img, IsBoard = false, Value = GetValueImage() };
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

                if (node != null)
                    node.Nodes.Add(item.Value);

            }

            foreach (var item in _images)
            {
                _imageList.Add(new KeyValuePair<string, string>(item.Name, item.Value));
            }

        }

        public Bitmap CropImage(Bitmap source, Rectangle section)
        {
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            Graphics g = Graphics.FromImage(bmp);

            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);

            return bmp;
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

            if (!cbTest.Checked)
            {
                GetImageWhilePlaying();
            }

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


            GetActionsResponse getAction = new GetActionsResponse();

            if (_scrapeResult.P1Sit && _scrapeResult.P2Sit)
                getAction = GetAction3Handed().Data;
            else
                getAction = GetAction2Handed().Data;


            _boardPlayerData.Aggressor = getAction.Style == Enums.Styles.Agresive;
            _boardPlayerData.InPosition = getAction.Position == Enums.Positions.InPosition;
            _boardPlayerData.PreflopAction = getAction.PreflopAction;



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
                return new GetActions2HandedResponse { Data = new GetActionsResponse { Action = "Error" } };
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

                if (region != null)
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

                        if (_fonts.Count <= 0)
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

                            if (!exist)
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



