using MediatR;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Enums;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using OpenScrape.App.Models;
using Tesseract;
using Image = System.Drawing.Image;

namespace OpenScrape.App
{
    public partial class Form1 : Form, IAddRegion
    {
        #region Forms
        FormRegions _formRegions;
        FormCreateImage _formCreateImage;
        FormCreateFont _formCreateFont;
        FormImage _formImage;
        FormPlaying _formPlaying;
        Graphics _papel;
        #endregion

        #region Regions

        List<Regions> _regions = new List<Regions>();
        List<ImageRegion> _images = new List<ImageRegion>();
        List<ImageRegion> _imagesBoard = new List<ImageRegion>();
        List<FontRegion> _fonts = new List<FontRegion>();
        Regions _locRegion = new Regions();
        ImageRegion _locImage = new ImageRegion();

        #endregion

        Image _img = null;

        List<KeyValuePair<string, string>> _imageList = new List<KeyValuePair<string, string>>();

        TableScrapeResult _scrapeResult = new TableScrapeResult();


        private int _speed = 1;
        public string _newRegion = string.Empty;
        private string Key = "8UHjPgXZzXCGkhxV2QCnooyJexUzvJrO";

        private List<int> _colorDealer = new List<int> { 250, 251, 252, 253, 254, 255 };
        private List<int> _colorActive = new List<int> { 33, 34, 35, 36, 37 };
        private List<int> _colorSit = new List<int> { 0, 9, 11, 12 };

        private int _umbral = 100;

        private readonly GetWindowsScreenUseCase _useCase = new GetWindowsScreenUseCase();


        private readonly IGetOpenRaiseUseCase _openRaiseUseCase = new GetOpenRaiseUseCase();
        private readonly IGetRaiseOverLimperUseCase _raiseOverLimperUseCase = new GetRaiseOverLimperUseCase();
        private readonly IGet3BetUseCase _threeBetUseCase = new Get3BetUseCase();



        private readonly ISaveTableMapUseCase _saveUseCase = new SaveTableMapUseCase();
        private readonly ILoadTableMapUseCase _loadUseCase = new LoadTableMapUseCase();
        private readonly IMediator _mediator;

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
                    _locImage = new ImageRegion { Name = texto, Image = img, IsBoard = false, Value = GetValueImage(), Force = GetForceHand(texto), Suit = GetSuitHand(texto) };
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

        private static int GetSuitHand(string texto)
        {
            var suit = texto[1] switch
            {
                'c' => 1,
                'h' => 2,
                'd' => 3,
                's' => 4,
                _ => 0
            };

            return suit;
        }

        private static int GetForceHand(string texto)
        {
            var force = texto[0] switch
            {
                'A' => 14,
                'K' => 13,
                'Q' => 12,
                'J' => 11,
                'T' => 10,
                '9' => 9,
                '8' => 8,
                '7' => 7,
                '6' => 6,
                '5' => 5,
                '4' => 4,
                '3' => 3,
                '2' => 2,
                _ => 0
            };

            return force;
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

            foreach (var item in _regions.Where(x => x.IsHash))
            {
                var maxEqual = 0;
                var max = 0;

                string iHash1 = GetHashImage(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height)), 130));

