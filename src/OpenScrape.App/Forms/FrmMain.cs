using JasperFx.Core;
using Marten;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Helpers.FlopHelper;
using OpenScrape.App.Helpers.FlopHelper.RaiseOverLimper;
using OpenScrape.App.Helpers.MLHelper;
using OpenScrape.App.Models;
using OpenScrape.App.Services;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Mappers;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.Features.ActionScenario;
using OpenScrape.Features.Card;
using OpenScrape.Features.RegionsTableMap;
using OpenScrape.Features.RegionsTableMap.Update;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using static OpenScrape.App.Helpers.CaptureWindowsHelper;
using static OpenScrape.DecisionMaker.Services.EquityCalculatorService;
using Image = System.Drawing.Image;
using System.Drawing.Drawing2D;

namespace OpenScrape.App
{
    /// <summary>
    /// Formulario principal de la aplicación OpenScrape para análisis de mesas de póker
    /// </summary>
    public partial class FrmMain : Form, IDisposable
    {
        #region [Constants]
        private const string DEFAULT_RESOURCES_PATH = @"C:\Code\Poker\ScrapePoker\resources";
        #endregion

        #region [Forms]
        FormImage _formImage;
        Graphics _papel;
        FormAction _formAction;
        FrmOverlay _frmOverlay;
        #endregion

        #region [Regions]
        List<Regions> _regions = new List<Regions>();
        List<ImageRegion> _images = new List<ImageRegion>();
        #endregion

        #region [Private Fields]
        private List<Card> _cards = new();
        private RadioButton? _lastChecked;
        private Image? _img;
        private PlayerGameState _playerGameState = new();
        private TableScrapeFlopResult _scrapeFlopResult = new();
        private ResponseAction _responseAction = new();
        private int _speed = 1;
        private string _folderPath = string.Empty;
        private string _tableHand = string.Empty;
        private List<RegionTableMap>? _regionsTableMap;
        private Domain.ValueObjects.Region? _selectedRegion;
        private readonly string _pathResume;
        private readonly List<int> _colorDealer = new() { 250, 251, 252, 253, 254, 255 };
        private readonly List<int> _colorEmpty = new() { 14, 15, 53, 59, 74 }; //, 41, 42, 43, 44, 45, 46, 47, 48, 49, 57, 66, 67, 68, 69 };
        private readonly List<int> _colorPlaying = new() { 33 };
        private Dictionary<TablePosition, Dictionary<TablePosition, decimal>> _preflopHeroPosition = new();
        private int _pictureUmbralBet = 130;
        private string _session = string.Empty;
        private IntPtr _handle;
        private User32.RECT _locWindowRect = new();
        private bool _executeCapture;
        private bool _isPreflop = true;
        private bool _isFlop;
        private bool _isTurn;
        private bool _isRiver;
        private string _tableName = string.Empty;
        private long _newTableHand;
        private bool _newHand;
        private bool _backgroundExecute;
        private IReadOnlyList<Table>? _tables;
        private List<Table>? _dataTables;
        private readonly CancellationTokenSource _cancellationTokenSource = new(); // MOSTRAR CAMBIOS: Añadido para gestionar cancelación
        private PokerHandEvaluator _handEvaluator = new();

        private readonly ConcurrentDictionary<string, object> _playerCache = new();
        #endregion

        #region [Services and UseCases]
        private readonly GetWindowsScreenUseCase _useCase = new();
        private readonly ActionScenarioUseCases _actionScenarioUseCases;
        private readonly RegionTableMapUseCases _regionTableMapUseCases;
        private readonly ISetPreflopActionUseCase _setPreflopActionUseCase;
        private readonly ImageCropperService _imageCropperService = new();
        private List<CardDTO>? _cardsImages;
        private readonly IDocumentStore _dataBase;
        private IDocumentSession _sessionDB;
        private readonly ISetFlopForceBoardUseCase _setFlopForceBoardUseCase = new SetFlopForceBoardUseCase();
        private static readonly IGetHashImageUseCase _getHashImageUseCase = new GetHashImageUseCase();
        private static readonly IGetCropImageUseCase _getCropImageUseCase = new GetCropImageUseCase();
        private readonly IGetCardsFlopUseCase _getCardsFlopUseCase;
        private readonly IGetCardsTurnUseCase _getCardsTurnUseCase;
        private readonly IGetCardsRiverUseCase _getCardsRiverUseCase;
        private readonly IOutsCalculatorUseCase _outsCalculatorUseCase = new OutsCalculatorUseCase();
        private readonly IPokerCalculator _pokerCalculator;
        private readonly ColorDetectionService _colorDetectionService = new();
        private readonly OcrService _ocrService = new();
        private readonly CardUseCases _cardUseCases;
        #endregion

        /// <summary>
        /// Constructor del formulario principal
        /// </summary>
        public FrmMain(IDocumentStore dataBase,
                        ActionScenarioUseCases actionScenarioUseCases,
                        CardUseCases cardUseCases,
                        RegionTableMapUseCases regionTableMapUseCases,
                        IPokerCalculator pokerCalculator)
        {
            InitializeComponent();

            // NUEVO: Aplicar estilos visuales ANTES de la inicialización
            //InitializeVisualStyles();

            // Inicialización existente...
            _dataBase = dataBase ?? throw new ArgumentNullException(nameof(dataBase));
            _actionScenarioUseCases = actionScenarioUseCases ?? throw new ArgumentNullException(nameof(actionScenarioUseCases));
            _regionTableMapUseCases = regionTableMapUseCases ?? throw new ArgumentNullException(nameof(regionTableMapUseCases));
            _cardUseCases = cardUseCases ?? throw new ArgumentNullException(nameof(cardUseCases));

            // Resto de inicialización existente...
            _session = GenerateRandomNumbers();
            _sessionDB = _dataBase.LightweightSession();
            _lastChecked = new RadioButton();

            _setPreflopActionUseCase = new SetPreflopActionUseCase(_actionScenarioUseCases);
            _getCardsFlopUseCase = new GetCardsFlopUseCase(_dataBase);
            _getCardsTurnUseCase = new GetCardsTurnUseCase(_dataBase);
            _getCardsRiverUseCase = new GetCardsRiverUseCase(_dataBase);

            _pathResume = Path.Combine(DEFAULT_RESOURCES_PATH,
                $"resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt");
        }

        /// <summary>
        /// Evento de carga del formulario
        /// </summary>
        private async void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                _formImage = new FormImage();
                _frmOverlay = new FrmOverlay();
                cbSpeed.SelectedIndex = 0;

                await LoadRegionTableMapAsync();
                await LoadTablesAsync(_sessionDB);

                var allCards = await _sessionDB.Query<Card>().ToListAsync();
                _cards.AddRange(allCards);

