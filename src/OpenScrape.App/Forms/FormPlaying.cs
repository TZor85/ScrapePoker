using OpenScrape.App.Aplication;
using OpenScrape.App.Entities;
using OpenScrape.App.Helpers;
using OpenScrape.App.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Tesseract;
using Image = System.Drawing.Image;
using OpenScrape.App.Aplication.UseCases;

namespace OpenScrape.App.Forms
{
    public partial class FormPlaying : Form
    {
        public bool Checked = false;
        public FormImage FormImage = new FormImage();

        private List<int> _colorDealer = new List<int> { 250, 251, 252, 253, 254, 255 };
        private List<int> _colorActive = new List<int> { 33, 34, 35, 36, 37 };
        private List<int> _colorSit = new List<int> { 0, 9, 11, 12 };

        TableScrapeResult _scrapeResult = new TableScrapeResult();
        BoardPlayerData _boardPlayerData = new BoardPlayerData();

        public List<Regions> Regions = new List<Regions>();
        public List<ImageRegion> Images = new List<ImageRegion>();
        public List<ImageRegion> ImagesBoard = new List<ImageRegion>();
        public List<FontRegion> Fonts = new List<FontRegion>();

        private int _umbral = 86;
        private string? _effectiveStack = string.Empty;

        private readonly GetWindowsScreenUseCase _useCase = new GetWindowsScreenUseCase();
        private readonly GetActions3HandedUseCase _actions3HandedUseCase = new GetActions3HandedUseCase();
        private readonly GetActions2HandedUseCase _actions2HandedUseCase = new GetActions2HandedUseCase();

        public FormPlaying()
        {
            InitializeComponent();
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {

            if (!Checked)
            {
                GetImageWhilePlaying();
            }

            //AutoItX3 au3 = new AutoItX3();
            //au3.MouseMove(0, 0, 10);


            //TODO: hacer las comparaciones de las imagenes / OCR

            _scrapeResult = new TableScrapeResult();

            var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(FormImage.pbImagen.Image), _umbral));

            foreach (var item in Regions.Where(x => x.IsHash))
            {
                var maxEqual = 0;
                var max = 0;

                string iHash1 = GetHashImage(CaptureWindowsHelper.BinaryImage(CropImage(new Bitmap(FormImage.pbImagen.Image), new Rectangle(item.X, item.Y, item.Width, item.Height)), 130));

                foreach (var immg in Images)
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

            foreach (var item in Regions.Where(x => x.IsColor))
            {

                if (FormImage.pbImagen.Image != null)
                {
                    Color color = new Bitmap(FormImage.pbImagen.Image).GetPixel(item.X, item.Y);
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

            foreach (var item in Regions.Where(x => !x.IsColor && !x.IsHash))
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

        private void btnWindow_Click(object sender, EventArgs e)
        {
            Thread.Sleep(2000);
            _useCase.GetWindow();
           
        }

        #region ActionHands

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

        #endregion

        #region Private Methods

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

            FormImage.Width = 461;
            FormImage.Height = 327;

            FormImage.Width = windowImg.Width + FormImage.Width / 11;
            FormImage.Height = windowImg.Height + FormImage.Height / 4;

            FormImage.pbImagen.Width = windowImg.Width;
            FormImage.pbImagen.Height = windowImg.Height;

            FormImage.pbImagen.Image = windowImg;
            FormImage.pbImagen.Refresh();

            Thread.Sleep(100);
        }

        private string GetHashImage(Bitmap bmpSource)
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

        private Bitmap CropImage(Bitmap source, Rectangle section)
        {
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            Graphics g = Graphics.FromImage(bmp);

            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);

            return bmp;
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

        private void SetBoardValues()
        {
            lbP0Chips.Text = $"Chips: {_scrapeResult.P0Chips}";
            lbP1Chips.Text = $"Chips: {_scrapeResult.P1Chips}";
            lbP2Chips.Text = $"Chips: {_scrapeResult.P2Chips}";

            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace0))
                pbCard0.Image = Images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.U0CardFace0))!.Image;
            else
                pbCard0.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace1))
                pbCard1.Image = Images.FirstOrDefault(x => x.Name.Contains(_scrapeResult.U0CardFace1))!.Image;
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

        private string GetEffectiveBB(string p0BB, string p1BB, string p2BB)
        {
            double medio = 0;

            double.TryParse(p0BB.Split(" ")[0].Replace(".", ",").Replace("O", "0"), out double p0chips);
            double.TryParse(p1BB.Split(" ")[0].Replace(".", ",").Replace("O", "0"), out double p1chips);
            double.TryParse(p2BB.Split(" ")[0].Replace(".", ",").Replace("O", "0"), out double p2chips);


            if (!_scrapeResult.P1Active)
                p1chips = 0;

            if (!_scrapeResult.P2Active)
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

        #endregion

        
    }

}
