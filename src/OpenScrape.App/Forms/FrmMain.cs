using JasperFx.Core;
using Marten;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Helpers.FlopHelper;
using OpenScrape.App.Models;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Mappers;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.Features.ActionScenario;
using OpenScrape.Features.Card;
using OpenScrape.Features.RegionsTableMap;
using OpenScrape.Features.RegionsTableMap.Update;
using System.Data;
using System.Text;
using Tesseract;
using static OpenScrape.App.Helpers.CaptureWindowsHelper;
using Image = System.Drawing.Image;

namespace OpenScrape.App
{
    public partial class FrmMain : Form
    {
        #region Forms
        FormImage _formImage;
        Graphics _papel;
        FormAction _formAction;
        FrmOverlay _frmOverlay;
        #endregion

        #region Regions

        List<Regions> _regions = new List<Regions>();
        List<ImageRegion> _images = new List<ImageRegion>();

        #endregion

        List<Card> _cards = new List<Card>();

        Image? _img = null;

        TableScrapeResult _scrapeResult = new TableScrapeResult();
        TableScrapeFlopResult _scrapeFlopResult = new TableScrapeFlopResult();
        ResponseAction _responseAction = new ResponseAction();

        private int _speed = 1;
        private string _folderPath = string.Empty;
        private string _tableHand = string.Empty;

        private List<RegionTableMap>? _regionsTableMap;
        private Domain.ValueObjects.Region? _selectedRegion;


        //Portatil
        private string _pathResume = @$"C:\Code\Poker\ScrapePoker\resources\resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt";

        //private string _pathResume = @$"C:\Code\ScrapePoker\resources\resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt";

        private List<int> _colorDealer = new List<int> { 250, 251, 252, 253, 254, 255 };
        private List<int> _colorEmpty = new List<int> { 41, 42, 43, 44, 45, 46, 47, 48, 49, 57, 66, 67, 68, 69 };
        private Dictionary<TablePosition, Dictionary<TablePosition, decimal>> _preflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();

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

        private IReadOnlyList<Table>? _tables;
        private List<Table>? _dataTables;

        private readonly GetWindowsScreenUseCase _useCase = new GetWindowsScreenUseCase();
        private ActionScenarioUseCases _actionScenarioUseCases;
        private RegionTableMapUseCases _regionTableMapUseCases;

        #region Action_UseCase
        private ISetPreflopActionUseCase _setPreflopActionUseCase;

        private ImageCropperService _imageCropperService = new();
        private List<CardDTO>? _cardsImages;

        #endregion

        #region DataBase
        private readonly IDocumentStore _dataBase;
        private IDocumentSession _sessionDB;
        #endregion

        #region UseCases
        readonly ISetFlopForceBoardUseCase _setFlopForceBoardUseCase = new SetFlopForceBoardUseCase();

        static readonly IGetHashImageUseCase _getHashImageUseCase = new GetHashImageUseCase();
        static readonly IGetCropImageUseCase _getCropImageUseCase = new GetCropImageUseCase();

        readonly IGetCardsFlopUseCase _getCardsFlopUseCase;
        readonly IOutsCalculatorUseCase _outsCalculatorUseCase = new OutsCalculatorUseCase();
        readonly IPotOddsCalculator _potOddsCalculator;


        #endregion



        private ColorDetectionService _colorDetectionService = new();
        private OcrService _ocrService = new();
        private CardUseCases _cardUseCases;


        private const int BUTTON_SIZE = 40;
        private const int MARGIN = 5;
        private const int MATRIX_SIZE = 13;

        public FrmMain(IDocumentStore dataBase, 
                        ActionScenarioUseCases actionScenarioUseCases, 
                        CardUseCases cardUseCases,
                        RegionTableMapUseCases regionTableMapUseCases)
        {
            InitializeComponent();
            _dataBase = dataBase;
            _session = GenerateRandomNumbers();
            _actionScenarioUseCases = actionScenarioUseCases;
            _regionTableMapUseCases = regionTableMapUseCases;
            _sessionDB = _dataBase.LightweightSession();

            _setPreflopActionUseCase = new SetPreflopActionUseCase(_actionScenarioUseCases);
            _getCardsFlopUseCase = new GetCardsFlopUseCase(_dataBase, _getHashImageUseCase, _getCropImageUseCase);
            _potOddsCalculator = new PotOddsCalculator(_outsCalculatorUseCase);
            _cardUseCases = cardUseCases;
        }

        private async void FrmMain_Load(object sender, EventArgs e)
        {
            _formImage = new FormImage();
            _frmOverlay = new FrmOverlay();
            cbSpeed.SelectedIndex = 0;
            await LoadRegionTableMap();

            await LoadTables(_sessionDB);

            var allCards = await _sessionDB.Query<Card>().ToListAsync();
            _cards = allCards.ToList();

            _formImage.Location = new Point(this.Width, this.Location.Y);
            _formImage.Show();
            //CreateMatrix();


        }

        private async Task LoadRegionTableMap()
        {
            var regions = new List<Domain.ValueObjects.Region>();
            var regionsTableMap = await _sessionDB.Query<RegionTableMap>().ToListAsync();
            var categories = regionsTableMap.Select(x => x.Regions).Where(x => x != null).Distinct().ToList();

            foreach (var group in categories)
            {
                foreach (var category in group!)
                {
                    regions.Add(category);
                }
            }

            _regionsTableMap = regionsTableMap.ToList();
            LoadTreeViewRegions(regionsTableMap.ToList());
        }

        private async Task LoadTables(IDocumentSession session)
        {
            _tables = await session.Query<Table>().ToListAsync();
            _dataTables = _tables.ToList();
            LoadTreeViewTables(_dataTables);
        }