                _formImage.Location = new Point(Width, Location.Y);
                _formImage.Show();
            }
            catch (Exception ex)
            {
                LogError($"Error al cargar el formulario: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga la configuración de regiones de la tabla desde la base de datos
        /// </summary>
        private async Task LoadRegionTableMapAsync()
        {
            try
            {
                var regions = new List<Domain.ValueObjects.Region>();
                var regionsTableMap = await _sessionDB.Query<RegionTableMap>().ToListAsync();

                var categories = regionsTableMap
                    .Where(x => x.Regions != null)
                    .SelectMany(x => x.Regions!)
                    .Distinct()
                    .ToList();

                regions.AddRange(categories);
                _regionsTableMap = regionsTableMap.ToList();

                LoadTreeViewRegions(regionsTableMap);
            }
            catch (Exception ex)
            {
                LogError($"Error al cargar regiones: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga las tablas desde la base de datos
        /// </summary>
        private async Task LoadTablesAsync(IDocumentSession session)
        {
            try
            {
                _tables = await session.Query<Table>().ToListAsync();
                _dataTables = _tables.ToList();
                LoadTreeViewTables(_dataTables);
            }
            catch (Exception ex)
            {
                LogError($"Error al cargar tablas: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga el TreeView con las tablas disponibles
        /// </summary>
        private void LoadTreeViewTables(IReadOnlyList<Table> tables)
        {
            twTables.Nodes.Clear();

            foreach (var table in tables)
            {
                // Primer nivel - Name de la tabla
                TreeNode actionNode = twTables.Nodes.Add(table.Id, table.Id);

                if (table.Positions != null && table.Positions.Any())
                {
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
                                position.Name, // Identificador único
                                position.Name  // Texto a mostrar
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carga el TreeView con las regiones de configuración
        /// </summary>
        private void LoadTreeViewRegions(IReadOnlyList<RegionTableMap> categories)
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
                        regionsConfigNode.Nodes.Add(region.Name);
                    }
                }
            }
        }

        #region [Event Handlers]

        /// <summary>
        /// Handles double-click event on the regions TreeView.
        /// </summary>
        private void twRegions_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                EnableButtons();
                _formImage.pbImage.Refresh();

                // Colapsar nodos no relacionados
                if (twRegionsConfig?.SelectedNode?.Parent != null)
                {
                    foreach (TreeNode rootNode in twRegionsConfig.Nodes)
                    {
                        if (rootNode != twRegionsConfig?.SelectedNode?.Parent)
                        {
                            rootNode.Collapse();
                        }
                    }
                }

                if (twRegionsConfig?.SelectedNode != null)
                {
                    _selectedRegion = GetRegionFromNode(twRegionsConfig.SelectedNode);
                    if (_selectedRegion != null && _formImage.pbImage.Image != null)
                    {
                        UpdateRegionDisplay();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error selecting region: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza la visualización de la región seleccionada
        /// </summary>
        private void UpdateRegionDisplay()
        {
            using (var lapiz = new Pen(Color.Red))
            {
                _papel = _formImage.pbImage.CreateGraphics();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY,
                    _selectedRegion.Width, _selectedRegion.Height);
            }

            // Actualizar campos de texto con los valores de la región
            tbX.Text = _selectedRegion.PosX.ToString();
            tbY.Text = _selectedRegion.PosY.ToString();
            tbWidth.Text = _selectedRegion.Width.ToString();
            tbHeight.Text = _selectedRegion.Height.ToString();
            tbRegionName.Text = _selectedRegion.Name;
            tbColor.Text = _selectedRegion.Color != null ? _selectedRegion.Color.ToUpper() : string.Empty;
            tbRegionUmbral.Text = _selectedRegion.Umbral.ToString();
            tbRegionInactUmbral.Text = _selectedRegion.InactiveUmbral.ToString();

            // Actualizar checkboxes
            cbRegionColor.Checked = _selectedRegion.IsColor.GetValueOrDefault();
            cbRegionHash.Checked = _selectedRegion.IsHash.GetValueOrDefault();
            cbRegionBoard.Checked = _selectedRegion.IsBoard.GetValueOrDefault();
            cbRegionNumber.Checked = _selectedRegion.IsOnlyNumber.GetValueOrDefault();

            SetPictureBoxColor(_selectedRegion.Color != null ? _selectedRegion.Color.ToUpper() : string.Empty);

            // Habilitar botones según propiedades de la región
            btnTestColor.Enabled = _selectedRegion.IsColor.GetValueOrDefault();
            btnTestTexto.Enabled = _selectedRegion.Umbral != null;
            btnTestCarta.Enabled = _selectedRegion.IsHash.GetValueOrDefault();
        }

        /// <summary>
        /// Obtiene la región correspondiente al nodo seleccionado
        /// </summary>
        private Domain.ValueObjects.Region? GetRegionFromNode(TreeNode node)
        {
            return _regionsTableMap?
                .SelectMany(c => c.Regions ?? new List<Domain.ValueObjects.Region>())
                .FirstOrDefault(r => r.Name == node.Text);
        }

        /// <summary>
        /// Maneja el cambio en la velocidad de movimiento
        /// </summary>
        private void cbSpeed_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(cbSpeed.Text, out var speed))
            {
                _speed = speed;
            }
        }

        /// <summary>
        /// Guarda los cambios en la configuración de la región
        /// </summary>
        private async void btnSaveMap_Click(object sender, EventArgs e)
        {
            try
            {
                // Validación de entrada mejorada
                var umbral = string.IsNullOrEmpty(tbRegionUmbral.Text) ? "0" : tbRegionUmbral.Text;
                var inactUmbral = string.IsNullOrEmpty(tbRegionInactUmbral.Text) ? "0" : tbRegionInactUmbral.Text;

                if (!double.TryParse(umbral, out double regionUmbral) ||
                    !double.TryParse(inactUmbral, out double regionInactUmbral) ||
                    !int.TryParse(tbX.Text, out int x) ||
                    !int.TryParse(tbY.Text, out int y) ||
                    !int.TryParse(tbWidth.Text, out int width) ||
                    !int.TryParse(tbHeight.Text, out int height) ||
                    _selectedRegion == null)
                {
                    LogInformation($"Error de validación: {umbral}, {inactUmbral}, {tbX.Text}, {tbY.Text}, {tbWidth.Text}, {tbHeight.Text}");
                    return;
                }

                await Task.Run(async () =>
                {
                    await _regionTableMapUseCases.UpdateRegionTableMap.ExecuteAsync(
                        new UpdateRegionTableMapRequest(
                            _selectedRegion.Category,
                            _selectedRegion.Name,
                            x, y, width, height,
                            regionUmbral,
                            regionInactUmbral,
                            tbColor.Text,
                            cbRegionColor.Checked,
                            cbRegionHash.Checked,
                            cbRegionNumber.Checked,
                            cbRegionBoard.Checked
                        )
                    );
                });

                await LoadRegionTableMapAsync();
                LogInformation($"Región actualizada: {_selectedRegion.Name}");
            }
            catch (Exception ex)
            {
                LogError($"Error al guardar la región: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Captura y procesa la información de la mesa de póker
        /// </summary>
        private async void btnCapture_Click(object sender, EventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                lbAction.Text = string.Empty;
                _frmOverlay.UpdateEquityPercentage(string.Empty);
                _frmOverlay.UpdatePotOddsPercentage(string.Empty);
                _frmOverlay.UpdateShouldCall(null);
                _frmOverlay.UpdateTableName(_tableName);
                lbPositionAction.Text = string.Empty;
                _executeCapture = true;
                var potOddsResult = new PokerCalculationResult();

                if (!cbTest.Checked)
                {
                    _frmOverlay.UpdateAction(string.Empty);

                    if (cbMark.Checked)
                        CreateLogWithMarkedHands();

                    await GetImageWhilePlaying();
                    _formImage.WindowState = FormWindowState.Minimized;
                }

                if (cbTest.Checked)
                {
                    _isFlop = rbFlop.Checked;
                    _isTurn = rbTurn.Checked;
                    _isRiver = rbRiver.Checked;
                    _frmOverlay.Show();
                }

                SetTableHand();

                if (_newHand)
                {
                    _playerGameState = new PlayerGameState();
                    _responseAction = new ResponseAction();
                    _preflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
                    _newHand = false;
                    _isFlop = false;

                    if (!cbTest.Checked)
                        await HandleNewHandAsync();
                }

                if (_playerGameState.Players.Count() == 0)
                {
                    await InitializePlayersAsync();
                }

                SetActivePlayer();
                SetBetPlayer();

                // Procesar la información de la mesa
                await ProcessTableInfoAsync(potOddsResult);

                // Actualizar la interfaz con los resultados
                UpdateUIWithResults(potOddsResult);

                stopwatch.Stop();
                if (cbTest.Checked)
                {
                    tbResume.Text += $"\nProcessing time: {stopwatch.ElapsedMilliseconds} ms";
                }
            }
            catch (Exception ex)
            {
                LogError($"Error durante la captura: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicializa los datos de los jugadores
        /// </summary>
        private async Task InitializePlayersAsync()
        {
            try
            {
                await ObtainCardsPlayerAsync();
                SetEmptyPlayer();
                SetSitOutPlayer();
                //SetActivePlayer();
                SetDealerPlayer();
                //SetBetPlayer();
                SetVillainPosition(_playerGameState.Position);
                SetAliasVillain();
            }
            catch (Exception ex)
            {
                // Log del error específico
                LogError($"Error en InitializePlayersAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Procesa la información de la mesa
        /// </summary>
        private async Task ProcessTableInfoAsync(PokerCalculationResult potOddsResult)
        {

            SetPotValue();
            _preflopHeroPosition = GetPreflopHeroPosition();

            if (!_isFlop && !_isTurn && !_isRiver)
            {
                await ProcessPreflopAsync();
            }
            else
            {
                await ProcessPostFlopAsync(potOddsResult);
            }
        }

        /// <summary>
        /// Procesa la fase de preflop
        /// </summary>
        private async Task ProcessPreflopAsync()
        {
            _isPreflop = true;
            var responseFlop = await _setPreflopActionUseCase.Execute(new SetPreflopActionUseCaseRequest
            {
                ResponseAction = _responseAction,
                PlayerState = _playerGameState,
                PreflopHeroPosition = _preflopHeroPosition
            });

            _responseAction = responseFlop.ResponseAction;
            _playerGameState = responseFlop.PlayerState;
        }

        /// <summary>
        /// Procesa las fases posteriores al flop (flop, turn, river)
        /// </summary>
        private async Task ProcessPostFlopAsync(PokerCalculationResult potOddsResult)
        {
            if (_isFlop)
            {
                await ProcessFlopAsync(potOddsResult);
            }

            if (_isTurn)
            {
                await ProcessTurnAsync();
            }

            if (_isRiver)
            {
                await ProcessRiverAsync();
            }

            _responseAction.Action = string.IsNullOrEmpty(_responseAction.Action)
                ? "No Preflop action"
                : _responseAction.Action;

            _frmOverlay.UpdateAction(_responseAction.Action);
        }

        /// <summary>
        /// Procesa la fase de flop
        /// </summary>
        private async Task ProcessFlopAsync(PokerCalculationResult potOddsResult)
        {
            // Crear el servicio
            var equityService = new EquityCalculatorService(
                new MonteCarloSimulator(),
                new OutsCalculator(),
                new PreflopEquityCalculator());

            _isFlop = false;

            // Capturar cartas del flop
            using var bitmap = new Bitmap(_formImage.pbImage.Image);
            var flopResponse = await _getCardsFlopUseCase.ExecuteAsync(new GetCardsFlopUseCaseRequest
            {
                Image = bitmap,
                RegionsTableMap = _regionsTableMap
            });

            var dataBoard = flopResponse.DataBoard;
            _playerGameState.BoardCards = dataBoard;

            dataBoard.Add(new BoardData { Force = _playerGameState.HoleCard1Rank, Suit = _playerGameState.HoleCard1Suit, Position = BoardPosition.Hand, Name = _playerGameState.HoleCard1Face, Location = 0 });
            dataBoard.Add(new BoardData { Force = _playerGameState.HoleCard2Rank, Suit = _playerGameState.HoleCard2Suit, Position = BoardPosition.Hand, Name = _playerGameState.HoleCard2Face, Location = 0 });

            _handEvaluator.EvaluateHand(dataBoard);

            // Procesar el flop
            var setFlopForceBoardResponse = _setFlopForceBoardUseCase.Execute(
                new SetFlopForceBoardUseCaseRequest
                {
                    PlayerState = _playerGameState,
                    TableScrapeFlopResult = _scrapeFlopResult
                });

            _playerGameState = setFlopForceBoardResponse.PlayerState;
            _scrapeFlopResult = setFlopForceBoardResponse.TableScrapeFlopResult;

            // Calcular odds y actualizar overlay
            //potOddsResult = GetPotOddsCalculator();

            var myCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                new CardDataOuts((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
            };

            var communityCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)dataBoard[0].Suit, (Rank)dataBoard[0].Force),
                new CardDataOuts((Suit)dataBoard[1].Suit, (Rank)dataBoard[1].Force),
                new CardDataOuts((Suit)dataBoard[2].Suit, (Rank)dataBoard[2].Force)
            };

            var result = _pokerCalculator.Calculate(
                myCards,
                communityCards,
                _playerGameState.PotSize,
                _playerGameState.Players.Max(m => m.Bet),
                isInPosition: _playerGameState.IsInPosition,
                heroStack: 0,
                villainStack: 0,
                handSituation: _playerGameState.HandSituation.ToString());

            UpdateOverlayWithPotOdds(result);

            // Determinar si estamos en posición
            SetIsInPosition();

            // Analizar el flop y determinar acción
            DetermineFlopAction();
            //_responseAction.Action = analysis.RecommendedAction;
        }

        /// <summary>
        /// Actualiza el panel de métricas con los resultados del cálculo
        /// </summary>
        private void UpdateMetricsPanel(PokerCalculationResult result)
        {
            lblEV.Text = $"EV: {result.ExpectedValue:F2}";
            lblFoldEquity.Text = $"Fold Eq: {result.FoldEquity:F1}%";
            lblBetSize.Text = result.SuggestedBetSize.HasValue ? $"Bet: {result.SuggestedBetSize.Value:F1}x" : "Bet: N/A";
        }

        /// <summary>
        /// Determina la acción a tomar en el flop según la situación de la mano
        /// </summary>
        private void DetermineFlopAction()
        {
            var flopAnalyzerRequest = new FlopAnalyzerHelperReqest
            {
                PlayerState = _playerGameState,
                TableScrapeFlopResult = _scrapeFlopResult
            };

            switch (_playerGameState.HandSituation)
            {
                case HandSituation.OpenRaise:
                    HandleOpenRaiseFlopAction();
                    break;

                case HandSituation.Call:
                    HandleCallFlopAction();
                    break;

                case HandSituation.RaiseOverLimper:
                    HandleRaiseOverLimperFlopAction(flopAnalyzerRequest);
                    break;

                case HandSituation.ThreeBet:
                    HandleThreeBetFlopAction();
                    break;

                case HandSituation.OpenRaiseVs3Bet:
                    HandleOpenRaiseVs3BetFlopAction();
                    break;

                case HandSituation.OpenRaiseVs3BetAndCall:
                    HandleOpenRaiseVs3BetAndCallFlopAction();
                    break;

                case HandSituation.FourBet:
                    HandleFourBetFlopAction();
                    break;

                case HandSituation.Cold4Bet:
                    HandleCold4BetFlopAction();
                    break;

                case HandSituation.Squeeze:
                    HandleSqueezeFlopAction();
                    break;

                case HandSituation.VsSqueeze:
                    HandleVsSqueezeFlopAction();
                    break;

                default:
                    break;
            }
        }

        #region [Handle Flop Action]

        private void HandleOpenRaiseFlopAction()
        {
            var board = _scrapeFlopResult.BoardTexture;
            var hero = _scrapeFlopResult.HeroStrength;
            var inPosition = _playerGameState.IsInPosition;

            _responseAction.Action = board switch
            {
                { IsDry: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet 2/3 (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet 1/2 (Valor)",
                    { HasMiddlePair: true } => inPosition ? "Bet 1/3 (Proteger)" : "Check (Call)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },

                { IsCoordinated: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet 2/3 (Valor)",
                    { HasMiddlePair: true } => "Bet 1/2 (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => inPosition ? "Bet 1/2 (Semibluff)" : "Check (Fold)"
                },

                { IsPaired: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet 2/3 (Valor)",
                    { HasMiddlePair: true } => "Check (Call)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },
                _ => "Check (Fold)"
            };
        }

        private void HandleCallFlopAction()
        {
            var board = _scrapeFlopResult.BoardTexture;
            var hero = _scrapeFlopResult.HeroStrength;
            var inPosition = _playerGameState.IsInPosition;

            _responseAction.Action = board switch
            {
                { IsDry: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet 2/3 (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet 1/2 (Valor)",
                    { HasMiddlePair: true } => "Check (Call)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },

                { IsCoordinated: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet 2/3 (Valor)",
                    { HasMiddlePair: true } => "Check (Call)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => inPosition ? "Bet 2/3 (Proyecto muy Fuerte)" : "Check (Fold)"
                },

                { IsPaired: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet 2/3 (Valor)",
                    { HasMiddlePair: true } => "Check (Call)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },
                _ => "Check (Fold)"
            };
        }

        private void HandleRaiseOverLimperFlopAction(FlopAnalyzerHelperReqest flopAnalyzerRequest)
        {
            var rolOopEngine = new RolOopHelper();
            var heroCardsName = $"{flopAnalyzerRequest.PlayerState.HoleCard1Face}{flopAnalyzerRequest.PlayerState.HoleCard2Face}";
            var flopCardsName = string.Empty;

            foreach (var item in flopAnalyzerRequest.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop))
            {
                flopCardsName += item.Name;
            }

            // IP
            if (_playerGameState.IsInPosition)
            {
                var response = RaiseOverLimperIPAnalyzerHelper.DetermineContinuationBetSizing(flopAnalyzerRequest.TableScrapeFlopResult, flopAnalyzerRequest.PlayerState);
                if (response != "Check")
                    _responseAction.Action = $"Bet {response}";
                else
                    _responseAction.Action = response;
            }
            // OOP
            else
            {
                //rolOopEngine.LoadModel();
                //_responseAction.Action = rolOopEngine.PredictAction(heroCardsName,flopCardsName, out var probs);

                if (RaiseOverLimperOOPAnalyzerHelper.IsActionTo13Bet(flopAnalyzerRequest))
                    _responseAction.Action = "Bet 1/3";
                else if (RaiseOverLimperOOPAnalyzerHelper.IsActionTo34Bet(flopAnalyzerRequest))
                    _responseAction.Action = "Bet 3/4";
                else if (RaiseOverLimperOOPAnalyzerHelper.IsActionToCheckCall(flopAnalyzerRequest))
                    _responseAction.Action = "Check/Call";
                else
                    _responseAction.Action = "Check/Fold";
            }
        }

        private void HandleThreeBetFlopAction()
        {
            var board = _scrapeFlopResult.BoardTexture;
            var hero = _scrapeFlopResult.HeroStrength;
            var inPosition = _playerGameState.IsInPosition;

            _responseAction.Action = board switch
            {
                { IsDry: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet 1/2 (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },
                { IsCoordinated: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet 2/3 (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => inPosition ? "Check (Fold)" : "Bet 2/3 (Draw muy Fuerte)"
                },
                { IsPaired: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet 1/2 (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },
                _ => "Check (Fold)"
            };
        }

        private void HandleOpenRaiseVs3BetFlopAction()
        {
            _responseAction.Action = "Not implemented";
        }

        private void HandleOpenRaiseVs3BetAndCallFlopAction()
        {
            _responseAction.Action = "Not implemented";
        }

        private void HandleFourBetFlopAction()
        {
            _responseAction.Action = "Not implemented";
        }

        private void HandleCold4BetFlopAction()
        {
            var board = _scrapeFlopResult.BoardTexture;
            var hero = _scrapeFlopResult.HeroStrength;
            var inPosition = _playerGameState.IsInPosition;

            _responseAction.Action = board switch
            {
                { IsDry: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet 2/3 (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },
                { IsCoordinated: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet Pot (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => inPosition ? "Check (Fold)" : "Bet 2/3 (Draw muy Fuerte)"
                },
                { IsPaired: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet 2/3 (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },
                _ => "Check (Fold)"
            };
        }

        private void HandleSqueezeFlopAction()
        {
            var board = _scrapeFlopResult.BoardTexture;
            var hero = _scrapeFlopResult.HeroStrength;
            var inPosition = _playerGameState.IsInPosition;

            _responseAction.Action = board switch
            {
                { IsDry: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet 2/3 (Proteger)",
                    { HasBottomPair: true } => "1/2 (Bluff)",
                    _ => inPosition ? "Check (Fold)" : "Bet 2/3 (Semibluff)"
                },
                { IsCoordinated: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet Pot (Proteger)",
                    { HasBottomPair: true } => "Bet 1/2 (Bluff)",
                    _ => inPosition ? "Check (Fold)" : "Bet Pot (Semibluff)"
                },
                { IsPaired: true } => hero switch
                {
                    { HasTopPairOrBetter: true } => "Bet Pot (Valor)",
                    { HasTopPair: true } or { HasOverPair: true } => "Bet Pot (Valor)",
                    { HasMiddlePair: true } => "Bet 2/3 (Proteger)",
                    { HasBottomPair: true } => "Check (Fold)",
                    _ => "Check (Fold)"
                },
                _ => "Check (Fold)"
            };
        }

        private void HandleVsSqueezeFlopAction()
        {
            _responseAction.Action = "Not implemented";
        }

        #endregion

        /// <summary>
        /// Procesa la fase de turn
        /// </summary>
        private async Task ProcessTurnAsync()
        {
            _isTurn = false;

            using var bitmap = new Bitmap(_formImage.pbImage.Image);
            var turnResponse = await _getCardsTurnUseCase.ExecuteAsync(new GetCardsTurnUseCaseRequest
            {
                Image = bitmap,
                RegionsTableMap = _regionsTableMap,
                DataBoard = _playerGameState.BoardCards
            });

            var dataBoard = turnResponse.DataBoard;
            _playerGameState.BoardCards = turnResponse.DataBoard;

            var myCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                new CardDataOuts((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
            };

            var communityCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)dataBoard[0].Suit, (Rank)dataBoard[0].Force),
                new CardDataOuts((Suit)dataBoard[1].Suit, (Rank)dataBoard[1].Force),
                new CardDataOuts((Suit)dataBoard[2].Suit, (Rank)dataBoard[2].Force),
                new CardDataOuts((Suit)dataBoard[3].Suit, (Rank)dataBoard[3].Force)
            };

            var result = _pokerCalculator.Calculate(
                myCards,
                communityCards,
                _playerGameState.PotSize,
                _playerGameState.Players.Max(m => m.Bet),
                isInPosition: _playerGameState.IsInPosition,
                heroStack: 0,
                villainStack: 0,
                handSituation: _playerGameState.HandSituation.ToString());

            UpdateOverlayWithPotOdds(result);

            _responseAction.Action = "Turn action not implemented"; // Placeholder
        }

        /// <summary>
        /// Procesa la fase de river
        /// </summary>
        private async Task ProcessRiverAsync()
        {
            _isRiver = false;
            using var bitmap = new Bitmap(_formImage.pbImage.Image);
            var riverResponse = await _getCardsRiverUseCase.ExecuteAsync(new GetCardsRiverUseCaseRequest
            {
                Image = bitmap,
                RegionsTableMap = _regionsTableMap,
                DataBoard = _playerGameState.BoardCards
            });

            var dataBoard = riverResponse.DataBoard;
            _playerGameState.BoardCards = riverResponse.DataBoard;


            var myCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                new CardDataOuts((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
            };

            var communityCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)dataBoard[0].Suit, (Rank)dataBoard[0].Force),
                new CardDataOuts((Suit)dataBoard[1].Suit, (Rank)dataBoard[1].Force),
                new CardDataOuts((Suit)dataBoard[2].Suit, (Rank)dataBoard[2].Force),
                new CardDataOuts((Suit)dataBoard[3].Suit, (Rank)dataBoard[3].Force),
                new CardDataOuts((Suit)dataBoard[4].Suit, (Rank)dataBoard[4].Force)
            };

            var result = _pokerCalculator.Calculate(
                myCards,
                communityCards,
                _playerGameState.PotSize,
                _playerGameState.Players.Max(m => m.Bet),
                isInPosition: _playerGameState.IsInPosition,
                heroStack: 0,
                villainStack: 0,
                handSituation: _playerGameState.HandSituation.ToString());

            UpdateOverlayWithPotOdds(result);

            _responseAction.Action = "River action not implemented"; // Placeholder
        }

        /// <summary>
        /// Maneja la inicialización de una nueva mano
        /// </summary>
        private async Task HandleNewHandAsync()
        {
            _folderPath = Path.Combine(
                DEFAULT_RESOURCES_PATH,
                "Games",
                $"Game_{new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_")}",
                _session);

            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            var path = Path.Combine(_folderPath, "resume.txt");
            await File.AppendAllTextAsync(path, tbResume.Text + Environment.NewLine);
            tbResume.Text = string.Empty;
        }

        /// <summary>
        /// Actualiza el overlay con la información de pot odds
        /// </summary>
        private void UpdateOverlayWithPotOdds(PokerCalculationResult potOddsResult)
        {
            _frmOverlay.UpdateWithCalculationResult(potOddsResult);
            UpdateMetricsPanel(potOddsResult);
        }

        /// <summary>
        /// Actualiza la interfaz con los resultados del análisis
        /// </summary>
        private void UpdateUIWithResults(PokerCalculationResult potOddsResult)
        {
            _playerGameState.HandSituation = _responseAction.HandSituation;

            if (_playerGameState != null)
            {
                if (_isPreflop)
                {
                    UpdateResumeTextForPreflop(potOddsResult);
                    _isPreflop = false;
                }

                if (_isFlop)
                {
                    UpdateResumeTextForFlop();
                }

                UpdatePlayerIndicators();
                UpdateCardImages();
            }

            lbAction.Text = _responseAction?.Action ?? string.Empty;
            lbPositionAction.Text = _playerGameState.HandSituation.ToString() ?? string.Empty;

            _frmOverlay.UpdateSituacion(lbPositionAction.Text);

            if (_frmOverlay != null)
                _frmOverlay.UpdateAction(_responseAction?.Action ?? string.Empty);

            UpdateMetricsPanel(potOddsResult);
        }

        /// <summary>
        /// Actualiza el texto de resumen para la fase de preflop
        /// </summary>
        private void UpdateResumeTextForPreflop(PokerCalculationResult potOddsResult)
        {
            var enMesa = _playerGameState.Players.Count(e => !e.Empty) + 1;
            var sitout = _playerGameState.Players.Count(s => s.SitOut);
            var playing = _playerGameState.Players.Count(p => p.Active) + 1;

            var sb = new StringBuilder();
            sb.AppendLine($"Hand #{_tableHand}: Hold'em No Limit");
            sb.AppendLine($"#{_playerGameState.Players.FirstOrDefault(d => d.Dealer)?.Name ?? "Hero"} is the Dealer");
            sb.AppendLine($"{_playerGameState.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "Hero"}: posts small blind");
            sb.AppendLine($"{_playerGameState.Players.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "Hero"}: posts big blind");
            sb.AppendLine($"Pot: {_playerGameState.PotSize}");
            sb.AppendLine($"*** STATISTICS ***");
            sb.AppendLine($"PotOdds: {(decimal)potOddsResult.PotOddsPercentage}%");
            sb.AppendLine($"Equity: {(decimal)potOddsResult.EquityPercentage}%");
            sb.AppendLine($"Should Call: {potOddsResult.ShouldCall}");
            sb.AppendLine("*** HOLE CARDS ***");
            sb.AppendLine($"Dealt to Hero [{_playerGameState.HoleCard1Face} {_playerGameState.HoleCard2Face}]");

            foreach (TablePosition position in Enum.GetValues(typeof(TablePosition)))
            {
                if (position != TablePosition.None && position <= _playerGameState.Position)
                {
                    var player = _playerGameState.Players.FirstOrDefault(f => f.Position == position);
                    string name = player?.Name ?? "Hero";
                    string action = string.Empty;

                    if (name != "Hero")
                        action = player?.Bet == null ? "folds" : $"bets/calls {player.Bet}";
                    else
                    {
                        if (position == _playerGameState.Position)
                        {
                            sb.AppendLine($"Hand Situation: {_playerGameState.HandSituation}");
                            action = _responseAction?.Action ?? string.Empty;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(action))
                        sb.AppendLine($"{name}: {action}");
                }
            }

            tbResume.Text = sb.ToString();
        }

        /// <summary>
        /// Actualiza el texto de resumen para la fase de flop
        /// </summary>
        private void UpdateResumeTextForFlop()
        {
            tbResume.Text += "*** FLOP *** [";
            var countFlop = 0;
            foreach (var carta in _playerGameState.BoardCards.Where(w => w.Position == BoardPosition.Flop))
            {
                countFlop++;
                if (countFlop == 3)
                    tbResume.Text += $"{carta.Name}]";
                else
                    tbResume.Text += $"{carta.Name} ";
            }
        }

        /// <summary>
        /// Actualiza los indicadores visuales de los jugadores
        /// </summary>
        private void UpdatePlayerIndicators()
        {
            // Resetear colores de botones
            pbbuttonPlayerOne.BackColor = Color.Transparent;
            pbButtonPlayerTwo.BackColor = Color.Transparent;
            pbButtonPlayerThree.BackColor = Color.Transparent;
            pbButtonPlayerFour.BackColor = Color.Transparent;
            pbButtonPlayerFive.BackColor = Color.Transparent;
            pbButtonHero.BackColor = Color.Transparent;

            // Actualizar nombres y apuestas
            lbNamePlayerOne.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P1")?.Alias;
            lbNamePlayerTwo.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P2")?.Alias;
            lbNamePlayerThree.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P3")?.Alias;
            lbNamePlayerFour.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P4")?.Alias;
            lbNamePlayerFive.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P5")?.Alias;

            lbBetPlayerOne.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P1")?.Bet.ToString();
            lbBetPlayerTwo.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P2")?.Bet.ToString();
            lbBetPlayerThree.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P3")?.Bet.ToString();
            lbBetPlayerFour.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P4")?.Bet.ToString();
            lbBetPlayerFive.Text = _playerGameState.Players.FirstOrDefault(f => f.Name == "P5")?.Bet.ToString();

            lbBetHero.Text = _playerGameState.CurrentBet.ToString();
            lbPot.Text = _playerGameState.PotSize.ToString();

            // Marcar el dealer
            if (_playerGameState.Players.FirstOrDefault(f => f.Name == "P1")?.Dealer == true)
                pbbuttonPlayerOne.BackColor = Color.Red;
            if (_playerGameState.Players.FirstOrDefault(f => f.Name == "P2")?.Dealer == true)
                pbButtonPlayerTwo.BackColor = Color.Red;
            if (_playerGameState.Players.FirstOrDefault(f => f.Name == "P3")?.Dealer == true)
                pbButtonPlayerThree.BackColor = Color.Red;
            if (_playerGameState.Players.FirstOrDefault(f => f.Name == "P4")?.Dealer == true)
                pbButtonPlayerFour.BackColor = Color.Red;
            if (_playerGameState.Players.FirstOrDefault(f => f.Name == "P5")?.Dealer == true)
                pbButtonPlayerFive.BackColor = Color.Red;
            if (_playerGameState.IsDealer == true)
                pbButtonHero.BackColor = Color.Red;

            lbTableHand.Text = _tableHand;
            lbTableName.Text = _tableName;
        }

        /// <summary>
        /// Actualiza las imágenes de las cartas
        /// </summary>
        private void UpdateCardImages()
        {
            // Cartas del héroe
            pbHeroCard0.Image = GetCardImage(_playerGameState.HoleCard1Face);
            pbHeroCard1.Image = GetCardImage(_playerGameState.HoleCard2Face);

            // Cartas del tablero
            pbBoard1.Image = GetCardImage(_playerGameState.BoardCards.FirstOrDefault(f => f.Location == 1)?.Name);
            pbBoard2.Image = GetCardImage(_playerGameState.BoardCards.FirstOrDefault(f => f.Location == 2)?.Name);
            pbBoard3.Image = GetCardImage(_playerGameState.BoardCards.FirstOrDefault(f => f.Location == 3)?.Name);
            pbBoard4.Image = GetCardImage(_playerGameState.BoardCards.FirstOrDefault(f => f.Location == 4)?.Name);
            pbBoard5.Image = GetCardImage(_playerGameState.BoardCards.FirstOrDefault(f => f.Location == 5)?.Name);
        }

        /// <summary>
        /// Obtiene la imagen de una carta a partir de su nombre
        /// </summary>
        private Image? GetCardImage(string? cardName)
        {
            if (string.IsNullOrEmpty(cardName))
                return null;

            var card = _cards.FirstOrDefault(x => x.Id.Contains(cardName));
            return card != null ? _imageCropperService.Base64ToImage(card.ImageBase64) : null;
        }

        /// <summary>
        /// Calcula las pot odds y la equidad
        /// </summary>
        private PokerCalculationResult GetPotOddsCalculator()
        {
            var result = _pokerCalculator.Calculate(
                new List<CardDataOuts>
                {
                    new CardDataOuts((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                    new CardDataOuts((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
                },
                new List<CardDataOuts>
                {
                    new CardDataOuts((Suit)_playerGameState.BoardCards[0].Suit, (Rank)_playerGameState.BoardCards[0].Force),
                    new CardDataOuts((Suit)_playerGameState.BoardCards[1].Suit, (Rank)_playerGameState.BoardCards[1].Force),
                    new CardDataOuts((Suit)_playerGameState.BoardCards[2].Suit, (Rank)_playerGameState.BoardCards[2].Force)
                },
                _playerGameState.PotSize,
                _playerGameState.Players.Max(m => m.Bet),
                isInPosition: _playerGameState.IsInPosition,
                heroStack: 0,
                villainStack: 0,
                handSituation: _playerGameState.HandSituation.ToString());

            return result;
        }

        /// <summary>
        /// Establece las apuestas de los jugadores
        /// </summary>
        private void SetBetPlayer()
        {
            using var binaryImage = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImage.Image), _pictureUmbralBet));
            var regionTableMap = _regionsTableMap?.FirstOrDefault(f => f.Id == "Bets");

            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "bet");
                if (playerNumber == null) continue;

                var betValue = SetBetValue(region.PosX, region.PosY, region.Width, region.Height,
                    region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);

                // Validación y limpieza de valores
                if (betValue.ToString().Length > 2 &&
                    !betValue.ToString().Contains(",") &&
                    !betValue.ToString().Contains(".") &&
                    betValue.ToString().Contains("88"))
                {
                    betValue = decimal.Parse(betValue.ToString().Replace("88", ""));
                }

                if (playerNumber == 0)
                {
                    _playerGameState.CurrentBet = betValue;
                    continue;
                }

                var player = _playerGameState.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
                if (player != null)
                {
                    player.Bet = betValue;
                }
            }
        }

        /// <summary>
        /// Establece los jugadores vacíos
        /// </summary>
        private void SetEmptyPlayer()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Empty");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            using var bitmap = new Bitmap(_formImage.pbImage.Image);

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "empty");
                if (playerNumber == null)
                    continue;

                var color = bitmap.GetPixel(region.PosX, region.PosY);

                _playerGameState.Players.Add(CreatePlayerData(playerNumber.Value));

                // Verificamos si el jugador está vacío
                if (region.Name.Contains("empty") && _colorEmpty.Contains(color.B))
                {
                    var player = _playerGameState.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");
                    if (player != null)
                    {
                        player.Empty = true;
                        player.SitOut = true;
                    }
                }
            }
        }

        private void SetActivePlayer()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Playing");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            using var bitmap = new Bitmap(_formImage.pbImage.Image);

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "playing");
                if (playerNumber == null)
                    continue;

                var color = bitmap.GetPixel(region.PosX, region.PosY);

                //_playerGameState.Players.Add(CreatePlayerData(playerNumber.Value));

                // Verificamos si el jugador está vacío
                var player = _playerGameState.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");
                if (region.Name.Contains("playing") && _colorPlaying.Contains(color.B))
                {
                    if (player != null)
                    {
                        player.Active = true;
                    }
                }
                else
                {
                    if (player != null)
                    {
                        player.Active = false;
                    }
                }
            }
        }


        /// <summary>
        /// Establece los alias de los villanos
        /// </summary>
        private void SetAliasVillain()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Names");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "Name");
                if (playerNumber == null) continue;

                var player = _playerGameState.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
                if (player != null)
                {
                    player.Alias = SetTextOCR(region.PosX, region.PosY, region.Width, region.Height,
                        region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);
                }
            }
        }

        /// <summary>
        /// Crea un objeto PlayerData con los datos básicos
        /// </summary>
        private Player CreatePlayerData(int playerNumber) =>
            new Player
            {
                Name = $"P{playerNumber}",
                Active = false,
                Empty = false,
                SitOut = false,
                ValuePosition = playerNumber
            };

        /// <summary>
        /// Extrae el número de jugador de un nombre de región
        /// </summary>
        private int? GetPlayerNumber(string regionName, string extraText = "")
        {
            if (string.IsNullOrEmpty(regionName))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(regionName, @$"p(\d+){extraText}");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        /// <summary>
        /// Establece el valor del bote
        /// </summary>
        private void SetPotValue()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Table");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            var regionPot = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "pot");
            if (regionPot != null)
            {
                decimal potValue = 0;
                var pot = SetTextOCR(regionPot.PosX, regionPot.PosY, regionPot.Width, regionPot.Height,
                    regionPot.Umbral, regionPot.InactiveUmbral, regionPot.IsOnlyNumber);
                try
                {
                    // Mejorada la lógica de parsing
                    if (pot.Length == 4 && !pot.Contains(".") && !pot.Contains(","))
                        decimal.TryParse(pot.Substring(0, 2) + "," + pot.Substring(2), out potValue);
                    else
                        decimal.TryParse(pot, out potValue);
                }
                catch (Exception ex)
                {
                    var pp = ex.Message;
                }
                _playerGameState.PotSize = potValue;
            }
        }

        /// <summary>
        /// Establece el número de mano de la mesa
        /// </summary>
        private void SetTableHand()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Table");
            if (regionTableMap == null)
                return;

            var regionTableHand = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "tablehand");
            if (regionTableHand != null)
            {
                if (string.IsNullOrEmpty(_tableHand))
                {
                    _tableHand = SetTextOCR(regionTableHand.PosX, regionTableHand.PosY, regionTableHand.Width, regionTableHand.Height,
                        regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber);
                    _newHand = true;
                }
                else
                {
                    if (long.TryParse(_tableHand, out var oldTableHand) &&
                        long.TryParse(SetTextOCR(regionTableHand.PosX, regionTableHand.PosY, regionTableHand.Width, regionTableHand.Height,
                            regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber), out var newTableHand))
                    {
                        if (oldTableHand != newTableHand)
                        {
                            _newHand = true;
                            _tableHand = newTableHand.ToString();
                        }
                        else if (newTableHand == 0)
                        {
                            _newHand = true;
                            _newTableHand++;
                            _tableHand = _newTableHand.ToString();
                        }
                    }
                }
            }

            var regionTableName = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "tablename");
            if (regionTableName != null && string.IsNullOrEmpty(_tableName))
            {
                _tableName = SetTextOCR(regionTableName.PosX, regionTableName.PosY, regionTableName.Width, regionTableName.Height,
                    regionTableName.Umbral, regionTableName.InactiveUmbral, regionTableName.IsOnlyNumber);
                // Remove numbers from table name
                _tableName = Regex.Replace(_tableName, @"\d", "");
            }
        }
        #region [Dealer and Positions]

        /// <summary>
        /// Establece el dealer entre los jugadores
        /// </summary>
        private void SetDealerPlayer()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Dealer");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            using var bitmap = new Bitmap(_formImage.pbImage.Image);

            var emptyPositions = _playerGameState.Players
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

        /// <summary>
        /// Establece el dealer para un jugador específico
        /// </summary>
        /// <param name="playerNumber">Número del jugador</param>
        /// <param name="emptyPositions">Lista de posiciones vacías</param>
        private void SetDealerForPlayer(int playerNumber, List<int> emptyPositions)
        {
            // Validación de parámetros
            if (emptyPositions == null)
                throw new ArgumentNullException(nameof(emptyPositions));

            // Para P0 (caso especial)
            if (playerNumber == 0)
            {
                _playerGameState.IsDealer = true;
                _playerGameState.Position = TablePosition.Button;
                return;
            }

            var player = _playerGameState.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");
            if (player == null)
            {
                LogError($"No se encontró el jugador P{playerNumber}");
                return;
            }

            // Actualizar estado del jugador
            player.Dealer = true;
            player.Empty = false;

            // Determinar posición P0 basado en la posición del dealer y asientos vacíos
            _playerGameState.Position = DetermineP0Position(playerNumber, emptyPositions);
        }

        /// <summary>
        /// Determina la posición de P0 basado en la posición del dealer y asientos vacíos
        /// </summary>
        /// <param name="dealerPosition">Posición del dealer</param>
        /// <param name="emptyPositions">Lista de posiciones vacías</param>
        /// <returns>Posición de la mesa para P0</returns>
        private TablePosition DetermineP0Position(int dealerPosition, List<int> emptyPositions)
        {
            var positionMap = new Dictionary<int, (TablePosition defaultPosition, Dictionary<int, TablePosition> emptyPositions)>
            {
                { 1, (TablePosition.CutOff, new Dictionary<int, TablePosition>()) },
                { 2, (TablePosition.Middle, new Dictionary<int, TablePosition> {
                    { 1, TablePosition.Early },
                    { 2, TablePosition.BigBlind }
                })},
                { 3, (TablePosition.Early, new Dictionary<int, TablePosition> {
                    { 1, TablePosition.BigBlind },
                    { 2, TablePosition.SmallBlind }
                })},
                { 4, (TablePosition.BigBlind, new Dictionary<int, TablePosition> {
                    { 1, TablePosition.SmallBlind }
                })},
                { 5, (TablePosition.SmallBlind, new Dictionary<int, TablePosition>()) }
            };

            if (!positionMap.TryGetValue(dealerPosition, out var positionInfo))
                return TablePosition.None;

            // Contar asientos vacíos que afectan la posición de P0
            var relevantEmptySeats = emptyPositions.Count(pos => pos > dealerPosition);

            // Si hay una regla específica para el número de asientos vacíos, úsala
            if (positionInfo.emptyPositions.TryGetValue(relevantEmptySeats, out var specialPosition))
                return specialPosition;

            // Si no hay regla específica, usar la posición por defecto
            return positionInfo.defaultPosition;
        }

        /// <summary>
        /// Establece los jugadores que están en "sit out"
        /// </summary>
        private void SetSitOutPlayer()
        {
            // Validación temprana con return
            var regionTableMap = _regionsTableMap?.FirstOrDefault(f => f.Id == "SitOut");
            if (regionTableMap?.Regions == null || _formImage.pbImage.Image == null)
                return;

            // Inicialización de diccionario con object initializer
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
                if (playerNumber == null)
                    continue;

                var player = _playerGameState.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
                if (player == null)
                    continue;

                var colorIndex = colorSitOutMap.TryGetValue(region.Name, out var index) ? index : 0;

                var active = !player.Active;
                var empty = !player.Empty;
                var textoo = SetTextOCR(region.PosX, region.PosY, region.Width, region.Height,
                    region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);

                // Extracción de condición compleja a variable
                bool isSittingOut = !player.Empty && !player.Active &&
                    SetTextOCR(region.PosX, region.PosY, region.Width, region.Height,
                               region.Umbral, region.InactiveUmbral, region.IsOnlyNumber)
                    .Contains("SIT");

                if (isSittingOut)
                {
                    player.SitOut = true;
                    player.Empty = true;
                }
            }
        }

        /// <summary>
        /// Determina si el jugador P0 está en posición
        /// </summary>
        private void SetIsInPosition()
        {
            var activePlayers = _playerGameState.Players.Where(w => w.Active &&
                                                                   w.ValuePosition != 5 &&
                                                                   w.ValuePosition != 6);

            // Por defecto, asumimos que está en posición
            _playerGameState.IsInPosition = true;

            if (activePlayers.Any(item => (int)_playerGameState.Position > item.ValuePosition) ||
                activePlayers.Any(item => item.Position == TablePosition.Button && item.Active))
            {
                _playerGameState.IsInPosition = false;
            }

            // Simplificación de condiciones específicas
            if (_playerGameState.Position == TablePosition.BigBlind &&
                _playerGameState.Players.Any(a => a.Active && a.Position != TablePosition.SmallBlind))
            {
                _playerGameState.IsInPosition = false;
            }

            if (_playerGameState.Position == TablePosition.SmallBlind)
            {
                _playerGameState.IsInPosition = false;
            }

            if (_playerGameState.Position == TablePosition.BigBlind &&
                _playerGameState.Players.Count(w => w.Active) == 1 &&
                _playerGameState.Players.FirstOrDefault(w => w.Active)?.Position == TablePosition.SmallBlind)
            {
                _playerGameState.IsInPosition = true;
            }
        }

        /// <summary>
        /// Establece las posiciones de los villanos basado en la posición de P0
        /// </summary>
        /// <param name="p0Position">Posición de P0</param>
        private void SetVillainPosition(TablePosition p0Position)
        {
            // Obtener copia de jugadores y filtrar activos (no vacíos ni sitout)
            var allPlayers = _playerGameState.Players.ToList();
            if (allPlayers == null || allPlayers.Count == 0)
                return;

            var activePlayers = allPlayers
                .Where(p => p != null && !p.Empty && !p.SitOut)
                .OrderBy(p => p.ValuePosition)
                .ToList();

            if (!activePlayers.Any())
                return;

            // Orden base de posiciones según la posición de P0 (héroe) para mesa 6-max
            var positionsOrder = p0Position switch
            {
                TablePosition.BigBlind => new List<TablePosition>
                {
                    TablePosition.Early, TablePosition.Middle, TablePosition.CutOff,
                    TablePosition.Button, TablePosition.SmallBlind
                },
                TablePosition.SmallBlind => new List<TablePosition>
                {
                    TablePosition.BigBlind, TablePosition.Early, TablePosition.Middle,
                    TablePosition.CutOff, TablePosition.Button
                },
                TablePosition.Button => new List<TablePosition>
                {
                    TablePosition.SmallBlind, TablePosition.BigBlind, TablePosition.Early,
                    TablePosition.Middle, TablePosition.CutOff
                },
                TablePosition.CutOff => new List<TablePosition>
                {
                    TablePosition.Button, TablePosition.SmallBlind, TablePosition.BigBlind,
                    TablePosition.Early, TablePosition.Middle
                },
                TablePosition.Middle => new List<TablePosition>
                {
                    TablePosition.CutOff, TablePosition.Button, TablePosition.SmallBlind,
                    TablePosition.BigBlind, TablePosition.Early
                },
                TablePosition.Early => new List<TablePosition>
                {
                    TablePosition.Middle, TablePosition.CutOff, TablePosition.Button,
                    TablePosition.SmallBlind, TablePosition.BigBlind
                },
                _ => new List<TablePosition>()
            };

            if (activePlayers.Count() == 4)
                positionsOrder.Remove(TablePosition.Middle);

            if (activePlayers.Count() == 3)
            {
                positionsOrder.Remove(TablePosition.Middle);
                positionsOrder.Remove(TablePosition.Early);
            }

            if (activePlayers.Count() == 2)
            {
                positionsOrder.Remove(TablePosition.Middle);
                positionsOrder.Remove(TablePosition.Early);
                positionsOrder.Remove(TablePosition.CutOff);
            }

            // Limpiar posiciones previas de jugadores activos
            foreach (var p in activePlayers)
            {
                p.Position = TablePosition.None;
            }

            SetVillainPositionExtension(activePlayers, positionsOrder);
        }

        /// <summary>
        /// Extiende la funcionalidad de SetVillainPosition para asignar posiciones a los jugadores
        /// </summary>
        /// <param name="players">Lista de jugadores (ordenados por ValuePosition) a considerar</param>
        /// <param name="positions">Lista de posiciones a asignar (ya recortada a jugadores activos)</param>
        private void SetVillainPositionExtension(List<Player> players, List<TablePosition> positions)
        {
            // Validación de parámetros
            if (players == null || players.Count == 0 || positions == null || positions.Count == 0)
                return;

            // Asignación secuencial respetando blinds cuando sea posible
            foreach (var position in positions)
            {
                Player? assigned = null;

                foreach (var player in players.OrderBy(o => o.ValuePosition))
                {
                    if (player == null)
                        continue;

                    // Elegibles solamente jugadores activos
                    bool shouldAssignPosition = !player.Empty && !player.SitOut && player.Position == TablePosition.None;
                    if (!shouldAssignPosition)
                        continue;

                    // Para blinds, preferir quien muestre apuesta (> 0) si está disponible
                    if (position == TablePosition.SmallBlind || position == TablePosition.BigBlind)
                    {
                        // Si no tiene apuesta, intentar encontrar otro con apuesta para blind
                        if (player.Bet <= 0)
                            continue;
                    }

                    assigned = player;
                    break;
                }

                // Si no se pudo respetar la preferencia de apuesta en blinds, asignar el siguiente disponible
                if (assigned == null)
                {
                    assigned = players.OrderBy(o => o.ValuePosition)
                                      .FirstOrDefault(p => p != null && !p.Empty && !p.SitOut && p.Position == TablePosition.None);
                }

                if (assigned != null)
                {
                    assigned.Position = position;
                }
            }
        }

        #endregion

        #region [Image capture and process]

        /// <summary>
        /// Crea un archivo de log con las manos marcadas
        /// </summary>
        private void CreateLogWithMarkedHands()
        {
            try
            {
                var directoryInfo = new DirectoryInfo(_folderPath);
                if (!Directory.Exists(_folderPath))
                {
                    LogError($"El directorio {_folderPath} no existe");
                    return;
                }

                var archivosPNG = directoryInfo.GetFiles("*.png")
                    .Where(file => file.Extension.ToLower() == ".png")
                    .ToArray();

                if (archivosPNG.Length > 0)
                {
                    var ultimaImagen = archivosPNG.OrderByDescending(file => file.LastWriteTime)
                        .First();

                    string nombreArchivoTexto = $"{directoryInfo.Name}-Revisar.txt";
                    string rutaArchivoTexto = Path.Combine(ultimaImagen.Directory.FullName, nombreArchivoTexto);

                    string contenido = File.Exists(rutaArchivoTexto)
                        ? Environment.NewLine + ultimaImagen.Name
                        : ultimaImagen.Name;

                    File.AppendAllText(rutaArchivoTexto, contenido);
                }

                cbMark.Checked = false;
            }
            catch (Exception ex)
            {
                LogError($"Error al crear log de manos marcadas: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene las posiciones de preflop para el héroe
        /// </summary>
        /// <returns>Diccionario con posiciones y apuestas</returns>
        private Dictionary<TablePosition, Dictionary<TablePosition, decimal>> GetPreflopHeroPosition()
        {
            // Inicialización de diccionario
            var players = new Dictionary<TablePosition, decimal>();

            foreach (var item in _playerGameState.Players.Where(p => !p.Empty || !p.SitOut))
            {
                if (item.Position != TablePosition.None)
                    players[item.Position] = item.Bet;
            }

            // Inicialización de diccionario con posiciones
            var position = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();

            foreach (var heroPosition in Enum.GetValues(typeof(TablePosition))
                                            .Cast<TablePosition>()
                                            .Where(p => p != TablePosition.None))
            {
                position[heroPosition] = new Dictionary<TablePosition, decimal>(players);
            }

            return position;
        }

        /// <summary>
        /// Genera una cadena de números aleatorios
        /// </summary>
        /// <returns>Cadena de 10 dígitos aleatorios</returns>
        private string GenerateRandomNumbers()
        {
            var randomBytes = new byte[10];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);

            // Convertir los bytes a una cadena de números
            var sb = new StringBuilder(10);
            foreach (var b in randomBytes)
            {
                sb.Append(b % 10); // Asegurarse de que cada byte se convierta en un dígito (0-9)
            }

            return sb.ToString();
        }

        /// <summary>
        /// Obtiene las cartas del jugador de forma asíncrona
        /// </summary>
        private async Task ObtainCardsPlayerAsync()
        {
            using var session = _dataBase.LightweightSession();

            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "User");
            if (regionTableMap?.Regions == null || _formImage.pbImage.Image == null)
                return;

            // Carga de cartas una sola vez
            if (_cardsImages == null)
            {
                var cards = await session.Query<Card>().ToListAsync();
                _cardsImages = cards.Select(item => item.ToDto()).ToList();
            }

            foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
            {
                var imageToBase64 = _imageCropperService.CropImageToBase64(
                    _formImage.pbImage.Image,
                    region.PosX,
                    region.PosY,
                    region.Width,
                    region.Height);

                if (_cardsImages == null || !_cardsImages.Any())
                    continue;

                var bestMatch = _cardsImages
                    .Where(item => !string.IsNullOrEmpty(item.ImageBase64))
                    .Select(item => new
                    {
                        Card = item,
                        Percentage = _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64)
                    })
                    .OrderByDescending(x => x.Percentage)
                    .FirstOrDefault();

                if (bestMatch == null)
                    continue;

                switch (region.Name)
                {
                    case "u0cardface0":
                        _playerGameState.HoleCard1Face = bestMatch.Card.Name.Split(" ")[0];
                        _playerGameState.HoleCard1Rank = bestMatch.Card.Force;
                        _playerGameState.HoleCard1Suit = bestMatch.Card.Suit;
                        break;
                    case "u0cardface1":
                        _playerGameState.HoleCard2Face = bestMatch.Card.Name.Split(" ")[0];
                        _playerGameState.HoleCard2Rank = bestMatch.Card.Force;
                        _playerGameState.HoleCard2Suit = bestMatch.Card.Suit;
                        break;
                }
            }
        }

        /// <summary>
        /// Establece el valor de la apuesta para una región
        /// </summary>
        /// <returns>Valor decimal de la apuesta</returns>
        private decimal SetBetValue(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            // Validación de parámetros
            if (_formImage.pbImage.Image == null)
                return 0;

            var firstOcr = new OcrResult();
            var secondOcr = new OcrResult();

            var result = string.Empty;

            firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                posX,
                posY,
                width,
                height,
                umbral ?? 0,
                isOnlyNumber ?? false);


            secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                posX,
                posY,
                width,
                height,
                inactiveUmbral ?? 0,
                isOnlyNumber ?? false);

            if (isOnlyNumber.HasValue == true)
            {
                if (string.IsNullOrEmpty(firstOcr.Text))
                    firstOcr.Text = "0";

                if (string.IsNullOrEmpty(secondOcr.Text))
                    secondOcr.Text = "0";

                var ocr1 = decimal.Parse(firstOcr.Text);
                var ocr2 = decimal.Parse(secondOcr.Text);

                if (ocr2 >= ocr1)
                    result = ocr2.ToString();
                else
                    result = ocr1.ToString();
            }


            if (decimal.TryParse(result, out var bet))
                return bet;

            return 0;
        }

        /// <summary>
        /// Extrae texto OCR de una región específica
        /// </summary>
        /// <returns>Texto extraído</returns>
        private string SetTextOCR(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            // Validación de parámetros
            if (_formImage.pbImage.Image == null)
                return string.Empty;

            var ocr = new OcrResult();

            ocr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                posX,
                posY,
                width,
                height,
                umbral ?? 0,
                isOnlyNumber ?? false);

            // Si no se obtiene texto, intentar con umbral inactivo
            if (string.IsNullOrEmpty(ocr.Text))
            {
                ocr = _ocrService.ExtractTextFromRegionAndDebug(
                    _formImage.pbImage.Image,
                    posX,
                    posY,
                    width,
                    height,
                    inactiveUmbral ?? 0,
                    isOnlyNumber ?? false);
            }

            return ocr.Text ?? string.Empty;
        }

        /// <summary>
        /// Obtiene una imagen mientras se está jugando
        /// </summary>
        private async Task GetImageWhilePlaying()
        {
            try
            {
                string baseFolder = Path.Combine(
                    "C:", "Code", "Poker", "ScrapePoker", "resources", "Games",
                    $"Game_{new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).ToString().Replace("/", "_")}");

                _folderPath = Path.Combine(baseFolder, _session);

                // Crear directorio si no existe
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }

                var path = Path.Combine(_folderPath, $"game_{DateTime.Now.Ticks}.png");

                _useCase.ExecuteImage(path);

                using var windowImg = Image.FromFile(path);

                // Ajuste de tamaño de formulario
                _formImage.Width = windowImg.Width + _formImage.Width / 11;
                _formImage.Height = windowImg.Height + _formImage.Height / 4;

                _formImage.pbImage.Width = windowImg.Width;
                _formImage.pbImage.Height = windowImg.Height;

                // Clonar imagen para evitar problemas de acceso
                _formImage.pbImage.Image = new Bitmap(windowImg);
                _formImage.pbImage.Refresh();

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                LogError($"Error al obtener imagen: {ex.Message}", ex);
            }
        }

        #endregion

        #region [UI Events]

        /// <summary>
        /// Maneja el evento de clic en el botón de ventana
        /// </summary>
        private void btnWindow_Click(object sender, EventArgs e)
        {
            try
            {
                User32.RECT windowRect = new User32.RECT();

                if (_handle == IntPtr.Zero)
                {
                    Task.Delay(2000).Wait();
                    _handle = _useCase.GetWindow(CaptureWindowsHelper.User32.GetForegroundWindow());

                    User32.GetWindowRect(_handle, ref windowRect);
                    _locWindowRect = windowRect;

                    // Inicialización de overlay
                    _frmOverlay = new FrmOverlay
                    {
                        Location = new Point(
                            windowRect.left + (((windowRect.right - windowRect.left) / 2) - ((_frmOverlay.Size.Width / 2) + 165)),
                            windowRect.bottom - 125)
                    };
                    _frmOverlay.Show();
                }

                if (_handle != IntPtr.Zero)
                {
                    User32.GetWindowRect(_handle, ref windowRect);

                    // Extracción de condición compleja a variable
                    bool windowMoved = windowRect.left != _locWindowRect.left ||
                                      windowRect.right != _locWindowRect.right ||
                                      windowRect.top != _locWindowRect.top ||
                                      windowRect.bottom != _locWindowRect.bottom;

                    if (windowMoved)
                    {
                        _locWindowRect = windowRect;
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (_frmOverlay != null)
                            {
                                _frmOverlay.Location = new Point(
                                    windowRect.left + (((windowRect.right - windowRect.left) / 2) - ((_frmOverlay.Size.Width / 2) + 165)),
                                    windowRect.bottom - 125);
                            }
                        });
                    }
                }

                if (!_backgroundExecute)
                    backgroundWorker1.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                LogError($"Error en btnWindow_Click: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Maneja el evento DoWork del BackgroundWorker
        /// </summary>
        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                _backgroundExecute = true;

                User32.RECT windowRect = new User32.RECT();
                User32.GetWindowRect(_handle, ref windowRect);

                while (true)
                {
                    // Validación de overlay
                    if (_frmOverlay == null || !_frmOverlay.Visible)
                    {
                        e.Cancel = true;
                        return;
                    }

                    using var img = _useCase.Execute(_handle);

                    // Validación de regiones
                    var regionAction = _regionsTableMap?.FirstOrDefault(f => f.Id == "User")?.Regions?.FirstOrDefault(x => x.Name == "uAction");
                    var flop = _regionsTableMap?.FirstOrDefault(f => f.Id == "Table")?.Regions?.FirstOrDefault(x => x.Name == "isFlop");

                    if (regionAction == null || flop == null)
                    {
                        Task.Delay(100).Wait();
                        continue;
                    }

                    using var bitmap = new Bitmap(img);
                    Color colorAction = bitmap.GetPixel(regionAction.PosX, regionAction.PosY);
                    Color colorFlop = bitmap.GetPixel(flop.PosX, flop.PosY);

                    this.Invoke((MethodInvoker)delegate
                    {
                        // Extracción de condiciones a variables
                        bool shouldCaptureFlop = colorAction.B == 24 && !_executeCapture && colorFlop.B == 255;
                        bool shouldCapture = colorAction.B == 24 && !_executeCapture;

                        if (shouldCaptureFlop)
                        {
                            _isFlop = true;
                        }

                        if (shouldCapture)
                        {
                            btnCapture_Click(sender, e);
                        }

                        if (colorAction.B != 24)
                            _executeCapture = false;
                    });

                    btnWindow_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error en BackgroundWorker1_DoWork: {ex.Message}", ex);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Habilita los botones de control
        /// </summary>
        private void EnableButtons()
        {
            var buttons = new[]
            {
                btnPlusHeight, btnPlusWidth, btnMinusHeight, btnMinusWidth,
                btnUp, btnUpRight, btnRight, btnDownRight, btnDown,
                btnDownLeft, btnLeft, btnUpLeft
            };

            foreach (var button in buttons)
            {
                button.Enabled = true;
            }
        }

        #endregion


        #region [Regions manipulation methods]

        /// <summary>
        /// Maneja el evento de clic en el botón para aumentar el ancho
        /// </summary>
        private void btnPlusWidth_Click(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null)
                return;

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { Width = _selectedRegion.Width + _speed };
                _selectedRegion = updatedRegion;
                tbWidth.Text = _selectedRegion.Width.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Maneja el evento de clic en el botón para disminuir el ancho
        /// </summary>
        private void btnMinusWidth_Click(object sender, EventArgs e)
        {
            if (_selectedRegion == null)
                return;

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { Width = Math.Max(1, _selectedRegion.Width - _speed) };
                _selectedRegion = updatedRegion;
                tbWidth.Text = _selectedRegion.Width.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Maneja el evento de clic en el botón para aumentar la altura
        /// </summary>
        private void btnPlusHeight_Click(object sender, EventArgs e)
        {
            if (_selectedRegion == null)
                return;

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { Height = _selectedRegion.Height + _speed };
                _selectedRegion = updatedRegion;
                tbHeight.Text = _selectedRegion.Height.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Maneja el evento de clic en el botón para disminuir la altura
        /// </summary>
        private void btnMinusHeight_Click(object sender, EventArgs e)
        {
            if (_selectedRegion == null)
                return;

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { Height = Math.Max(1, _selectedRegion.Height - _speed) };
                _selectedRegion = updatedRegion;
                tbHeight.Text = _selectedRegion.Height.ToString();
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Handles the click event for the move right button
        /// </summary>
        private void btnRigth_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: _speed, offsetY: 0);
        }

        /// <summary>
        /// Handles the click event for the move left button
        /// </summary>
        private void btnLeft_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: -_speed, offsetY: 0);
        }

        /// <summary>
        /// Handles the click event for the move down button
        /// </summary>
        private void btnDown_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: 0, offsetY: _speed);
        }

