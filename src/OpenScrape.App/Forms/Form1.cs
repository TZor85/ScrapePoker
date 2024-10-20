using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Aplication.UseCases.Actions;
using OpenScrape.App.Entities;
using OpenScrape.App.Enums;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Interfaces;
using OpenScrape.App.Models;
using System.Text;
using Tesseract;
using static OpenScrape.App.Helpers.CaptureWindowsHelper;
using Image = System.Drawing.Image;
using Page = Tesseract.Page;

namespace OpenScrape.App
{
    public partial class Form1 : Form, IAddRegion
    {
        #region Forms
        FormRegions _formRegions;
        FormCreateImage _formCreateImage;
        FormCreateFont _formCreateFont;
        FormImage _formImage;
        Graphics _papel;
        FormAction _formAction;
        #endregion

        #region Regions

        List<Regions> _regions = new List<Regions>();
        List<ImageRegion> _images = new List<ImageRegion>();
        List<ImageRegion> _imagesBoard = new List<ImageRegion>();
        List<FontRegion> _fonts = new List<FontRegion>();
        Regions _locRegion = new Regions();
        ImageRegion _locImage = new ImageRegion();

        #endregion

        Image? _img = null;

        List<KeyValuePair<string, string>> _imageList = new List<KeyValuePair<string, string>>();

        TableScrapeResult _scrapeResult = new TableScrapeResult();
        TableScrapeFlopResult _scrapeFlopResult = new TableScrapeFlopResult();
        ResponseAction _responseAction = new ResponseAction();



        private int _speed = 1;
        public string _newRegion = string.Empty;
        private string Key = "8UHjPgXZzXCGkhxV2QCnooyJexUzvJrO";
        private string _folderPath = string.Empty;
        private string _tableHand = string.Empty;

        //Portatil
        private string _pathResume = @$"C:\Code\Poker\ScrapePoker\resources\resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt";

        //private string _pathResume = @$"C:\Code\ScrapePoker\resources\resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt";

        private List<int> _colorDealer = new List<int> { 250, 251, 252, 253, 254, 255 };
        private List<int> _colorEmpty = new List<int> { 43, 44, 45, 46, 47, 48, 49, 57, 66, 67, 68, 69 };
        private List<int> _colorPlaying = new List<int> { 32, 33, 34 };
        private List<int> _colorPlayingRed = new List<int> { 36 };
        private List<int> _colorSitOut = new List<int> { 68, 78, 80 };
        private Dictionary<HeroPosition, Dictionary<HeroPosition, decimal>> _preflopHeroPosition = new Dictionary<HeroPosition, Dictionary<HeroPosition, decimal>>();

        private int _pictureUmbralBet = 130;
        private string _session = string.Empty;
        private IntPtr _handle = new IntPtr();
        User32.RECT _locWindowRect = new User32.RECT();
        bool _executeCapture = false;

        bool _isPreflop = true;
        bool _isFlop = false;

        long _newTableHand;

        bool _newHand = false;
        bool _backgroundExecute = false;

        private readonly GetWindowsScreenUseCase _useCase = new GetWindowsScreenUseCase();

        #region Action_UseCase

        static readonly IGetActionHeroCallOpenRaiseAndGetSqueezeUseCase _heroCallOpenRaiseAndGetSqueezeUseCase = new GetActionHeroCallOpenRaiseAndGetSqueezeUseCase();
        static readonly IGetActionHero3BetAndOpenRaiser4BetUseCase _hero3BetAndOpenRaiser4BetUseCase = new GetActionHero3BetAndOpenRaiser4BetUseCase();
        static readonly IGetActionVs3BetUseCaseUseCase _vs3BetUseCase = new GetActionVs3BetUseCaseUseCase();
        static readonly IGetActionVs3BetAndCallUseCase _vs3BetAndCallUseCase = new GetActionVs3BetAndCallUseCase();
        static readonly IGetActionSqueezeUseCase _squeezeUseCase = new GetActionSqueezeUseCase();
        static readonly IGetActionOpenRaiseUseCase _openRaiseUseCase = new GetActionOpenRaiseUseCase();
        static readonly IGetActionRaiseOverLimperUseCase _raiseOverLimperUseCase = new GetActionRaiseOverLimperUseCase();
        static readonly IGetAction3BetUseCase _threeBetUseCase = new GetAction3BetUseCase();
        static readonly IGetActionRaiseVsSBLimpUseCase _raiseVsSBLimpUseCase = new GetActionRaiseVsSBLimpUseCase();
        static readonly IGetActionCold4BetUseCase _cold4BetUseCase = new GetActionCold4BetUseCase();

        readonly ISetPreflopActionUseCase _setPreflopActionUseCase = new SetPreflopActionUseCase(_heroCallOpenRaiseAndGetSqueezeUseCase,
                                                                                                 _hero3BetAndOpenRaiser4BetUseCase,
                                                                                                 _vs3BetUseCase,
                                                                                                 _vs3BetAndCallUseCase,
                                                                                                 _squeezeUseCase,
                                                                                                 _openRaiseUseCase,
                                                                                                 _raiseOverLimperUseCase,
                                                                                                 _threeBetUseCase,
                                                                                                 _raiseVsSBLimpUseCase,
                                                                                                 _cold4BetUseCase);

        #endregion