        private void CreateMatrix()
        {
            string[] suits = { "s", "o" }; // suited y offsuit
            string[] ranks = { "A", "K", "Q", "J", "T", "9", "8", "7", "6", "5", "4", "3", "2" };

            for (int row = 0; row < MATRIX_SIZE; row++)
            {
                for (int col = 0; col < MATRIX_SIZE; col++)
                {
                    Button btn = new Button();
                    btn.Size = new Size(BUTTON_SIZE, BUTTON_SIZE);
                    btn.Location = new Point(
                        col * (BUTTON_SIZE + MARGIN) + MARGIN,
                        row * (BUTTON_SIZE + MARGIN) + MARGIN
                    );

                    // Determinar el texto del botón
                    string firstCard = ranks[row];
                    string secondCard = ranks[col];
                    string suffix = row == col ? "" : (row < col ? "s" : "o");
                    btn.Text = firstCard + secondCard + suffix;

                    // Establecer el color del botón
                    btn.FlatStyle = FlatStyle.Flat;

                    if (row == col) // Pares
                    {
                        btn.BackColor = Color.LightBlue;
                    }
                    else if (row < col) // Suited
                    {
                       btn.BackColor = Color.LightYellow;
                    }
                    else // Offsuit
                    {
                        btn.BackColor = Color.LightGreen;
                    }

                    // Añadir número de combinaciones si es necesario
                    if (!string.IsNullOrEmpty(suffix))
                    {
                        btn.Text += "\n" + (suffix == "s" ? "4" : "12");
                    }

                    //btn.Click += Button_Click;
                    this.tbJuego.Controls.Add(btn);
                }
            }
        }

        private void LoadTreeViewTables(IReadOnlyList<Table> tables)
        {
            twTables.Nodes.Clear();

            foreach (var table in tables)
            {
                // Primer nivel - Name de la tabla
                TreeNode actionNode = twTables.Nodes.Add(table.Id, table.Id);

                if (table.Positions != null && table.Positions.Any())
                {
                    // Agrupar por HeroPosition para crear el segundo nivel
                    var positionGroups = table.Positions
                        .GroupBy(p => p.HeroPosition)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var positionGroup in positionGroups)
                    {
                        // Segundo nivel - HeroPosition
                        TreeNode heroPositionNode = actionNode.Nodes.Add(
                            positionGroup.Key, // Key como identificador
                            positionGroup.Key  // Key como texto a mostrar
                        );

                        // Tercer nivel - Name de cada posición
                        foreach (var position in positionGroup.Value)
                        {
                            heroPositionNode.Nodes.Add(
                                position.Name, // Identificador único (puedes usar un Guid si lo necesitas)
                                position.Name  // Texto a mostrar
                            );
                        }
                    }
                }
            }
        }

        private void LoadTreeViewRegions(List<RegionTableMap> categories)
        {
            twRegionsConfig.Nodes.Clear();

            foreach (var category in categories)
            {
                // Añadir nodo principal (categoría)
                TreeNode regionsConfigNode = twRegionsConfig.Nodes.Add(category.Id);

                // Añadir sub-nodos (regiones)
                if (category.Regions != null)
                {
                    foreach (var region in category.Regions)
                    {
                        regionsConfigNode.Nodes.Add(region.Name); // Asumiendo que Region tiene una propiedad Name
                    }
                }
            }
        }

        #region FrontMethods

        private void btnNew_Click(object sender, EventArgs e)
        {
            
        }

        private void twRegions_DoubleClick(object sender, EventArgs e)
        {
            EnableButtons();

            _formImage.pbImagen.Refresh();

            if (twRegionsConfig?.SelectedNode?.Parent != null)
            {
                foreach (TreeNode rootNode in twRegionsConfig.Nodes)
                {
                    if (rootNode != twRegionsConfig?.SelectedNode?.Parent) // Si no es el padre del nodo clickeado
                    {
                        rootNode.Collapse();
                    }
                }
            }

            if (twRegionsConfig?.SelectedNode != null)
            {
                _selectedRegion = ObtenerRegionDelNodo(twRegionsConfig.SelectedNode);
                if (_selectedRegion != null && _formImage.pbImagen.Image != null)
                {
                    _papel = _formImage.pbImagen.CreateGraphics();
                    Pen lapiz = new Pen(Color.Red);

                    _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
                    tbX.Text = _selectedRegion.PosX.ToString();
                    tbY.Text = _selectedRegion.PosY.ToString();
                    tbWidth.Text = _selectedRegion.Width.ToString();
                    tbHeight.Text = _selectedRegion.Height.ToString();
                    tbRegionName.Text = _selectedRegion.Name;
                    tbColor.Text = _selectedRegion.Color != null ? _selectedRegion.Color.ToUpper() : string.Empty;
                    tbRegionUmbral.Text = _selectedRegion.Umbral.ToString();
                    tbRegionInactUmbral.Text = _selectedRegion.InactiveUmbral.ToString();
                    cbRegionColor.Checked = _selectedRegion.IsColor.GetValueOrDefault();
                    cbRegionHash.Checked = _selectedRegion.IsHash.GetValueOrDefault();
                    cbRegionBoard.Checked = _selectedRegion.IsBoard.GetValueOrDefault();
                    cbRegionNumber.Checked = _selectedRegion.IsOnlyNumber.GetValueOrDefault();

                    SetPictureBoxColor(_selectedRegion.Color != null ? _selectedRegion.Color.ToUpper() : string.Empty);

                    if (_selectedRegion.IsColor.GetValueOrDefault())
                        btnTestColor.Enabled = true;

                    if (_selectedRegion.Umbral != null)
                        btnTestTexto.Enabled = true;

                    if (_selectedRegion.IsHash.GetValueOrDefault())
                        btnTestCarta.Enabled = true;
                }
            }
        }

        private Domain.ValueObjects.Region? ObtenerRegionDelNodo(TreeNode node)
        {
            // Implementa la lógica para obtener la región basada en el nodo
            return _regionsTableMap.SelectMany(c => c.Regions ?? new List<Domain.ValueObjects.Region>())
                        .FirstOrDefault(r => r.Name == node.Text);
        }