        /// <summary>
        /// Handles the click event for the move up button
        /// </summary>
        private void btnUp_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: 0, offsetY: -_speed);
        }

        /// <summary>
        /// Handles the click event for the move up-left button
        /// </summary>
        private void btnUpLeft_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: -_speed, offsetY: -_speed);
        }

        /// <summary>
        /// Handles the click event for the move up-right button
        /// </summary>
        private void btnUpRight_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: _speed, offsetY: -_speed);
        }

        /// <summary>
        /// Handles the click event for the move down-left button
        /// </summary>
        private void btnDownLeft_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: -_speed, offsetY: _speed);
        }

        /// <summary>
        /// Handles the click event for the move down-right button
        /// </summary>
        private void btnDownRight_Click(object sender, EventArgs e)
        {
            // Extracción a método común
            MoveRegion(offsetX: _speed, offsetY: _speed);
        }

        /// <summary>
        /// Método común para mover la región seleccionada
        /// </summary>
        /// <param name="offsetX">Desplazamiento en X</param>
        /// <param name="offsetY">Desplazamiento en Y</param>
        private void MoveRegion(int offsetX, int offsetY)
        {
            // Método común para mover región
            if (_selectedRegion == null || _formImage.pbImage.Image == null)
                return;

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                // Obtener información de color
                var rgbResponse = GetColorResponse();

                var updateRegion = _selectedRegion with
                {
                    PosX = _selectedRegion.PosX + offsetX,
                    PosY = _selectedRegion.PosY + offsetY
                };

                // Actualizar UI
                tbY.Text = updateRegion.PosY.ToString();
                tbX.Text = updateRegion.PosX.ToString();
                _papel.DrawRectangle(lapiz, updateRegion.PosX, updateRegion.PosY, updateRegion.Width, updateRegion.Height);

                // Actualizar color si es necesario
                if (updateRegion.IsColor.GetValueOrDefault())
                {
                    updateRegion = updateRegion with
                    {
                        Color = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}"
                    };
                }

                _selectedRegion = updateRegion;
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Obtiene la respuesta de color RGB para la región seleccionada
        /// </summary>
        /// <returns>Respuesta con valores RGB</returns>
        private GetRGBColorResponse GetColorResponse()
        {
            // Validación de región seleccionada
            if (_selectedRegion == null || _formImage.pbImage.Image == null)
                return new GetRGBColorResponse();

            var rgbRequest = new GetRGBColorRequest
            {
                Image = (Bitmap)_formImage.pbImage.Image,
                X = _selectedRegion.PosX,
                Y = _selectedRegion.PosY,
                IsColor = _selectedRegion.IsColor.GetValueOrDefault()
            };

            var rgbResponse = ColorHelper.GetRGBColor(rgbRequest);

            if (_selectedRegion.IsColor.GetValueOrDefault())
            {
                tbColor.Text = $"{rgbResponse.RColor}{rgbResponse.GColor}{rgbResponse.BColor}";
            }

            return rgbResponse;
        }

        #endregion

        #region [TextBox Events]


        /// <summary>
        /// Maneja el evento de salida del textbox de ancho
        /// </summary>
        private void tbWidth_Leave(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null)
                return;

            // Validación de entrada
            if (!int.TryParse(tbWidth.Text, out int width))
            {
                tbWidth.Text = _selectedRegion.Width.ToString();
                return;
            }

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { Width = Math.Max(1, width) };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Maneja el evento de salida del textbox de altura
        /// </summary>
        private void tbHeight_Leave(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null)
                return;

            // Validación de entrada
            if (!int.TryParse(tbHeight.Text, out int height))
            {
                tbHeight.Text = _selectedRegion.Height.ToString();
                return;
            }

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { Height = Math.Max(1, height) };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Maneja el evento de clic en el textbox de posición X
        /// </summary>
        private void tbX_Leave(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null)
                return;

            // Validación de entrada
            if (!int.TryParse(tbX.Text, out int posX))
            {
                tbX.Text = _selectedRegion.PosX.ToString();
                return;
            }

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { PosX = posX };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        /// <summary>
        /// Maneja el evento de salida del textbox de posición Y
        /// </summary>
        private void tbY_Leave(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null)
                return;

            // Validación de entrada
            if (!int.TryParse(tbY.Text, out int posY))
            {
                tbY.Text = _selectedRegion.PosY.ToString();
                return;
            }

            _formImage.pbImage.Refresh();

            using (_papel = _formImage.pbImage.CreateGraphics())
            {
                using var lapiz = new Pen(Color.Red);

                var updatedRegion = _selectedRegion with { PosY = posY };
                _selectedRegion = updatedRegion;
                _papel.DrawRectangle(lapiz, _selectedRegion.PosX, _selectedRegion.PosY, _selectedRegion.Width, _selectedRegion.Height);
            }

            _img = _formImage.pbImage.Image;
        }

        #endregion

        #region [Others UI Events]

        /// <summary>
        /// Maneja el evento de cambio del checkbox de prueba
        /// </summary>
        private void cbTest_CheckedChanged(object sender, EventArgs e)
        {
            gbTest.Enabled = cbTest.Checked;
            gbTest.Visible = cbTest.Checked;

            if (!gbTest.Enabled)
            {
                rbFlop.Checked = false;
                rbTurn.Checked = false;
                rbRiver.Checked = false;
            }
        }

        /// <summary>
        /// Maneja el evento de doble clic en el TreeView de tablas
        /// </summary>
        private void twTables_DoubleClick(object sender, EventArgs e)
        {
            if (sender is TreeView treeView && treeView.SelectedNode != null)
            {
                var manos = GetHandsFromActionNode(treeView.SelectedNode);

                var manosOrder = manos?.OrderByDescending(o => o.Name).ToList();
                dgvHands.DataSource = manosOrder;
            }
        }

        /// <summary>
        /// Obtiene las manos de acción para un nodo seleccionado
        /// </summary>
        /// <param name="selectedNode">Nodo seleccionado</param>
        /// <returns>Lista de manos</returns>
        private List<Hand> GetHandsFromActionNode(TreeNode selectedNode)
        {
            // Validación de nodo
            if (selectedNode == null)
                return new List<Hand>();

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

                var table = _dataTables?.FirstOrDefault(f => f.Id == tableName);
                if (table != null)
                {
                    var position = table.Positions?
                        .FirstOrDefault(p =>
                            p.Name == positionName &&
                            p.HeroPosition == heroPosition);

                    return position?.Hands?.ToList() ?? new List<Domain.ValueObjects.Hand>();
                }
            }

            return new List<Domain.ValueObjects.Hand>();
        }

        /// <summary>
        /// Maneja el evento antes de expandir un nodo del TreeView
        /// </summary>
        private void twTables_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            // Validación de nodo
            if (e.Node == null)
                return;

            TreeNode nodeToExpand = e.Node;

            IEnumerable<TreeNode> siblingNodes = nodeToExpand.Parent == null
                ? twTables.Nodes.Cast<TreeNode>().Where(node => node != nodeToExpand)
                : nodeToExpand.Parent.Nodes.Cast<TreeNode>().Where(node => node != nodeToExpand);

            // Colapsar todos los nodos hermanos que estén expandidos
            foreach (TreeNode sibling in siblingNodes)
            {
                if (sibling.IsExpanded)
                {
                    sibling.Collapse();
                }
            }
        }

        /// <summary>
        /// Maneja el evento de clic en el botón de prueba de color
        /// </summary>
        private void btnTestColor_Click(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null || _formImage.pbImage.Image == null)
                return;

            var color = _colorDetectionService.GetPixelColor(
                _formImage.pbImage.Image,
                _selectedRegion.PosX,
                _selectedRegion.PosY);

            pbColorDebug.BackColor = Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
            tbTestColor.Text = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
        }

        /// <summary>
        /// Establece el color del PictureBox
        /// </summary>
        /// <param name="hexColor">Color en formato hexadecimal</param>
        private void SetPictureBoxColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
                return;

            // Asegurarse que el valor hex tenga el formato correcto
            hexColor = hexColor.Replace("#", "");

            try
            {
                Color color = ColorTranslator.FromHtml("#" + hexColor);
                using (Bitmap bmp = new Bitmap(pbRegionColor.Width, pbRegionColor.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(color);
                    }
                    pbRegionColor.Image?.Dispose();
                    pbRegionColor.Image = new Bitmap(bmp);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error al establecer color: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Maneja el evento de clic en el botón de prueba de texto
        /// </summary>
        private void btnTestTexto_Click(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null || _formImage.pbImage.Image == null)
                return;

            var firstOcr = new OcrResult();
            var secondOcr = new OcrResult();

            var result = string.Empty;

            firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                _selectedRegion.PosX,
                _selectedRegion.PosY,
                _selectedRegion.Width,
                _selectedRegion.Height,
                _selectedRegion.Umbral ?? 0,
                _selectedRegion.IsOnlyNumber ?? false);

            secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                _selectedRegion.PosX,
                _selectedRegion.PosY,
                _selectedRegion.Width,
                _selectedRegion.Height,
                _selectedRegion.InactiveUmbral ?? 0,
                _selectedRegion.IsOnlyNumber ?? false);

            if (_selectedRegion.IsOnlyNumber == true)
            {
                if (string.IsNullOrEmpty(firstOcr.Text))
                    firstOcr.Text = "0";

                if (string.IsNullOrEmpty(secondOcr.Text))
                    secondOcr.Text = "0";

                var ocr1 = decimal.Parse(firstOcr.Text);
                var ocr2 = decimal.Parse(secondOcr.Text);

                if (ocr2 >= ocr1)
                    result = ocr2.ToString();
                else
                    result = ocr1.ToString();
            }


            tbTestTexto.Text = !string.IsNullOrEmpty(result) ? result : "Sin resultado";

            // Liberar imagen anterior
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = firstOcr.Image;
        }

        /// <summary>
        /// Maneja el evento de clic en el botón de prueba de carta
        /// </summary>
        private async void btnTestCarta_Click(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null || _formImage.pbImage.Image == null)
            {
                LogError("No se ha seleccionado una región o la imagen es nula.");
                return;
            }

            try
            {
                var imageToBase64 = _imageCropperService.CropImageToBase64(
                    _formImage.pbImage.Image,
                    _selectedRegion.PosX,
                    _selectedRegion.PosY,
                    _selectedRegion.Width,
                    _selectedRegion.Height);

                // Carga de cartas una sola vez
                if (_cardsImages == null)
                {
                    _cardsImages = await _cardUseCases.GetAllCards.ExecuteAsync();
                }

                if (_cardsImages?.Any() == true)
                {
                    var bestMatch = _cardsImages
                        .Where(item => !string.IsNullOrEmpty(item.ImageBase64))
                        .Select(item => new
                        {
                            Card = item,
                            Percentage = _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64)
                        })
                        .OrderByDescending(x => x.Percentage)
                        .FirstOrDefault();

                    if (bestMatch != null)
                    {
                        // Liberar imagen anterior
                        pbTestCarta.Image?.Dispose();
                        pbTestCarta.Image = _imageCropperService.Base64ToImage(bestMatch.Card.ImageBase64);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error al probar carta: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Maneja el evento de clic en el botón de limpiar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            rbFlop.Checked = false;
            rbTurn.Checked = false;
            rbRiver.Checked = false;
        }

        /// <summary>
        /// Logs an error message to the output or a log file.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception details.</param>
        private void LogError(string message, Exception? exception = null)
        {
            // Example implementation: Log to the console or a file
            Console.WriteLine($"Error: {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception.Message}");
            }
        }

        /// <summary>
        /// Logs an informational message to the output or a log file.
        /// </summary>
        /// <param name="message"></param>
        private void LogInformation(string message)
        {
            // Example implementation: Log to the console or a file
            Console.WriteLine($"Info: {message}");
        }

        #endregion

        #region [Visual Styling Methods]

        /// <summary>
        /// Inicializa todos los estilos visuales
        /// </summary>
        private void InitializeVisualStyles()
        {
            this.SuspendLayout();

            // Configuración del formulario principal
            this.BackColor = AppThemeHelper.BackgroundMain;
            this.Font = new Font("Segoe UI", 9F);

            // Aplicar estilos
            ApplyModernTabStyle();

            // Configurar renderizado optimizado
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.DoubleBuffer |
                          ControlStyles.ResizeRedraw, true);

            this.ResumeLayout(true);
        }

        /// <summary>
        /// Aplica estilo moderno al TabControl
        /// </summary>
        private void ApplyModernTabStyle()
        {
            tbControl.SuspendLayout();

            // Configuración del TabControl
            tbControl.Appearance = TabAppearance.FlatButtons;
            tbControl.BackColor = AppThemeHelper.BackgroundMain;
            tbControl.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // Estilo de las pestañas
            foreach (TabPage tab in tbControl.TabPages)
            {
                tab.BackColor = AppThemeHelper.BackgroundMain;
                tab.Padding = new Padding(10);

                // Aplicar estilo específico por pestaña
                switch (tab.Name)
                {
                    case "tbJuego":
                        ApplyGameTabStyle(tab);
                        break;
                    case "tbConfig":
                        ApplyConfigTabStyle(tab);
                        break;
                    case "tbTables":
                        ApplyTablesTabStyle(tab);
                        break;
                    case "tbLogs":
                        ApplyLogsTabStyle(tab);
                        break;
                }
            }

            tbControl.ResumeLayout(true);
        }

        /// <summary>
        /// Aplica estilo moderno a la pestaña de juego
        /// </summary>
        private void ApplyGameTabStyle(TabPage gameTab)
        {
            gameTab.SuspendLayout();

            // Mejorar visualización de cartas
            var cardPictureBoxes = new[] { pbHeroCard0, pbHeroCard1, pbBoard1, pbBoard2, pbBoard3, pbBoard4, pbBoard5 };
            foreach (var pb in cardPictureBoxes)
            {
                pb.BackColor = AppThemeHelper.BackgroundCard;
                pb.BorderStyle = BorderStyle.None;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;

                // Agregar borde redondeado visual
                pb.Paint += (s, e) =>
                {
                    using var pen = new Pen(AppThemeHelper.BorderLight, 2f);
                    e.Graphics.DrawRectangle(pen, 0, 0, pb.Width - 1, pb.Height - 1);
                };
            }

            // Mejorar paneles de información
            var infoPanels = new[] { pnNamePlayerFive, panel2, panel3, panel4, panel5, panel6, panel7, panel8, panel9, panel10,
                            pnNameHero, pnNamePlayerOne, pnNamePlayerTwo, pnNamePlayerThree, pnNamePlayerFour };

            foreach (Panel panel in infoPanels)
            {
                panel.BackColor = AppThemeHelper.BackgroundCard;
                panel.BorderStyle = BorderStyle.None;
                panel.Padding = new Padding(5);

                // Sombra sutil
                panel.Paint += (s, e) =>
                {
                    using var brush = new SolidBrush(AppThemeHelper.BorderLight);
                    e.Graphics.FillRectangle(brush, 0, panel.Height - 1, panel.Width, 1);
                    e.Graphics.FillRectangle(brush, panel.Width - 1, 0, 1, panel.Height);
                };
            }

            // Mejorar botones de acción
            var actionButtons = new[] { btnCapture, btnWindow };
            foreach (Button btn in actionButtons)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = AppThemeHelper.Accent;
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                btn.Cursor = Cursors.Hand;

                // Efecto hover
                btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(0, 100, 160);
                btn.MouseLeave += (s, e) => btn.BackColor = AppThemeHelper.Accent;
            }

            // Mejorar label de acción principal
            lbAction.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lbAction.ForeColor = AppThemeHelper.PrimaryDark;
            lbAction.BackColor = Color.Transparent;

            gameTab.ResumeLayout(true);
        }

        /// <summary>
        /// Aplica estilo moderno a la pestaña de configuración
        /// </summary>
        private void ApplyConfigTabStyle(TabPage configTab)
        {
            configTab.SuspendLayout();

            // Mejorar TreeView de regiones
            twRegionsConfig.BackColor = AppThemeHelper.BackgroundCard;
            twRegionsConfig.BorderStyle = BorderStyle.None;
            twRegionsConfig.Font = new Font("Segoe UI", 9F);
            twRegionsConfig.ForeColor = AppThemeHelper.PrimaryDark;
            twRegionsConfig.LineColor = AppThemeHelper.BorderLight;

            // Mejorar GroupBox de región
            rgRegion.BackColor = AppThemeHelper.BackgroundCard;
            rgRegion.ForeColor = AppThemeHelper.PrimaryDark;
            rgRegion.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Mejorar controles de movimiento
            var movementButtons = new[] { btnUp, btnDown, btnLeft, btnRight, btnUpLeft, btnUpRight, btnDownLeft, btnDownRight };
            foreach (Button btn in movementButtons)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = AppThemeHelper.BorderLight;
                btn.BackColor = AppThemeHelper.BackgroundCard;
                btn.ForeColor = AppThemeHelper.PrimaryDark;
                btn.Font = new Font("Segoe UI", 10F);
                btn.Cursor = Cursors.Hand;

                // Efecto hover
                btn.MouseEnter += (s, e) =>
                {
                    btn.BackColor = AppThemeHelper.PrimaryLight;
                    btn.ForeColor = Color.White;
                };
                btn.MouseLeave += (s, e) =>
                {
                    btn.BackColor = AppThemeHelper.BackgroundCard;
                    btn.ForeColor = AppThemeHelper.PrimaryDark;
                };
            }

            // Mejorar botones de tamaño
            var sizeButtons = new[] { btnPlusWidth, btnMinusWidth, btnPlusHeight, btnMinusHeight };
            foreach (Button btn in sizeButtons)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = AppThemeHelper.Accent;
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                btn.Cursor = Cursors.Hand;
            }

            // Mejorar TextBoxes
            var textBoxes = new[] { tbX, tbY, tbWidth, tbHeight, tbColor, tbRegionUmbral, tbRegionInactUmbral };
            foreach (TextBox tb in textBoxes)
            {
                tb.BorderStyle = BorderStyle.FixedSingle;
                tb.BackColor = AppThemeHelper.BackgroundCard;
                tb.ForeColor = AppThemeHelper.PrimaryDark;
                tb.Font = new Font("Segoe UI", 9F);
            }

            // Mejorar CheckBoxes
            var checkBoxes = new[] { cbRegionColor, cbRegionHash, cbRegionBoard, cbRegionNumber };
            foreach (CheckBox cb in checkBoxes)
            {
                cb.FlatStyle = FlatStyle.System;
                cb.BackColor = Color.Transparent;
                cb.ForeColor = AppThemeHelper.PrimaryDark;
                cb.Font = new Font("Segoe UI", 9F);
            }

            configTab.ResumeLayout(true);
        }

        /// <summary>
        /// Aplica estilo moderno a la pestaña de tablas
        /// </summary>
        private void ApplyTablesTabStyle(TabPage tablesTab)
        {
            tablesTab.SuspendLayout();

            // Mejorar TreeView de tablas
            twTables.BackColor = AppThemeHelper.BackgroundCard;
            twTables.BorderStyle = BorderStyle.None;
            twTables.Font = new Font("Segoe UI", 9F);
            twTables.ForeColor = AppThemeHelper.PrimaryDark;

            // Mejorar DataGridView
            dgvHands.EnableHeadersVisualStyles = false;
            dgvHands.BackgroundColor = AppThemeHelper.BackgroundMain;
            dgvHands.BorderStyle = BorderStyle.None;
            dgvHands.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvHands.GridColor = AppThemeHelper.BorderLight;
            dgvHands.Font = new Font("Segoe UI", 9F);

            // Estilo de headers
            dgvHands.ColumnHeadersDefaultCellStyle.BackColor = AppThemeHelper.PrimaryDark;
            dgvHands.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHands.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            dgvHands.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryDark;
            dgvHands.ColumnHeadersHeight = 35;

            // Estilo de celdas
            dgvHands.DefaultCellStyle.BackColor = AppThemeHelper.BackgroundCard;
            dgvHands.DefaultCellStyle.ForeColor = AppThemeHelper.PrimaryDark;
            dgvHands.DefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryLight;
            dgvHands.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvHands.RowTemplate.Height = 28;

            // Estilo alternado
            dgvHands.AlternatingRowsDefaultCellStyle.BackColor = AppThemeHelper.BackgroundMain;

            tablesTab.ResumeLayout(true);
        }

        /// <summary>
        /// Aplica estilo moderno a la pestaña de logs
        /// </summary>
        private void ApplyLogsTabStyle(TabPage logsTab)
        {
            logsTab.SuspendLayout();

            // Mejorar TextBox de logs
            tbResume.BackColor = AppThemeHelper.PrimaryDark;
            tbResume.ForeColor = Color.FromArgb(220, 220, 220);
            tbResume.Font = new Font("Consolas", 9F);
            tbResume.BorderStyle = BorderStyle.None;

            logsTab.ResumeLayout(true);
        }

        private void pnMetrics_Paint(object sender, PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(pnMetrics.ClientRectangle, Color.LightBlue, Color.White, LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, pnMetrics.ClientRectangle);
            }
        }

        #endregion

        private void tbJuego_Click(object sender, EventArgs e)
        {

        }
    }


}