                foreach (var image in _images)
                {
                    int equalElements = iHash1.Zip(image.Value, (i, j) => i == j).Count(eq => eq);

                    if (equalElements > maxEqual)
                        maxEqual = equalElements;

                    if (maxEqual > max && maxEqual >= (700 * 0.9))
                    {
                        switch (item.Name)
                        {
                            case "u0cardface0":
                                _scrapeResult.U0CardFace0 = image.Name.Split(" ")[0];
                                _scrapeResult.U0CardForce0 = image.Force;
                                _scrapeResult.U0CardSuit0 = image.Suit;
                                max = maxEqual;
                                break;
                            case "u0cardface1":
                                _scrapeResult.U0CardFace1 = image.Name.Split(" ")[0];
                                _scrapeResult.U0CardForce1 = image.Force;
                                _scrapeResult.U0CardSuit1 = image.Suit;
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
                if (_formImage.pbImagen.Image == null)
                    continue;

                Color color = new Bitmap(_formImage.pbImagen.Image).GetPixel(item.X, item.Y);
                var rgbColor = color.Name.Substring(2, 6);

                if (_colorDealer.Contains(color.R))
                {
                    switch (item.Name)
                    {
                        case "p0dealer":
                            _scrapeResult.P0Dealer = true;
                            _scrapeResult.P0Position = HeroPosition.Button;
                            break;
                        case "p1dealer":
                            _scrapeResult.P1Dealer = true;
                            _scrapeResult.P0Position = HeroPosition.CutOff;
                            break;
                        case "p2dealer":
                            _scrapeResult.P2Dealer = true;
                            _scrapeResult.P0Position = HeroPosition.MiddlePosition;
                            break;
                        case "p3dealer":
                            _scrapeResult.P3Dealer = true;
                            _scrapeResult.P0Position = HeroPosition.EarlyPosition;
                            break;
                        case "p4dealer":
                            _scrapeResult.P4Dealer = true;
                            _scrapeResult.P0Position = HeroPosition.BigBlind;
                            break;
                        case "p5dealer":
                            _scrapeResult.P5Dealer = true;
                            _scrapeResult.P0Position = HeroPosition.SmallBlind;
                            break;
                    }
                }
            }

            var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), _umbral));

            foreach (var item in _regions.Where(x => !x.IsColor && !x.IsHash))
            {
                var ocrengine = new TesseractEngine(@".\tessdata\", "eng", EngineMode.Default);
                Rect area = new Rect(item.X, item.Y, item.Width, item.Height);

                var res = ocrengine.Process(img, area, PageSegMode.Auto);

                switch (item.Name)
                {
                    case "p1bet":
                        _scrapeResult.P1Bet = SetBetValue(res);
                        break;
                    case "p2bet":
                        _scrapeResult.P2Bet = SetBetValue(res);
                        break;
                    case "p3bet":
                        _scrapeResult.P3Bet = SetBetValue(res);
                        break;
                    case "p4bet":
                        _scrapeResult.P4Bet = SetBetValue(res);
                        break;
                    case "p5bet":
                        _scrapeResult.P5Bet = SetBetValue(res);
                        break;
                    default:
                        break;
                }

            }


            //Comprobar si llega la mano sin subir
            Dictionary<HeroPosition, List<double>> preflopHeroPosition = new Dictionary<HeroPosition, List<double>>()
            {
                { HeroPosition.BigBlind, new List<double> { _scrapeResult.P1Bet, _scrapeResult.P2Bet, _scrapeResult.P3Bet, _scrapeResult.P4Bet, _scrapeResult.P5Bet } },
                { HeroPosition.SmallBlind, new List<double> { _scrapeResult.P2Bet, _scrapeResult.P3Bet, _scrapeResult.P4Bet, _scrapeResult.P5Bet } },
                { HeroPosition.Button, new List<double> { _scrapeResult.P3Bet, _scrapeResult.P4Bet, _scrapeResult.P5Bet } },
                { HeroPosition.CutOff, new List<double> { _scrapeResult.P4Bet, _scrapeResult.P5Bet } },
                { HeroPosition.MiddlePosition, new List<double> { _scrapeResult.P5Bet } }
            };

            var responseAction = string.Empty;
                
            responseAction = GetOpenRaiseAction(preflopHeroPosition);

            if(responseAction is null)
            {
                responseAction = GetRaiseOverLimperAction(preflopHeroPosition);
            }

            


            if (_scrapeResult.P0Position == HeroPosition.EarlyPosition && string.IsNullOrEmpty(responseAction))
            {

                var openRaiseCommand = new GetOpenRaiseUseCaseRequest
                {
                    Hand = SetHandValue(),
                    Position = _scrapeResult.P0Position
                };

                var response = _openRaiseUseCase.Execute(openRaiseCommand);

            }

            SetBoardValues();
            lbAction.Text = responseAction;
            responseAction = string.Empty;


            ////var response = _openRaiseUseCase.Execute(openRaiseCommand);
            //var response = _threeBetUseCase.Execute(openRaiseCommand);

        }

        private string GetRaiseOverLimperAction(Dictionary<HeroPosition, List<double>> preflopHeroPosition)
        {
            var responseAction = new GetRaiseOverLimperUseCaseResponse();

            if (preflopHeroPosition.ContainsKey(_scrapeResult.P0Position))
            {
                List<double> bets = preflopHeroPosition[_scrapeResult.P0Position];
                bool noneBet = bets.Any(b => b == 1 && b <= 1);

                if (noneBet)
                {
                    var openRaiseCommand = new GetRaiseOverLimperUseCaseRequest
                    {
                        Hand = SetHandValue(),
                        Position = _scrapeResult.P0Position
                    };

                    responseAction = _raiseOverLimperUseCase.Execute(openRaiseCommand);
                }
            }

            return responseAction.Action;
        }

        private string GetOpenRaiseAction(Dictionary<HeroPosition, List<double>> preflopHeroPosition)
        {
            var responseAction = new GetOpenRaiseUseCaseResponse();

            if (preflopHeroPosition.ContainsKey(_scrapeResult.P0Position))
            {
                List<double> bets = preflopHeroPosition[_scrapeResult.P0Position];
                bool noneBet = bets.Any(b => b > 0);

                if (!noneBet)
                {
                    var openRaiseCommand = new GetOpenRaiseUseCaseRequest
                    {
                        Hand = SetHandValue(),
                        Position = _scrapeResult.P0Position
                    };

                    responseAction = _openRaiseUseCase.Execute(openRaiseCommand);
                }
            }

            return responseAction.Action;
        }

        private string SetHandValue()
        {
            string hand = string.Empty;

            if (_scrapeResult.U0CardForce0 >= _scrapeResult.U0CardForce1)
                hand = $"{_scrapeResult.U0CardFace0[0]}{_scrapeResult.U0CardFace1[0]}";
            else
                hand = $"{_scrapeResult.U0CardFace1[0]}{_scrapeResult.U0CardFace0[0]}";


            if (_scrapeResult.U0CardSuit0 == _scrapeResult.U0CardSuit1)
                hand += "s";
            else
                hand += "o";


            return hand;
        }

        private double SetBetValue(Page res)
        {
            var text = string.Empty;
            var resultText = res.GetText();
            if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("Z"))
                text = "2";
            else if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("S"))
                text = "3";
            else
                text = resultText.Replace("BB", "").Trim().Replace(" ", ",");

            double.TryParse(text, out double pBet);
            if (pBet == 50)
                pBet = 0.5f;

            return pBet;
        }

        private void GetImageWhilePlaying()
        {
            var folderPath = @"C:\Code\ScrapePoker\resources\Games\Game_" + new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var path = folderPath + @"\game_" + DateTime.Now.Ticks + ".png";

            var imagen = _useCase.ExecuteImage(path);

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

        private void btnWindow_Click(object sender, EventArgs e)
        {
            Thread.Sleep(2000);
            _useCase.GetWindow(CaptureWindowsHelper.User32.GetForegroundWindow());
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
            string iHash1 = GetHashFont(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(_formImage.pbImagen.Image), new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height)), 120));
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

        private void SetBoardValues()
        {
            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace0))
                pbCard0.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.U0CardFace0))!.Image;
            else
                pbCard0.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace1))
                pbCard1.Image = _images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.U0CardFace1))!.Image;
            else
                pbCard1.Image = null;
        }

        public string GetHashImage(Bitmap bmpSource)
        {
            List<bool> lResult = new List<bool>();
            var textHash = string.Empty;
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(20, 35));
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