        #region UseCases
        readonly ISetFlopForceBoardUseCase _setFlopForceBoardUseCase = new SetFlopForceBoardUseCase();

        static readonly IGetHashImageUseCase _getHashImageUseCase = new GetHashImageUseCase();
        static readonly IGetCropImageUseCase _getCropImageUseCase = new GetCropImageUseCase();

        readonly IGetCardsFlopUseCase _getCardsFlopUseCase = new GetCardsFlopUseCase(_getHashImageUseCase, _getCropImageUseCase);

        #endregion

        private readonly ISaveTableMapUseCase _saveUseCase = new SaveTableMapUseCase();
        private readonly ILoadTableMapUseCase _loadUseCase = new LoadTableMapUseCase();

        public Form1()
        {
            InitializeComponent();
            _session = GenerateRandomNumbers();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _formImage = new FormImage();
            cbSpeed.SelectedIndex = 0;

            _formImage.Location = new Point(this.Width, this.Location.Y);
            _formImage.Show();
        }



        #region FrontMethods

        private void btnNew_Click(object sender, EventArgs e)
        {
            _formRegions = new FormRegions();

            _formRegions.Location = new Point(_formImage.Location.X, _formImage.Location.Y + 100);
            _formRegions.region = this;
            _formRegions.Show();

        }

        private void twRegions_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var sen = (System.Windows.Forms.TreeView)sender;
            var name = sen.SelectedNode.Text;

            if (twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo0")
            {
                var region = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                _locRegion = region;

                ckColor.Enabled = true;
                ckColor.Checked = region.IsColor;
                ckBoard.Enabled = true;
                ckBoard.Checked = region.IsBoard;
            }

            if (twRegions.SelectedNode.Parent != null && twRegions.SelectedNode.Parent.Name == "Nodo2")
            {
                _locImage = _images?.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text) ?? new ImageRegion();

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

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(twRegions.SelectedNode.Text))
            {
                var node = _regions.FirstOrDefault(x => x.Name == twRegions.SelectedNode.Text);

                twRegions.Nodes.Remove(twRegions.SelectedNode);
                _regions.Remove(node);

            }
        }

        #endregion