        private void cbSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            int.TryParse(cbSpeed.Text, out _speed);
        }

        private async void btnSaveMap_Click(object sender, EventArgs e)
        {
            var umbral = string.IsNullOrEmpty(tbRegionUmbral.Text) ? "0" : tbRegionUmbral.Text;
            var inactUmbral = string.IsNullOrEmpty(tbRegionInactUmbral.Text) ? "0" : tbRegionInactUmbral.Text;

            if (double.TryParse(umbral, out double regionUmbral) &&
                double.TryParse(inactUmbral, out double regionInactUmbral) &&
                int.TryParse(tbX.Text, out int x) &&
                int.TryParse(tbY.Text, out int y) &&
                int.TryParse(tbWidth.Text, out int width) &&
                int.TryParse(tbHeight.Text, out int height))
            {
                await Task.Run(async () => await _regionTableMapUseCases.UpdateRegionTableMap.ExecuteAsync(new UpdateRegionTableMapRequest
                (
                    _selectedRegion!.Category,
                    _selectedRegion!.Name,
                    x,
                    y,
                    width,
                    height,
                    regionUmbral,
                    regionInactUmbral,
                    tbColor.Text,
                    cbRegionColor.Checked,
                    cbRegionHash.Checked,
                    cbRegionNumber.Checked,
                    cbRegionBoard.Checked
                )));

                await LoadRegionTableMap();
            }
            else
            {
                MessageBox.Show("Please enter valid numerical values.");
            }
        }

        
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(twRegionsConfig.SelectedNode.Text))
            {
                var node = _regions.FirstOrDefault(x => x.Name == twRegionsConfig.SelectedNode.Text);

                twRegionsConfig.Nodes.Remove(twRegionsConfig.SelectedNode);
                _regions.Remove(node);

            }
        }

        #endregion

        private async void btnCapture_Click(object sender, EventArgs e)
        {
            lbAction.Text = string.Empty;
            _executeCapture = true;
            PotOddsResult potOddsResult = new PotOddsResult();

            if (!cbTest.Checked)
            {
                _frmOverlay.UpdateAction(string.Empty);

                if (cbMark.Checked)
                    CreateLogWithMarkedHands();

                GetImageWhilePlaying();
                _formImage.WindowState = FormWindowState.Minimized;
            }

            if (cbTest.Checked)
                _isFlop = cbFlop.Checked;

            SetTableHand();

            if (_newHand)
            {
                _scrapeResult = new TableScrapeResult();
                _responseAction = new ResponseAction();
                _preflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
                _newHand = false;
                _isFlop = false;

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

            if (_scrapeResult.DataPlayer.Count() == 0)
            {
                await ObtainCardsPlayer();
                SetEmptyPlayer();
                SetSitOutPlayer();
                SetDealerPlayer();                
                SetVillainPosition(_scrapeResult.P0Position);
                
            }

            //TODO: Comprobar second hand 
            SetBetPlayer();
            //_scrapeResult.Pot += _scrapeResult.DataPlayer.Sum(s => s.Bet) + _scrapeResult.U0Bet;
            SetPotValue();
            _preflopHeroPosition = GetPreflopHeroPosition();

            if (!_isFlop)
            {
                _isPreflop = true;
                var response = await _setPreflopActionUseCase.Execute(new SetPreflopActionUseCaseRequest { ResponseAction = _responseAction, ScrapeResult = _scrapeResult, PreflopHeroPosition = _preflopHeroPosition });

                _responseAction = response.ResponseAction;
                _scrapeResult = response.ScrapeResult;
            }
            else
            {
                //Si existe el flop, capturar las cartas del flop
                if (_isFlop)
                {
                    _isFlop = false;
                    var flopResponse = await _getCardsFlopUseCase.Execute(new GetCardsFlopUseCaseRequest { Image = new Bitmap(_formImage.pbImagen.Image), Regions = _regionsTableMap.FirstOrDefault(f => f.Id == "Board").Regions, ImageRegions = _images, RegionsTableMap = _regionsTableMap });
                    var dataBoard = flopResponse.DataBoard;

                    _scrapeResult.DataBoard = dataBoard;
                    var setFlopForceBoardResponse = _setFlopForceBoardUseCase.Execute(new SetFlopForceBoardUseCaseRequest { TableScrapeResult = _scrapeResult, TableScrapeFlopResult = _scrapeFlopResult });
                    _scrapeResult = setFlopForceBoardResponse.TableScrapeResult;
                    _scrapeFlopResult = setFlopForceBoardResponse.TableScrapeFlopResult;

                    //_scrapeResult.DataBoard = dataBoard;
                    //_scrapeFlopResult = _setFlopForceBoardUseCase.Execute(new SetFlopForceBoardUseCaseRequest { TableScrapeResult = _scrapeResult, TableScrapeFlopResult = _scrapeFlopResult }).TableScrapeFlopResult;

                    potOddsResult = _potOddsCalculator.Calculate(new List<CardDataOuts>
                    {
                        new CardDataOuts
                        {
                            Rank = (Rank)_scrapeResult.U0CardForce0,
                            Suit = (Suit)_scrapeResult.U0CardSuit0
                        },
                        new CardDataOuts
                        {
                            Rank = (Rank)_scrapeResult.U0CardForce1,
                            Suit = (Suit)_scrapeResult.U0CardSuit1
                        }
                    },
                    new List<CardDataOuts>
                    {
                        new CardDataOuts
                        {
                            Rank = (Rank)_scrapeResult.DataBoard[0].Force,
                            Suit = (Suit)_scrapeResult.DataBoard[0].Suit
                        },
                        new CardDataOuts
                        {
                            Rank = (Rank)_scrapeResult.DataBoard[1].Force,
                            Suit = (Suit)_scrapeResult.DataBoard[1].Suit
                        },
                        new CardDataOuts
                        {
                            Rank = (Rank)_scrapeResult.DataBoard[2].Force,
                            Suit = (Suit)_scrapeResult.DataBoard[2].Suit
                        }
                    },
                    _scrapeResult.Pot,
                    _scrapeResult.DataPlayer.Max(m => m.Bet));

                    _frmOverlay.UpdatePotOddsPercentage(potOddsResult.PotOddsPercentage.ToString());
                    _frmOverlay.UpdateEquityPercentage(potOddsResult.EquityPercentage.ToString());
                    _frmOverlay.UpdateShouldCall(potOddsResult.ShouldCall);

                    SetIsInPosition();

                    var flopAnalyzerRequest = new FlopAnalyzerHelperReqest { TableScrapeResult = _scrapeResult, TableScrapeFlopResult = _scrapeFlopResult };

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


                            }
                            //OOP
                            else
                            {
                                if (FlopAnalyzerHelper.IsActionToCheckCall(flopAnalyzerRequest))
                                    _responseAction.Action = "Check/Call";
                                else if (FlopAnalyzerHelper.IsActionToCheckFold(flopAnalyzerRequest))
                                    _responseAction.Action = "Check/Fold";
                                else
                                    _responseAction.Action = "Bet 1/3";
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

                _responseAction.Action =  string.IsNullOrEmpty(_responseAction.Action) ? "No Preflop action" : _responseAction.Action;
            }

            _scrapeResult.HandSituation = _responseAction.HandSituation;

            if (_scrapeResult != null)
            {
                if (_isPreflop)
                {
                    var enMesa = _scrapeResult.DataPlayer.Count(e => !e.Empty) + 1;
                    var sitout = _scrapeResult.DataPlayer.Count(s => s.SitOut);
                    var playing = _scrapeResult.DataPlayer.Count(p => p.Active) + 1;

                    tbResume.Text += $"Hand #{_tableHand}: Hold'em No Limit \r\n";
                    tbResume.Text += $"#{_scrapeResult.DataPlayer.FirstOrDefault(d => d.Dealer)?.Name ?? "Hero"} is the Dealer\r\n";
                    tbResume.Text += $"{_scrapeResult.DataPlayer.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "Hero"}: posts small blind\r\n";
                    tbResume.Text += $"{_scrapeResult.DataPlayer.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "Hero"}: posts big blind\r\n";
                    tbResume.Text += $"Pot: {_scrapeResult.Pot}\r\n";
                    tbResume.Text += $"*** STATISTICS ***\r\n";
                    tbResume.Text += $"PotOdds: {potOddsResult.PotOddsPercentage.ToString()}% \r\n";
                    tbResume.Text += $"Equity: {potOddsResult.EquityPercentage.ToString()}% \r\n";
                    tbResume.Text += $"Should Call: {potOddsResult.ShouldCall} \r\n";
                    tbResume.Text += "*** HOLE CARDS ***\r\n";
                    tbResume.Text += $"Dealt to Hero [{_scrapeResult.U0CardFace0} {_scrapeResult.U0CardFace1}]\r\n";

                    foreach (TablePosition position in Enum.GetValues(typeof(TablePosition)))
                    {
                        if (position != TablePosition.None && position <= _scrapeResult.P0Position)
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

                            if (!string.IsNullOrWhiteSpace(action))
                                tbResume.Text += $"{name}: {action}\r\n";
                        }
                    }

                    _isPreflop = false;
                }

                if (_isFlop)
                {
                    tbResume.Text += "*** FLOP *** [";
                    var countFlop = 0;
                    foreach (var carta in _scrapeResult.DataBoard.Where(w => w.Position == BoardPosition.Flop))
                    {
                        countFlop++;
                        if (countFlop == 3)
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

            if (_frmOverlay != null)
                _frmOverlay.UpdateAction(_responseAction?.Action ?? string.Empty);
        }

        private void SetBetPlayer()
        {
            using var binaryImage = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), _pictureUmbralBet));
            var regionTableMap = _regionsTableMap.FirstOrDefault(f => f.Id == "Bets");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImagen.Image == null)
                return;

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "bet");
                if (playerNumber == null) continue;

                var betValue = SetBetValue(region.PosX, region.PosY, region.Width, region.Height, region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);

                if (playerNumber == 0)
                {
                    _scrapeResult.U0Bet = betValue;
                    continue;
                }

                var player = _scrapeResult.DataPlayer.First(f => f.Name == $"P{playerNumber}");
                //if (IsValidBetPosition(player))
                //{
                    player.Bet = betValue;
                //}
            }
        }

        

        private void SetEmptyPlayer()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Empty");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImagen.Image == null)
                return;

            var bitmap = new Bitmap(_formImage.pbImagen.Image);

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "empty");
                if (playerNumber == null)
                    continue;

                var color = bitmap.GetPixel(region.PosX, region.PosY);
                
                _scrapeResult.DataPlayer.Add(CreatePlayerData(playerNumber.Value));
               
                // Verificamos si el jugador está vacío
                if (region.Name.Contains("empty") && _colorEmpty.Contains(color.B))
                {
                    var player = _scrapeResult.DataPlayer.First(n => n.Name == $"P{playerNumber}");
                    player.Empty = true;
                    player.SitOut = true;
                }
            }

            bitmap.Dispose(); // Liberamos recursos
        }

        private PlayerData CreatePlayerData(int playerNumber) =>
            new PlayerData
            {
                Name = $"P{playerNumber}",
                Active = false,
                Empty = false,
                SitOut = false,
                ValuePosition = playerNumber
            };

        private int? GetPlayerNumber(string regionName, string extraText = "")
        {
            if (string.IsNullOrEmpty(regionName))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(regionName, @$"p(\d+){extraText}");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        private void SetPotValue()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Table");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImagen.Image == null)
                return;

            var regionPot = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "pot");
            if (regionPot != null)
            {
                decimal potValue = 0;
                var pot = SetTextOCR(regionPot.PosX, regionPot.PosY, regionPot.Width, regionPot.Height, regionPot.Umbral, regionPot.InactiveUmbral, regionPot.IsOnlyNumber);

                if (pot.Length == 4)
                    if (!pot.Contains(".") && !pot.Contains(","))
                        decimal.TryParse(pot.Substring(0, 2) + "," + pot.Substring(2), out potValue);
                    else
                        potValue = decimal.Parse(pot);

                var sumBets = _scrapeResult.DataPlayer.Sum(s => s.Bet);
                //_scrapeResult.Pot = sumBets != 0 && potValue != sumBets ? sumBets : potValue;
                _scrapeResult.Pot = potValue;

            }

        }

        private void SetTableHand()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Table");
            if (regionTableMap != null)
            {
                var regionTableHand = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "tablehand");
                if (regionTableHand != null)
                {
                    if (string.IsNullOrEmpty(_tableHand))
                        _tableHand = SetTextOCR(regionTableHand.PosX, regionTableHand.PosY, regionTableHand.Width, regionTableHand.Height, regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber);
                    else
                    {
                        long.TryParse(_tableHand, out long oldTableHand);
                        long.TryParse(SetTextOCR(regionTableHand.PosX, regionTableHand.PosY, regionTableHand.Width, regionTableHand.Height, regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber), out long newTableHand);

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

                //var regionPot = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "pot");
                //if (regionPot != null)
                //{
                //    decimal potValue;
                //    if (decimal.TryParse(SetTextOCR(regionPot.PosX, regionPot.PosY, regionPot.Width, regionPot.Height, regionPot.Umbral, regionPot.InactiveUmbral, regionPot.IsOnlyNumber), out potValue))
                //    {
                //        var sumBets = _scrapeResult.DataPlayer.Sum(s => s.Bet);
                //        _scrapeResult.Pot = potValue != _scrapeResult.DataPlayer.Sum(s => s.Bet) ? _scrapeResult.DataPlayer.Sum(s => s.Bet) : potValue;
                //    }
                //}
            }
        }

        private void SetDealerPlayer()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Dealer");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImagen.Image == null)
                return;

            using var bitmap = new Bitmap(_formImage.pbImagen.Image);
            var emptyPositions = _scrapeResult.DataPlayer
                .Where(w => w.Empty || w.SitOut)
                .Select(s => s.ValuePosition)
                .ToList();

            foreach (var region in regionTableMap.Regions.Where(x => x.IsColor.GetValueOrDefault()))
            {
                var color = bitmap.GetPixel(region.PosX, region.PosY);
                if (!_colorDealer.Contains(color.R))
                    continue;

                var playerNumber = GetPlayerNumber(region.Name, "dealer");
                if (playerNumber == null)
                    continue;

                SetDealerForPlayer(playerNumber.Value, emptyPositions);
            }
        }

        private void SetDealerForPlayer(int playerNumber, List<int> emptyPositions)
        {
            // Para P0 (caso especial)
            if (playerNumber == 0)
            {
                _scrapeResult.P0Dealer = true;
                _scrapeResult.P0Position = TablePosition.Button;
                return;
            }

            // Actualizar estado del jugador
            var player = _scrapeResult.DataPlayer.First(n => n.Name == $"P{playerNumber}");
            player.Dealer = true;
            player.Empty = false;

            // Determinar posición P0 basado en la posición del dealer y asientos vacíos
            _scrapeResult.P0Position = DetermineP0Position(playerNumber, emptyPositions);
        }

        private TablePosition DetermineP0Position(int dealerPosition, List<int> emptyPositions)
        {
            var positionMap = new Dictionary<int, (TablePosition defaultPosition, Dictionary<int, TablePosition> emptyPositions)>
            {
                { 1, (TablePosition.CutOff, new Dictionary<int, TablePosition>()) },
                { 2, (TablePosition.Middle, new Dictionary<int, TablePosition> {
                    { 1, TablePosition.CutOff },
                    { 2, TablePosition.Early }
                })},
                { 3, (TablePosition.Early, new Dictionary<int, TablePosition> {
                    { 1, TablePosition.Middle },
                    { 2, TablePosition.CutOff }
                })},
                { 4, (TablePosition.BigBlind, new Dictionary<int, TablePosition> {
                    { 1, TablePosition.SmallBlind }
                })},
                { 5, (TablePosition.SmallBlind, new Dictionary<int, TablePosition>()) }
            };

            if (!positionMap.TryGetValue(dealerPosition, out var positionInfo))
                return TablePosition.None;

            // Contar asientos vacíos relevantes
            var relevantEmptySeats = emptyPositions.Count(pos =>
                dealerPosition < 4 ? pos < dealerPosition : pos > dealerPosition);

            // Si hay una regla específica para el número de asientos vacíos, úsala
            if (positionInfo.emptyPositions.TryGetValue(relevantEmptySeats, out var specialPosition))
                return specialPosition;

            // Si no hay regla específica, usar la posición por defecto
            return positionInfo.defaultPosition;
        }

        private void SetSitOutPlayer()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(f => f.Id == "SitOut");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImagen.Image == null)
                return;

            var colorSitOutMap = new Dictionary<string, int>
            {
                {"p1sitout", 0},
                {"p2sitout", 0},
                {"p3sitout", 2},
                {"p4sitout", 1},
                {"p5sitout", 1}
            };

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "sitout");
                if (playerNumber == null) continue;

                var player = _scrapeResult.DataPlayer.First(f => f.Name == $"P{playerNumber}");
                var colorIndex = colorSitOutMap[region.Name];

                if (!player.Empty && !player.Active &&
                    SetTextOCR(region.PosX, region.PosY, region.Width, region.Height, region.Umbral, region.InactiveUmbral, region.IsOnlyNumber).Contains("SIT"))
                {
                    player.SitOut = true;
                    player.Empty = true;
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

                if (item.Position == TablePosition.Button && item.Active)
                {
                    _scrapeResult.U0InPosition = false;
                    break;
                }

            }

            if (_scrapeResult.P0Position == TablePosition.BigBlind && (_scrapeResult.DataPlayer.Any(a => a.Active && a.Position != TablePosition.SmallBlind)))
                _scrapeResult.U0InPosition = false;

            if (_scrapeResult.P0Position == TablePosition.SmallBlind)
                _scrapeResult.U0InPosition = false;

            if (_scrapeResult.P0Position == TablePosition.BigBlind && (_scrapeResult.DataPlayer.Where(w => w.Active).Count() == 1 && _scrapeResult.DataPlayer.First(w => w.Active).Position == TablePosition.SmallBlind))
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

        private string GetTextSitOutByPosition(int x, int y, int width, int height, int umbral)
        {
            var imgSitOut = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImagen.Image), umbral));

            var tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            var ocrengine = new TesseractEngine(Path.Combine(tessdataPath, "tessdata"), "eng", EngineMode.Default);
            Rect area = new Rect(x, y, width, height);

            var res = ocrengine.Process(imgSitOut, area, PageSegMode.Auto);

            //if (umbral == 168)
            //{
            //    pictureBox1.Image = CaptureWindowsHelper.BinaryImage(_getCropImageUseCase.Execute(new GetCropImageUseCaseRequest { Source = new Bitmap(_formImage.pbImagen.Image), Section = new Rectangle(x, y, width, height) }).Image, umbral);

            //    label4.Text = res.GetText().Trim();
            //}

            return res.GetText().Trim();
        }

        private Dictionary<TablePosition, Dictionary<TablePosition, decimal>> GetPreflopHeroPosition()
        {
            Dictionary<TablePosition, decimal> players = new Dictionary<TablePosition, decimal>();

            foreach (var item in _scrapeResult.DataPlayer)
            {
                if (!item.Empty || !item.SitOut)
                {
                    if (item.Position != TablePosition.None)
                        players[item.Position] = item.Bet;
                }
            }

            //Comprobar si llega la mano sin subir
            var position = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();

            foreach (var heroPosition in Enum.GetValues(typeof(TablePosition)).Cast<TablePosition>())
            {
                if (heroPosition != TablePosition.None)
                    position[heroPosition] = new Dictionary<TablePosition, decimal>(players);
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

        private void SetVillainPosition(TablePosition p0Position)
        {
            //string[] playerNames = { "P1", "P2", "P3", "P4", "P5" };
            var playerNames = _scrapeResult.DataPlayer.ToList();
            List<TablePosition> positions = new List<TablePosition>();

            if (playerNames != null)
            {
                switch (p0Position)
                {
                    case TablePosition.BigBlind:
                        positions.AddRange(new List<TablePosition> { TablePosition.Early, TablePosition.Middle, TablePosition.CutOff, TablePosition.Button, TablePosition.SmallBlind });
                        SetVillainPositionExtension(playerNames!, positions);
                        break;
                    case TablePosition.SmallBlind:
                        positions.AddRange(new List<TablePosition> { TablePosition.BigBlind, TablePosition.Early, TablePosition.Middle, TablePosition.CutOff, TablePosition.Button });
                        SetVillainPositionExtension(playerNames!, positions);
                        break;
                    case TablePosition.Button:
                        positions.AddRange(new List<TablePosition> { TablePosition.SmallBlind, TablePosition.BigBlind, TablePosition.Early, TablePosition.Middle, TablePosition.CutOff });
                        SetVillainPositionExtension(playerNames!, positions);
                        break;
                    case TablePosition.CutOff:
                        positions.AddRange(new List<TablePosition> { TablePosition.Button, TablePosition.SmallBlind, TablePosition.BigBlind, TablePosition.Early, TablePosition.Middle });
                        SetVillainPositionExtension(playerNames!, positions);
                        break;
                    case TablePosition.Middle:
                        positions.AddRange(new List<TablePosition> { TablePosition.CutOff, TablePosition.Button, TablePosition.SmallBlind, TablePosition.BigBlind, TablePosition.Early });
                        SetVillainPositionExtension(playerNames!, positions);
                        break;
                    case TablePosition.Early:
                        positions.AddRange(new List<TablePosition> { TablePosition.Middle, TablePosition.CutOff, TablePosition.Button, TablePosition.SmallBlind, TablePosition.BigBlind });
                        SetVillainPositionExtension(playerNames!, positions);
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetVillainPositionExtension(List<PlayerData>? players, List<TablePosition> positions)
        {
            List<string> playerNames = new List<string> { "P1", "P2", "P3", "P4", "P5" };
            foreach (var position in positions)
            {
                foreach (var player in players!)
                {
                    //var player = _scrapeResult.DataPlayer.FirstOrDefault(n => n.Name == name);

                    //if(player != null && player.Empty)
                    //    break;

                    if (playerNames.Contains(player.Name) && player.Empty)
                    {
                        playerNames.Remove(player.Name);
                        if(position != TablePosition.SmallBlind && position != TablePosition.BigBlind)
                            break;
                    }

                    if (player != null && (!player.Empty || !player.SitOut) && player.Position == TablePosition.None)
                    {
                        player.Position = position;
                        break;
                    }
                }
            }
        }

        private async Task ObtainCardsPlayer()
        {
            var session = _dataBase.LightweightSession();
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "User");
            if (regionTableMap != null && regionTableMap.Regions != null)
            {
                foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
                {
                    var imageToBase64 = _imageCropperService.CropImageToBase64(_formImage.pbImagen.Image, region.PosX, region.PosY, region.Width, region.Height);

                    if (_cardsImages == null)
                    {
                        _cardsImages = [];
                        var cards = await session.Query<Card>().ToListAsync();
                        foreach (var item in cards)
                        {
                            _cardsImages.Add(item.ToDto());
                        }
                    }

                    //await Task.Run(async () => await _cardUseCases.GetAllCards.ExecuteAsync());

                    if (_cardsImages != null)
                    {
                        var maxPorcentaje = 0.0;
                        var card = new CardDTO { Name = string.Empty };

                        foreach (var item in _cardsImages)
                        {
                            if (!string.IsNullOrEmpty(item.ImageBase64))
                            {
                                var pocentaje = _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64);

                                if (pocentaje > maxPorcentaje)
                                {
                                    maxPorcentaje = pocentaje;
                                    card = item;
                                }
                            }
                        }

                        switch (region.Name)
                        {
                            case "u0cardface0":
                                _scrapeResult.U0CardFace0 = card.Name.Split(" ")[0];
                                _scrapeResult.U0CardForce0 = card.Force;
                                _scrapeResult.U0CardSuit0 = card.Suit;
                                break;
                            case "u0cardface1":
                                _scrapeResult.U0CardFace1 = card.Name.Split(" ")[0];
                                _scrapeResult.U0CardForce1 = card.Force;
                                _scrapeResult.U0CardSuit1 = card.Suit;
                                break;
                            default:
                                break;
                        }

                    }
                }
            }
        }

        private decimal SetBetValue(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            var ocr = new OcrResult();

            ocr = _ocrService.ExtractTextFromRegionAndDebug(
                        _formImage.pbImagen.Image,
                        posX,
                        posY,
                        width,
                        height,
                        umbral.GetValueOrDefault(),
                        isOnlyNumber.GetValueOrDefault());

            if (string.IsNullOrEmpty(ocr.Text))
            {
                ocr = _ocrService.ExtractTextFromRegionAndDebug(
                        _formImage.pbImagen.Image,
                        posX,
                        posY,
                        width,
                        height,
                        inactiveUmbral.GetValueOrDefault(),
                        isOnlyNumber.GetValueOrDefault());
            }

            decimal bet;
            decimal.TryParse(ocr.Text, out bet);
            return bet;
        }

        private string SetTextOCR(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            var ocr = new OcrResult();

            ocr = _ocrService.ExtractTextFromRegionAndDebug(
                        _formImage.pbImagen.Image,
                        posX,
                        posY,
                        width,
                        height,
                        umbral.GetValueOrDefault(),
                        isOnlyNumber.GetValueOrDefault());

            if (string.IsNullOrEmpty(ocr.Text))
            {
                ocr = _ocrService.ExtractTextFromRegionAndDebug(
                        _formImage.pbImagen.Image,
                        posX,
                        posY,
                        width,
                        height,
                        inactiveUmbral.GetValueOrDefault(),
                        isOnlyNumber.GetValueOrDefault());
            }

            return ocr.Text ?? string.Empty;
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

                _frmOverlay = new FrmOverlay();
                _frmOverlay.Location = new Point(windowRect.left + (((windowRect.right - windowRect.left) / 2) - (_frmOverlay.Size.Width / 2)), windowRect.bottom - 150);
                _frmOverlay.Show();

                //_formAction = new FormAction();
                //_formAction.Location = new Point(windowRect.left + (((windowRect.right - windowRect.left) / 2) - (_formAction.Size.Width / 2)), windowRect.bottom - 50);
                //_formAction.Show();
            }

            if (_handle != IntPtr.Zero)
            {
                User32.GetWindowRect(_handle, ref windowRect);
                if (windowRect.left != _locWindowRect.left || windowRect.right != _locWindowRect.right || windowRect.top != _locWindowRect.top || windowRect.bottom != _locWindowRect.bottom)
                {
                    _locWindowRect = windowRect;
                    this.Invoke((MethodInvoker)delegate
                    {
                        _frmOverlay.Location = new Point(windowRect.left + (((windowRect.right - windowRect.left) / 2) - (_frmOverlay.Size.Width / 2)), windowRect.bottom - 50);
                    });
                }
            }

            if (!_backgroundExecute)
                backgroundWorker1.RunWorkerAsync();

        }


        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            _backgroundExecute = true;

            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(_handle, ref windowRect);

            while (true)
            {
                if (_frmOverlay != null && !_frmOverlay.Visible)
                {
                    e.Cancel = true;
                    return;
                }

                var img = _useCase.Execute(_handle);

                var regionAction = _regionsTableMap.FirstOrDefault(f => f.Id == "User")?.Regions?.First(x => x.Name == "uAction");
                Color colorAction = new Bitmap(img).GetPixel(regionAction.PosX, regionAction.PosY);

                var flop = _regionsTableMap.FirstOrDefault(f => f.Id == "Table")?.Regions?.First(x => x.Name == "isFlop");
                Color colorFlop = new Bitmap(img).GetPixel(flop.PosX, flop.PosY);

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


        private void SetBoardValues()
        {
            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace0))
                pbCard0.Image = _scrapeResult.U0CardFace0 != null ? _imageCropperService.Base64ToImage(_cards.FirstOrDefault(x => x.Id.Contains(_scrapeResult.U0CardFace0))?.ImageBase64 ?? string.Empty) : null;
            else
                pbCard0.Image = null;

            if (!string.IsNullOrWhiteSpace(_scrapeResult.U0CardFace1))
                pbCard1.Image = _scrapeResult.U0CardFace0 != null ? _imageCropperService.Base64ToImage(_cards.FirstOrDefault(x => x.Id.Contains(_scrapeResult.U0CardFace1))?.ImageBase64 ?? string.Empty) : null;
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
        }

        #region Executes

        private void btnPlusWidth_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { Width = _selectedRegion.Width + _speed };
                _selectedRegion = updatedRegion;
                tbWidth.Text = _selectedRegion.Width.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);

                _selectedRegion = updatedRegion;
            }

            _img = _formImage.pbImagen.Image;
        }

        private void btnMinusWidth_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { Width = _selectedRegion.Width - _speed };
                _selectedRegion = updatedRegion;
                tbWidth.Text = _selectedRegion.Width.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);

                _selectedRegion = updatedRegion;
            }

            _img = _formImage.pbImagen.Image;
        }

        private void btnPlusHeight_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { Height = _selectedRegion.Height + _speed };
                _selectedRegion = updatedRegion;
                tbHeight.Text = _selectedRegion.Height.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);

                _selectedRegion = updatedRegion;
            }

            _img = _formImage.pbImagen.Image;
        }

        private void btnMinusHeight_Click(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { Height = _selectedRegion.Height - _speed };
                _selectedRegion = updatedRegion;
                tbHeight.Text = _selectedRegion.Height.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);

                _selectedRegion = updatedRegion;
            }

            _img = _formImage.pbImagen.Image;
        }


        #endregion

        #region Movimiento Region

        private void btnRigth_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosX = _selectedRegion.PosX + _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;

                _img = _formImage.pbImagen.Image;
            }
        }

        private void btnLeft_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosX = _selectedRegion.PosX - _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;

                _img = _formImage.pbImagen.Image;
            }
        }

        private GetRGBColorResponse GetColorResponse()
        {
            var rgbRequest = new GetRGBColorRequest
            {
                Image = (Bitmap)_formImage.pbImagen.Image,
                X = _selectedRegion.PosX,
                Y = _selectedRegion.PosY,
                IsColor = _selectedRegion.IsColor.GetValueOrDefault()
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (_selectedRegion.IsColor.GetValueOrDefault())
            {
                tbColor.Text = rgbResponse.RColor + rgbResponse.GColor + rgbResponse.BColor;
            }

            return rgbResponse;
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosY = _selectedRegion.PosY + _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;

                _img = _formImage.pbImagen.Image;
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosY = _selectedRegion.PosY - _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;

                _img = _formImage.pbImagen.Image;
            }
        }

        private void btnUpLeft_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosY = _selectedRegion.PosY - _speed, PosX = _selectedRegion.PosX - _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;
                _img = _formImage.pbImagen.Image;
            }
        }

        private void btnUpRight_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosY = _selectedRegion.PosY - _speed, PosX = _selectedRegion.PosX + _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;
                _img = _formImage.pbImagen.Image;
            }
        }

        private void btnDownLeft_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosY = _selectedRegion.PosY + _speed, PosX = _selectedRegion.PosX - _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;
                _img = _formImage.pbImagen.Image;
            }
        }

        private void btnDownRight_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null)
            {
                _formImage.pbImagen.Refresh();

                _papel = _formImage.pbImagen.CreateGraphics();
                Pen lapiz = new Pen(Color.Red);
                GetRGBColorResponse rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with { PosY = _selectedRegion.PosY + _speed, PosX = _selectedRegion.PosX + _speed };
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                if (updateRegion.IsColor.GetValueOrDefault())
                    updateRegion = updateRegion with { Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}" };

                _selectedRegion = updateRegion;
                _img = _formImage.pbImagen.Image;
            }
        }



        private void tbWidth_Leave(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { Width = int.Parse(tbWidth.Text) };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImagen.Image;
        }

        private void tbHeight_Leave(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { Height = int.Parse(tbHeight.Text) };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImagen.Image;
        }

        private void tbX_Leave(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { PosX = int.Parse(tbX.Text) };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImagen.Image;
        }

        private void tbY_Leave(object sender, EventArgs e)
        {
            _formImage.pbImagen.Refresh();

            _papel = _formImage.pbImagen.CreateGraphics();
            Pen lapiz = new Pen(Color.Red);

            if (_selectedRegion != null)
            {
                var updatedRegion = _selectedRegion with { PosY = int.Parse(tbY.Text) };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImagen.Image;
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

        private void twTables_DoubleClick(object sender, EventArgs e)
        {
            if (sender is TreeView treeView && treeView.SelectedNode != null)
            {
                var manos = ObtenerManosActionNodo(treeView.SelectedNode);
                var manosOrder = manos?.OrderByDescending(o => o.Name).ToList();
                dgvHands.DataSource = manosOrder;
            }
        }

        private List<Domain.ValueObjects.Hand> ObtenerManosActionNodo(TreeNode selectedNode)
        {
            // Solo procesar si es un nodo hoja (último nivel)
            if (selectedNode.Nodes.Count == 0 &&
                selectedNode.Parent != null &&
                selectedNode.Parent.Parent != null)
            {
                // Obtener el nombre de la posición (nivel actual)
                string positionName = selectedNode.Text;

                // Obtener el HeroPosition (nivel padre)
                string heroPosition = selectedNode.Parent.Text;

                // Obtener el nombre de la tabla (nivel raíz)
                string tableName = selectedNode.Parent.Parent.Text;

                // Buscar la tabla correspondiente
                var table = _dataTables?.FirstOrDefault(f => f.Id == tableName);
                if (table != null)
                {
                    // Buscar la posición específica
                    var position = table.Positions?
                        .FirstOrDefault(p =>
                            p.Name == positionName &&
                            p.HeroPosition == heroPosition);

                    return position?.Hands?.ToList() ?? new List<Domain.ValueObjects.Hand>();
                }
            }

            return new List<Domain.ValueObjects.Hand>();
        }

        private void twTables_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode nodeToExpand = e.Node;

            // Obtener todos los nodos del mismo nivel
            IEnumerable<TreeNode> siblingNodes;

            if (nodeToExpand.Parent == null)
            {
                // Nodos raíz
                siblingNodes = twTables.Nodes.Cast<TreeNode>()
                    .Where(node => node != nodeToExpand);
            }
            else
            {
                // Nodos hijo
                siblingNodes = nodeToExpand.Parent.Nodes.Cast<TreeNode>()
                    .Where(node => node != nodeToExpand);
            }

            // Colapsar todos los nodos hermanos que estén expandidos
            foreach (TreeNode sibling in siblingNodes)
            {
                if (sibling.IsExpanded)
                {
                    sibling.Collapse();
                }
            }
        }

        private void btnTestColor_Click(object sender, EventArgs e)
        {
            if (_formImage.pbImagen.Image != null)
            {
                var color = _colorDetectionService.GetPixelColor(_formImage.pbImagen.Image, _selectedRegion!.PosX, _selectedRegion.PosY);
                pbColorDebug.BackColor = Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
                tbTestColor.Text = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
            }
        }

        private void SetPictureBoxColor(string hexColor)
        {
            if (!string.IsNullOrEmpty(hexColor))
            {
                // Asegurarse que el valor hex tenga el formato correcto
                hexColor = hexColor.Replace("#", "");

                Color color = ColorTranslator.FromHtml("#" + hexColor);
                using (Bitmap bmp = new Bitmap(pbRegionColor.Width, pbRegionColor.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(color);
                    }
                    pbRegionColor.Image = new Bitmap(bmp);
                }
            }
        }

        private void btnTestTexto_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null && _formImage.pbImagen.Image != null)
            {
                var ocr = new OcrResult();

                ocr = _ocrService.ExtractTextFromRegionAndDebug(
                            _formImage.pbImagen.Image,
                            _selectedRegion.PosX,
                            _selectedRegion.PosY,
                            _selectedRegion.Width,
                            _selectedRegion.Height,
                            _selectedRegion.Umbral.GetValueOrDefault(),
                            _selectedRegion.IsOnlyNumber.GetValueOrDefault());

                if (string.IsNullOrEmpty(ocr.Text))
                {
                    ocr = _ocrService.ExtractTextFromRegionAndDebug(
                            _formImage.pbImagen.Image,
                            _selectedRegion.PosX,
                            _selectedRegion.PosY,
                            _selectedRegion.Width,
                            _selectedRegion.Height,
                            _selectedRegion.InactiveUmbral.GetValueOrDefault(),
                            _selectedRegion.IsOnlyNumber.GetValueOrDefault());
                }

                tbTestTexto.Text = !string.IsNullOrEmpty(ocr.Text) ? ocr.Text : "Sin resultado";
                pictureBox1.Image = ocr.Image;
            }
        }

        private async void btnTestCarta_Click(object sender, EventArgs e)
        {
            if (_selectedRegion != null && _formImage.pbImagen.Image != null)
            {
                var imageToBase64 = _imageCropperService.CropImageToBase64(_formImage.pbImagen.Image, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);

                if (_cardsImages == null)
                    _cardsImages = await Task.Run(async () => await _cardUseCases.GetAllCards.ExecuteAsync());

                if (_cardsImages != null)
                {
                    var maxPorcentaje = 0.0;

                    foreach (var item in _cardsImages)
                    {

                        if (!string.IsNullOrEmpty(item.ImageBase64))
                        {
                            var pocentaje = _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64);

                            if (pocentaje > maxPorcentaje)
                            {
                                maxPorcentaje = pocentaje;
                                pbTestCarta.Image = _imageCropperService.Base64ToImage(item.ImageBase64);
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("No se ha seleccionado una región o la imagen es nula.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}