        private string GetValueImage()
        {
            var imageBmp = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height) }).Image;
            return _getHashImageUseCase.Execute(new GetHashImageUseCaseRequest { Image = CaptureWindowsHelper.BinaryImage(imageBmp, 130) }).Hash;
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            lbAction.Text = string.Empty;
            _executeCapture = true;

            if (!cbTest.Checked)
            {
                _formAction.DatoRecibido = string.Empty;

                if (cbMark.Checked)
                    CreateLogWithMarkedHands();

                GetImageWhilePlaying();
                _formImage.WindowState = FormWindowState.Minimized;
            }

            SetTableHand();

            if (_newHand)
            {
                _scrapeResult = new TableScrapeResult();
                _responseAction = new ResponseAction();
                _preflopHeroPosition = new Dictionary<HeroPosition, Dictionary<HeroPosition, decimal>>();
                _newHand = false;

                _folderPath = @"C:\Code\Poker\ScrapePoker\resources\Games\Game_" + new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_");

                _folderPath += $"\\{_session}";

                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);

                }

                var path = _folderPath + @"\resume.txt";

                File.AppendAllText(path, tbResume.Text + Environment.NewLine);
                tbResume.Text = string.Empty;

                
            }

            //TODO: Comprobar second hand
            ObtainCardsPlayer();
            SetEmptyAndActivePlayer();
            SetDealerPlayer();
            SetSitOutPlayer();
            SetVillainPosition(_scrapeResult.P0Position);

            
            SetBetPlayer();

            _preflopHeroPosition = GetPreflopHeroPosition();

            if (!_isFlop)
            {
                _isPreflop = true;
                var response = _setPreflopActionUseCase.Execute(new SetPreflopActionUseCaseRequest { ResponseAction = _responseAction, ScrapeResult = _scrapeResult, PreflopHeroPosition = _preflopHeroPosition });

                _responseAction = response.ResponseAction;
                _scrapeResult = response.ScrapeResult;
            }
            else
            {
                //Si existe el flop, capturar las cartas del flop
                if (_isFlop)
                {
                    
                    var dataBoard = _getCardsFlopUseCase.Execute(new GetCardsFlopUseCaseRequest { Image = new Bitmap(_formImage.pbImagen.Image), Regions = _regions.Where(x => x.IsHash).ToList(), ImageRegions = _images }).DataBoard;
                    _scrapeResult.DataBoard = dataBoard;
                    var setFlopForceBoardResponse = _setFlopForceBoardUseCase.Execute(new SetFlopForceBoardUseCaseRequest { TableScrapeResult = _scrapeResult, TableScrapeFlopResult = _scrapeFlopResult });
                    _scrapeResult = setFlopForceBoardResponse.TableScrapeResult;
                    _scrapeFlopResult = setFlopForceBoardResponse.TableScrapeFlopResult;

                    //_scrapeResult.DataBoard = dataBoard;
                    //_scrapeFlopResult = _setFlopForceBoardUseCase.Execute(new SetFlopForceBoardUseCaseRequest { TableScrapeResult = _scrapeResult, TableScrapeFlopResult = _scrapeFlopResult }).TableScrapeFlopResult;

                    SetIsInPosition();

                    switch (_scrapeResult.HandSituation)
                    {
                        case HandSituation.OpenRaise:

                            if (_scrapeFlopResult.HaveTwoPairOnFlop ||
                                _scrapeFlopResult.HaveOverPairOnFlop ||
                                (_scrapeFlopResult.HaveTopPairOnFlop && !_scrapeFlopResult.HaveHighCardsOnHand) ||
                                (_scrapeFlopResult.HaveHighCardsOnHand && _scrapeFlopResult.HaveBackdoorFlushDraw))
                            {
                                _responseAction.Action = "Bet 3/4";
                            }
                            else
                            {
                                _responseAction.Action = "Check";
                            }

                            if (_scrapeResult.U0InPosition)
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {
                                    //_scrapeResult.
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.Call:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.RaiseOverLimper:
                            //IP
                            if (_scrapeResult.U0InPosition)
                            {
                                //Manos fuertes
                                if((_scrapeFlopResult.HaveTwoPairOnFlop ))
                                {
                                    _responseAction.Action = "Bet 3/4";
                                }
                                else
                                {
                                    _responseAction.Action = "Check";
                                }

                                //Flop ofensivo (cartas altas)
                                if (_scrapeResult.DataBoard.Where(w => w.Position == BoardPosition.Flop).Count(c => c.Force > 11) >= 2)
                                    _responseAction.Action = "Bet 1/3";
                                


                            }
                            //OOP
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.ThreeBet:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.OpenRaiseVs3Bet:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.OpenRaiseVs3BetAndCall:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.FourBet:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.Cold4Bet:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.Squeeze:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;
                        case HandSituation.VsSqueeze:
                            if (_scrapeResult.U0InPosition)
                            {
                            }
                            else
                            {
                                if (_scrapeFlopResult.FlopIsCoordinate)
                                {

                                }
                                else
                                {

                                }
                            }
                            break;

                        default:
                            break;
                    }

                    //vs recreacionales
                    if (_scrapeResult.HandSituation == HandSituation.RaiseOverLimper)
                    {
                        if (_scrapeResult.U0InPosition)
                        {

                        }
                        else
                        {

                        }

                    }

                }

                _responseAction.Action = "No Preflop action";
            }

            _scrapeResult.HandSituation = _responseAction.HandSituation;

            if (_scrapeResult != null)
            {
                if(_isPreflop)
                {
                    var enMesa = _scrapeResult.DataPlayer.Count(e => !e.Empty) + 1;
                    var sitout = _scrapeResult.DataPlayer.Count(s => s.SitOut);
                    var playing = _scrapeResult.DataPlayer.Count(p => p.Active) + 1;

                    tbResume.Text += $"Hand #{_tableHand}: Hold'em No Limit \r\n";
                    tbResume.Text += $"#{_scrapeResult.DataPlayer.FirstOrDefault(d => d.Dealer)?.Name ?? "Hero"} is the Dealer\r\n";
                    tbResume.Text += $"{_scrapeResult.DataPlayer.FirstOrDefault(f => f.Position == HeroPosition.SmallBlind)?.Name ?? "Hero"}: posts small blind\r\n";
                    tbResume.Text += $"{_scrapeResult.DataPlayer.FirstOrDefault(f => f.Position == HeroPosition.BigBlind)?.Name ?? "Hero"}: posts big blind\r\n";
                    tbResume.Text += "*** HOLE CARDS ***\r\n";
                    tbResume.Text += $"Dealt to Hero [{_scrapeResult.U0CardFace0} {_scrapeResult.U0CardFace1}]\r\n";
                    
                    foreach (HeroPosition position in Enum.GetValues(typeof(HeroPosition)))
                    {
                        if (position != HeroPosition.None && position <= _scrapeResult.P0Position)
                        {
                            var player = _scrapeResult.DataPlayer.FirstOrDefault(f => f.Position == position);
                            string name = player?.Name ?? "Hero";
                            string action = string.Empty;
                            if (name != "Hero")
                                action = player?.Bet == null ? "folds" : $"bets/calls {player.Bet}";
                            else
                            {
                                if (position == _scrapeResult.P0Position)
                                {
                                    tbResume.Text += $"Hand Situation: {_scrapeResult.HandSituation}\r\n";
                                    action = _responseAction?.Action ?? string.Empty;
                                }
                            }

                            if(!string.IsNullOrWhiteSpace(action))
                                tbResume.Text += $"{name}: {action}\r\n";
                        }
                    }

                    _isPreflop = false;
                }

                if(_isFlop)
                {
                    tbResume.Text += "*** FLOP *** [";
                    var countFlop = 0;
                    foreach (var carta in _scrapeResult.DataBoard.Where(w => w.Position == BoardPosition.Flop))
                    {
                        countFlop++;
                        if(countFlop == 3)
                            tbResume.Text += $"{carta.Name}]";
                        else
                            tbResume.Text += $"{carta.Name} ";
                    }
                }
            }

            if (!_isFlop)
            {
                
            }

            SetBoardValues();
            lbAction.Text = _responseAction?.Action ?? string.Empty;

            if (_formAction != null)
                _formAction.DatoRecibido = _responseAction?.Action ?? string.Empty;
        }

        private void SetBetPlayer()
        {
            var img = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), _pictureUmbralBet));

            foreach (var item in _regions.Where(x => !x.IsColor && !x.IsHash && x.Name.Contains("bet")))
            {
                switch (item.Name)
                {
                    case "u0bet":
                        _scrapeResult.U0Bet = SetBetValue(GetTextBetByPosition(item.X, item.Y, item.Width, item.Height, img));
                        break;
                    case "p1bet":
                        if (_scrapeResult.DataPlayer.First(f => f.Name == "P1").Active && _scrapeResult.DataPlayer.First(f => f.Name == "P1").Position < _scrapeResult.P0Position && _scrapeResult.DataPlayer.First(f => f.Name == "P1").Position != HeroPosition.None)
                            _scrapeResult.DataPlayer.First(n => n.Name == "P1").Bet = SetBetValue(GetTextBetByPosition(item.X, item.Y, item.Width, item.Height, img));
                        break;
                    case "p2bet":
                        if (_scrapeResult.DataPlayer.First(f => f.Name == "P2").Active && _scrapeResult.DataPlayer.First(f => f.Name == "P2").Position < _scrapeResult.P0Position && _scrapeResult.DataPlayer.First(f => f.Name == "P2").Position != HeroPosition.None)
                            _scrapeResult.DataPlayer.First(n => n.Name == "P2").Bet = SetBetValue(GetTextBetByPosition(item.X, item.Y, item.Width, item.Height, img));
                        break;
                    case "p3bet":
                        if (_scrapeResult.DataPlayer.First(f => f.Name == "P3").Active && _scrapeResult.DataPlayer.First(f => f.Name == "P3").Position < _scrapeResult.P0Position && _scrapeResult.DataPlayer.First(f => f.Name == "P3").Position != HeroPosition.None)
                            _scrapeResult.DataPlayer.First(n => n.Name == "P3").Bet = SetBetValue(GetTextBetByPosition(item.X, item.Y, item.Width, item.Height, img));
                        break;
                    case "p4bet":
                        if (_scrapeResult.DataPlayer.First(f => f.Name == "P4").Active && _scrapeResult.DataPlayer.First(f => f.Name == "P4").Position < _scrapeResult.P0Position && _scrapeResult.DataPlayer.First(f => f.Name == "P4").Position != HeroPosition.None)
                            _scrapeResult.DataPlayer.First(n => n.Name == "P4").Bet = SetBetValue(GetTextBetByPosition(item.X, item.Y, item.Width, item.Height, img));
                        break;
                    case "p5bet":
                        if (_scrapeResult.DataPlayer.First(f => f.Name == "P5").Active && _scrapeResult.DataPlayer.First(f => f.Name == "P5").Position < _scrapeResult.P0Position && _scrapeResult.DataPlayer.First(f => f.Name == "P5").Position != HeroPosition.None)
                            _scrapeResult.DataPlayer.First(n => n.Name == "P5").Bet = SetBetValue(GetTextBetByPosition(item.X, item.Y, item.Width, item.Height, img));
                        break;

                    default:
                        break;
                }
            }
        }

        private void SetEmptyAndActivePlayer()
        {
            foreach (var region in _regions.Where(x => x.IsColor))
            {
                if (_formImage.pbImagen.Image == null)
                    continue;

                Color color = new Bitmap(_formImage.pbImagen.Image).GetPixel(region.X, region.Y);

                if (!region.Name.Contains("dealer"))
                {
                    //Setear el jugador activo/desactivo
                    switch (region.Name)
                    {
                        case "p1playing":
                            if (_colorPlaying.Contains(color.B) && !_colorPlayingRed.Contains(color.R))
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P1", Active = true, Empty = false, SitOut = false, ValuePosition = 1 });
                            else
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P1", Active = false, Empty = false, SitOut = false, ValuePosition = 1 });
                            break;
                        case "p2playing":
                            if (_colorPlaying.Contains(color.B) && !_colorPlayingRed.Contains(color.R))
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P2", Active = true, Empty = false, SitOut = false, ValuePosition = 2 });
                            else
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P2", Active = false, Empty = false, SitOut = false, ValuePosition = 2 });
                            break;
                        case "p3playing":
                            if (_colorPlaying.Contains(color.B) && !_colorPlayingRed.Contains(color.R))
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P3", Active = true, Empty = false, SitOut = false, ValuePosition = 3 });
                            else
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P3", Active = false, Empty = false, SitOut = false, ValuePosition = 3 });
                            break;
                        case "p4playing":
                            if (_colorPlaying.Contains(color.B) && !_colorPlayingRed.Contains(color.R))
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P4", Active = true, Empty = false, SitOut = false, ValuePosition = 4 });
                            else
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P4", Active = false, Empty = false, SitOut = false, ValuePosition = 4 });
                            break;
                        case "p5playing":
                            if (_colorPlaying.Contains(color.B) && !_colorPlayingRed.Contains(color.R))
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P5", Active = true, Empty = false, SitOut = false, ValuePosition = 5 });
                            else
                                _scrapeResult.DataPlayer.Add(new PlayerData { Name = "P5", Active = false, Empty = false, SitOut = false, ValuePosition = 5 });
                            break;
                    }


                    //Setear el jugador vacio
                    if (_colorEmpty.Contains(color.B))
                    {
                        switch (region.Name)
                        {
                            case "p1empty":
                                _scrapeResult.DataPlayer.First(n => n.Name == "P1").Empty = true;
                                _scrapeResult.DataPlayer.First(n => n.Name == "P1").SitOut = true;
                                break;
                            case "p2empty":
                                _scrapeResult.DataPlayer.First(n => n.Name == "P2").Empty = true;
                                _scrapeResult.DataPlayer.First(n => n.Name == "P2").SitOut = true;
                                break;
                            case "p3empty":
                                _scrapeResult.DataPlayer.First(n => n.Name == "P3").Empty = true;
                                _scrapeResult.DataPlayer.First(n => n.Name == "P3").SitOut = true;
                                break;
                            case "p4empty":
                                _scrapeResult.DataPlayer.First(n => n.Name == "P4").Empty = true;
                                _scrapeResult.DataPlayer.First(n => n.Name == "P4").SitOut = true;
                                break;
                            case "p5empty":
                                _scrapeResult.DataPlayer.First(n => n.Name == "P5").Empty = true;
                                _scrapeResult.DataPlayer.First(n => n.Name == "P5").SitOut = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void SetTableHand()
        {
            foreach (var item in _regions.Where(x => !x.IsColor && !x.IsHash && x.Name.Contains("tablehand")))
            {
                if (string.IsNullOrEmpty(_tableHand))
                    _tableHand = GetTextSitOutByPosition(item.X, item.Y, item.Width, item.Height, 168);
                else
                {
                    long.TryParse(_tableHand, out long oldTableHand);
                    long.TryParse(GetTextSitOutByPosition(item.X, item.Y, item.Width, item.Height, 168), out long newTableHand);

                    if (oldTableHand != newTableHand)
                    {
                        _newHand = true;
                        _tableHand = newTableHand.ToString();
                    }
                    else
                    {
                        if (newTableHand == 0)
                        {
                            _newHand = true;
                            _newTableHand++;
                            _tableHand = _newTableHand.ToString();
                        }   
                    }
                }
            }
        }

        private void SetDealerPlayer()
        {
            foreach (var region in _regions.Where(x => x.IsColor))
            {
                if (_formImage.pbImagen.Image == null)
                    continue;

                Color color = new Bitmap(_formImage.pbImagen.Image).GetPixel(region.X, region.Y);

                if (_colorDealer.Contains(color.R))
                {
                    var emptys = _scrapeResult.DataPlayer.Where(w => w.Empty || w.SitOut).ToList().Select(s => s.ValuePosition).ToList();

                    switch (region.Name)
                    {
                        case "p0dealer":
                            _scrapeResult.P0Dealer = true;
                            _scrapeResult.P0Position = HeroPosition.Button;
                            break;
                        case "p1dealer":
                            _scrapeResult.DataPlayer.First(n => n.Name == "P1").Dealer = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P1").Empty = false;
                            _scrapeResult.P0Position = HeroPosition.CutOff;
                            break;
                        case "p2dealer":
                            _scrapeResult.DataPlayer.First(n => n.Name == "P2").Dealer = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P2").Empty = false;

                            if (emptys.Count > 0)
                            {
                                if (emptys.Count(c => c > 2) > 0)
                                {
                                    switch (emptys.Count(c => c > 2))
                                    {
                                        case 1:
                                            _scrapeResult.P0Position = HeroPosition.EarlyPosition;
                                            break;
                                        case 2:
                                            _scrapeResult.P0Position = HeroPosition.BigBlind;
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (emptys.Count(c => c < 2))
                                    {
                                        case 1:
                                            _scrapeResult.P0Position = HeroPosition.CutOff;
                                            break;
                                        case 2:
                                            _scrapeResult.P0Position = HeroPosition.EarlyPosition;
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                _scrapeResult.P0Position = HeroPosition.MiddlePosition;
                            }

                            break;
                        case "p3dealer":
                            _scrapeResult.DataPlayer.First(n => n.Name == "P3").Dealer = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P3").Empty = false;

                            if (emptys.Count > 0)
                            {
                                if (emptys.Count(c => c > 3) > 0)
                                {
                                    switch (emptys.Count(c => c > 3))
                                    {
                                        case 1:
                                            _scrapeResult.P0Position = HeroPosition.BigBlind;
                                            break;
                                        case 2:
                                            _scrapeResult.P0Position = HeroPosition.SmallBlind;
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (emptys.Count(c => c < 3))
                                    {
                                        case 1:
                                            _scrapeResult.P0Position = HeroPosition.MiddlePosition;
                                            break;
                                        case 2:
                                            _scrapeResult.P0Position = HeroPosition.CutOff;
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                _scrapeResult.P0Position = HeroPosition.EarlyPosition;
                            }

                            break;
                        case "p4dealer":
                            _scrapeResult.DataPlayer.First(n => n.Name == "P4").Dealer = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P4").Empty = false;

                            if (emptys.Count(c => c > 4) > 0)
                            {
                                switch (emptys.Count(c => c > 4))
                                {
                                    case 1:
                                        _scrapeResult.P0Position = HeroPosition.SmallBlind;
                                        break;
                                }
                            }
                            else
                            {
                                _scrapeResult.P0Position = HeroPosition.BigBlind;
                            }

                            break;
                        case "p5dealer":
                            _scrapeResult.DataPlayer.First(n => n.Name == "P5").Dealer = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P5").Empty = false;
                            _scrapeResult.P0Position = HeroPosition.SmallBlind;
                            break;
                    }
                }

            }
        }


        private void SetSitOutPlayer()
        {
            //Setear el jugador sitout
            foreach (var item in _regions.Where(x => !x.IsColor && !x.IsHash && (x.Name.Contains("sitout") || x.Name.Contains("tablename"))))
            {
                switch (item.Name)
                {   
                    case "p1sitout":
                        if (!_scrapeResult.DataPlayer.First(f => f.Name == "P1").Empty &&
                            !_scrapeResult.DataPlayer.First(f => f.Name == "P1").Active &&
                            GetTextSitOutByPosition(item.X, item.Y, item.Width, item.Height, _colorSitOut[0]).Contains("SIT"))
                        {
                            _scrapeResult.DataPlayer.First(n => n.Name == "P1").SitOut = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P1").Empty = true;
                        }
                        break;
                    case "p2sitout":
                        if (!_scrapeResult.DataPlayer.First(f => f.Name == "P2").Empty &&
                            !_scrapeResult.DataPlayer.First(f => f.Name == "P2").Active &&
                            GetTextSitOutByPosition(item.X, item.Y, item.Width, item.Height, _colorSitOut[0]).Contains("SIT"))
                        {
                            _scrapeResult.DataPlayer.First(n => n.Name == "P2").SitOut = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P2").Empty = true;
                        }
                        break;
                    case "p3sitout":
                        if (!_scrapeResult.DataPlayer.First(f => f.Name == "P3").Empty &&
                            !_scrapeResult.DataPlayer.First(f => f.Name == "P3").Active &&
                            GetTextSitOutByPosition(item.X, item.Y, item.Width, item.Height, _colorSitOut[2]).Contains("SIT"))
                        {
                            _scrapeResult.DataPlayer.First(n => n.Name == "P3").SitOut = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P3").Empty = true;
                        }
                        break;
                    case "p4sitout":
                        if (!_scrapeResult.DataPlayer.First(f => f.Name == "P4").Empty &&
                            !_scrapeResult.DataPlayer.First(f => f.Name == "P4").Active &&
                            GetTextSitOutByPosition(item.X, item.Y, item.Width, item.Height, _colorSitOut[1]).Contains("SIT"))
                        {
                            _scrapeResult.DataPlayer.First(n => n.Name == "P4").SitOut = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P4").Empty = true;
                        }
                        break;
                    case "p5sitout":
                        if (!_scrapeResult.DataPlayer.First(f => f.Name == "P5").Empty &&
                            !_scrapeResult.DataPlayer.First(f => f.Name == "P5").Active &&
                            GetTextSitOutByPosition(item.X, item.Y, item.Width, item.Height, _colorSitOut[1]).Contains("SIT"))
                        {
                            _scrapeResult.DataPlayer.First(n => n.Name == "P5").SitOut = true;
                            _scrapeResult.DataPlayer.First(n => n.Name == "P5").Empty = true;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetIsInPosition()
        {
            foreach (var item in _scrapeResult.DataPlayer.Where(w => w.Active && w.ValuePosition != 5 && w.ValuePosition != 6))
            {
                if (item.ValuePosition > (int)_scrapeResult.P0Position)
                {
                    _scrapeResult.U0InPosition = false;
                    break;
                }
                else
                    _scrapeResult.U0InPosition = true;

                if (item.Position == HeroPosition.Button && item.Active)
                {
                    _scrapeResult.U0InPosition = false;
                    break;
                }

            }

            if (_scrapeResult.P0Position == HeroPosition.BigBlind && (_scrapeResult.DataPlayer.Any(a => a.Active && a.Position != HeroPosition.SmallBlind)))
                _scrapeResult.U0InPosition = false;

            if (_scrapeResult.P0Position == HeroPosition.SmallBlind)
                _scrapeResult.U0InPosition = false;

            if (_scrapeResult.P0Position == HeroPosition.BigBlind && (_scrapeResult.DataPlayer.Where(w => w.Active).Count() == 1 && _scrapeResult.DataPlayer.First(w => w.Active).Position == HeroPosition.SmallBlind))
                _scrapeResult.U0InPosition = true;
        }

        private void CreateLogWithMarkedHands()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(_folderPath);
            FileInfo[] archivosPNG = directoryInfo.GetFiles("*.png")
                                                .Where(file => file.Extension.ToLower() == ".png")
                                                .ToArray();

            if (archivosPNG.Length > 0)
            {
                FileInfo ultimaImagen = archivosPNG.OrderByDescending(file => file.LastWriteTime)
                                                  .First();

                string nombreArchivoTexto = $"{directoryInfo.Name}-Revisar.txt";
                string rutaArchivoTexto = Path.Combine(ultimaImagen.Directory.FullName, nombreArchivoTexto);

                if (!File.Exists(rutaArchivoTexto))
                    File.WriteAllText(rutaArchivoTexto, ultimaImagen.Name);
                else
                    File.AppendAllText(rutaArchivoTexto, Environment.NewLine + ultimaImagen.Name);
            }

            cbMark.Checked = false;
        }

        private Page GetTextBetByPosition(int x, int y, int width, int height, Pix imgBet)
        {

            var ocrengine = new TesseractEngine(@".\tessdata\", "eng", EngineMode.Default);
            Rect area = new Rect(x, y, width, height);

            var res = ocrengine.Process(imgBet, area, PageSegMode.Auto);

            return res;
        }

        private string GetTextSitOutByPosition(int x, int y, int width, int height, int umbral)
        {
            var imgSitOut = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), umbral));

            var ocrengine = new TesseractEngine(@".\tessdata\", "eng", EngineMode.Default);
            Rect area = new Rect(x, y, width, height);

            var res = ocrengine.Process(imgSitOut, area, PageSegMode.Auto);
            
            //if (umbral == 168)
            //{
            //    pictureBox1.Image = CaptureWindowsHelper.BinaryImage(_getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(x, y, width, height) }).Image, umbral);

            //    label4.Text = res.GetText().Trim();
            //}

            return res.GetText().Trim();
        }

        private Dictionary<HeroPosition, Dictionary<HeroPosition, decimal>> GetPreflopHeroPosition()
        {
            Dictionary<HeroPosition, decimal> players = new Dictionary<HeroPosition, decimal>();

            foreach (var item in _scrapeResult.DataPlayer)
            {
                if (!item.Empty || !item.SitOut)
                {
                    if (item.Position != HeroPosition.None)
                        players[item.Position] = item.Bet;
                }
            }

            //Comprobar si llega la mano sin subir
            var position = new Dictionary<HeroPosition, Dictionary<HeroPosition, decimal>>();

            foreach (var heroPosition in Enum.GetValues(typeof(HeroPosition)).Cast<HeroPosition>())
            {
                if (heroPosition != HeroPosition.None)
                    position[heroPosition] = new Dictionary<HeroPosition, decimal>(players);
            }

            return position;
        }

        private string GenerateRandomNumbers()
        {
            Random random = new Random();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 10; i++)
            {
                sb.Append(random.Next(0, 10));
            }

            return sb.ToString();
        }

        private void SetVillainPosition(HeroPosition p0Position)
        {
            string[] playerNames = { "P1", "P2", "P3", "P4", "P5" };
            List<HeroPosition> positions = new List<HeroPosition>();

            switch (p0Position)
            {
                case HeroPosition.BigBlind:
                    positions.AddRange(new List<HeroPosition> { HeroPosition.EarlyPosition, HeroPosition.MiddlePosition, HeroPosition.CutOff, HeroPosition.Button, HeroPosition.SmallBlind });
                    SetVillainPositionExtension(playerNames, positions);
                    break;
                case HeroPosition.SmallBlind:
                    positions.AddRange(new List<HeroPosition> { HeroPosition.BigBlind, HeroPosition.EarlyPosition, HeroPosition.MiddlePosition, HeroPosition.CutOff, HeroPosition.Button });
                    SetVillainPositionExtension(playerNames, positions);
                    break;
                case HeroPosition.Button:
                    positions.AddRange(new List<HeroPosition> { HeroPosition.SmallBlind, HeroPosition.BigBlind, HeroPosition.EarlyPosition, HeroPosition.MiddlePosition, HeroPosition.CutOff });
                    SetVillainPositionExtension(playerNames, positions);
                    break;
                case HeroPosition.CutOff:
                    positions.AddRange(new List<HeroPosition> { HeroPosition.Button, HeroPosition.SmallBlind, HeroPosition.BigBlind, HeroPosition.EarlyPosition, HeroPosition.MiddlePosition });
                    SetVillainPositionExtension(playerNames, positions);
                    break;
                case HeroPosition.MiddlePosition:
                    positions.AddRange(new List<HeroPosition> { HeroPosition.CutOff, HeroPosition.Button, HeroPosition.SmallBlind, HeroPosition.BigBlind, HeroPosition.EarlyPosition });
                    SetVillainPositionExtension(playerNames, positions);
                    break;
                case HeroPosition.EarlyPosition:
                    positions.AddRange(new List<HeroPosition> { HeroPosition.MiddlePosition, HeroPosition.CutOff, HeroPosition.Button, HeroPosition.SmallBlind, HeroPosition.BigBlind });
                    SetVillainPositionExtension(playerNames, positions);
                    break;
                default:
                    break;
            }
        }

        private void SetVillainPositionExtension(string[] playerNames, List<HeroPosition> positions)
        {
            foreach (var position in positions)
            {
                foreach (var name in playerNames)
                {
                    var player = _scrapeResult.DataPlayer.FirstOrDefault(n => n.Name == name);

                    if (player != null && (!player.Empty || !player.SitOut) && player.Position == HeroPosition.None)
                    {
                        player.Position = position;
                        break;
                    }
                }
            }
        }

        private void ObtainCardsPlayer()
        {
            foreach (var item in _regions.Where(x => x.IsHash))
            {
                var maxEqual = 0;
                var max = 0;

                var imageBmp = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(item.X, item.Y, item.Width, item.Height) }).Image;
                string iHash1 = _getHashImageUseCase.Execute(new GetHashImageUseCaseRequest { Image = CaptureWindowsHelper.BinaryImage(imageBmp, 130) }).Hash;

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
        }

        private decimal SetBetValue(Page res)
        {
            var text = string.Empty;
            var resultText = res.GetText();
            if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("Z"))
                text = "2";
            else if (resultText.Replace("BB", "").Trim().Replace(" ", ",").Contains("S"))
                text = "3";
            else
                text = resultText.Replace("BB", "").Trim().Replace(" ", ",");

            text = text.Replace(".", ",");

            double.TryParse(text, out double pBet);
            if (pBet == 50)
                pBet = 0.5f;

            return (decimal)pBet;
        }

        private void GetImageWhilePlaying()
        {

            //_folderPath = @"C:\Code\ScrapePoker\resources\Games\Game_" + new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_");


            //portatil
            _folderPath = @"C:\Code\Poker\ScrapePoker\resources\Games\Game_" + new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_");

            _folderPath += $"\\{_session}";

            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);

            }

            var path = _folderPath + @"\game_" + DateTime.Now.Ticks + ".png";

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

        private void btnWindow_Click(object sender, EventArgs e)
        {

            User32.RECT windowRect = new User32.RECT();

            if (_handle == IntPtr.Zero)
            {
                Thread.Sleep(2000);
                _handle = _useCase.GetWindow(CaptureWindowsHelper.User32.GetForegroundWindow());

                User32.GetWindowRect(_handle, ref windowRect);

                _locWindowRect = windowRect;

                _formAction = new FormAction();
                _formAction.Location = new Point(windowRect.left + (((windowRect.right - windowRect.left) / 2) - (_formAction.Size.Width / 2)), windowRect.bottom - 50);
                _formAction.Show();
            }

            if (_handle != IntPtr.Zero)
            {
                User32.GetWindowRect(_handle, ref windowRect);
                if (windowRect.left != _locWindowRect.left || windowRect.right != _locWindowRect.right || windowRect.top != _locWindowRect.top || windowRect.bottom != _locWindowRect.bottom)
                {
                    _locWindowRect = windowRect;
                    this.Invoke((MethodInvoker)delegate
                    {
                        _formAction.Location = new Point(windowRect.left + (((windowRect.right - windowRect.left) / 2) - (_formAction.Size.Width / 2)), windowRect.bottom - 50);
                    });
                }
            }

            if (!_backgroundExecute)
                backgroundWorker1.RunWorkerAsync();

        }

        private void btnCreateImage_Click(object sender, EventArgs e)
        {
            _formCreateImage = new FormCreateImage();
            _formCreateImage.region = this;
            _formCreateImage.Show();
        }


        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            _backgroundExecute = true;

            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(_handle, ref windowRect);

            while (true)
            {
                if (_formAction != null && !_formAction.Visible)
                {
                    e.Cancel = true;
                    return;
                }

                var img = _useCase.Execute(_handle);

                var regionAction = _regions.First(x => x.Name == "uAction");
                Color colorAction = new Bitmap(img).GetPixel(regionAction.X, regionAction.Y);

                var flop = _regions.First(x => x.Name == "isFlop");
                Color colorFlop = new Bitmap(img).GetPixel(flop.X, flop.Y);

                this.Invoke((MethodInvoker)delegate
                {
                    if (colorAction.B == 24 && !_executeCapture && colorFlop.B == 255)
                    {
                        _isFlop = true;
                        //btnCapture_Click(sender, e);
                    }

                    if (colorAction.B == 24 && !_executeCapture)
                    {
                        btnCapture_Click(sender, e);
                    }

                    if (colorAction.B != 24)
                        _executeCapture = false;
                });

                btnWindow_Click(sender, e);
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
                    Image = (Bitmap)_formImage.pbImagen.Image,
                    X = _locRegion.X,
                    Y = _locRegion.Y,
                    IsColor = ckColor.Checked
                };

                var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

                if (ckColor.Checked)
                {
                    tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
            var imageBmp = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height) }).Image;
            string iHash1 = GetHashFont(CaptureWindowsHelper.BinaryImage(imageBmp, 120));
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

        #region Executes

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
                    img = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height) }).Image;
                    _locImage = new ImageRegion { Name = texto, Image = img, IsBoard = true };
                    _imagesBoard.Add(_locImage);
                    break;
                case "Nodo2":
                    texto = $"{texto} ({_locRegion.Width}x{_locRegion.Height})";
                    img = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height) }).Image;
                    _locImage = new ImageRegion { Name = texto, Image = img, IsBoard = false, Value = GetValueImage(), Force = HandHelper.GetForceHand(texto), Suit = HandHelper.GetSuitHand(texto) };
                    _images.Add(_locImage);
                    break;
                case "Nodo3":
                    var imageBmp = _getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(_locRegion.X, _locRegion.Y, _locRegion.Width, _locRegion.Height) }).Image;
                    _fonts.Add(new FontRegion { Name = texto, Value = GetHashFont(CaptureWindowsHelper.BinaryImage(imageBmp, 200), true) });
                    break;

                default:
                    break;
            }

            node.Nodes.Add(texto);
            twRegions.ExpandAll();

        }

        #endregion

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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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
                tbR.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
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

        #region Salir TextBox
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

        #endregion

        private void cbTest_CheckedChanged(object sender, EventArgs e)
        {
            gbTest.Enabled = cbTest.Checked;
        }

        private void cbFlop_CheckedChanged(object sender, EventArgs e)
        {
            if (gbTest.Enabled)
            {
                _isFlop = cbFlop.Checked;
            }
        }

    }
}