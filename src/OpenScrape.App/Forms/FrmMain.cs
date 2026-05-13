using JasperFx.Core;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Configuration;
using OpenScrape.App.Entities;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Helpers.FlopHelper;
using OpenScrape.App.Helpers.FlopHelper.RaiseOverLimper;
using OpenScrape.App.Models;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Logging;
using OpenScrape.App.Telemetry;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
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
using Image = System.Drawing.Image;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App
{
    /// <summary>
    /// Formulario principal de la aplicación OpenScrape para análisis de mesas de póker
    /// </summary>
    public partial class FrmMain : Form, IDisposable
    {
        #region [Constants]
        private static readonly string DEFAULT_SAMPLES_PATH = Path.Combine(
            "C:", "Code", "Poker", "ScrapePoker", "output", "samples");
        #endregion

        #region [Enums]
        // TurnBoardTexture y RiverBoardTexture movidos a Entities/BoardTextures.cs
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
        private readonly RegionLookupCache _regionLookupCache;
        private readonly CardCacheService _cardCacheService;
        private readonly ICoordinateScaler _coordinateScaler;
        private Domain.ValueObjects.Region? _selectedRegion;
        private readonly string _pathResume;
        private Dictionary<TablePosition, Dictionary<TablePosition, decimal>> _preflopHeroPosition = new();
        private int _pictureUmbralBet = 130;
        private string _session = string.Empty;
        private IntPtr _handle;
        private User32.RECT _locWindowRect = new();
        private volatile bool _executeCapture;
        // Street flags derivados del state machine
        private bool IsPreflop => _gameLoopStateMachine.IsPreflop;
        private bool IsFlop => _gameLoopStateMachine.IsFlop;
        private bool IsTurn => _gameLoopStateMachine.IsTurn;
        private bool IsRiver => _gameLoopStateMachine.IsRiver;
        private string _tableName = string.Empty;
        private long _newTableHand;
        private bool _newHand;
        /// <summary>
        /// Stack del hero antes del auto-rebuy. Se actualiza durante la mano activa
        /// y NO se actualiza cuando se detecta un rebuy (stack sube bruscamente).
        /// Usado para calcular el profit real de la mano.
        /// </summary>
        private decimal _heroStackPreRebuy;
        private string _previousSBPlayerName = "";
        private string _previousBBPlayerName = "";
        private int _lastActivePlayerCount = 0;
        private BetSizeCategory GetOpponentBetSize(decimal maxBet, decimal potSize)
            => _coordinator.GetOpponentBetSize(maxBet, potSize);

        private string GetActiveVillainId()
            => _coordinator.GetActiveVillainId(_playerGameState);

        private OpponentType GetVillainType(bool? heroIsInPosition = null)
            => _coordinator.GetVillainType(_playerGameState, heroIsInPosition);

        private void TrackVillainPostflopAction(decimal maxBet, bool isPreflopAggressor, bool? heroIsInPosition = null)
            => _coordinator.TrackVillainPostflopAction(_playerGameState, maxBet, isPreflopAggressor, heroIsInPosition);

        private decimal GetVillainStack()
            => _coordinator.GetVillainStack(_playerGameState);

        private (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(decimal maxBet, bool isHeroInPosition, HandSituation currentSituation)
            => _coordinator.DetectDonkBet(_playerGameState, maxBet, isHeroInPosition, currentSituation);
        private volatile bool _backgroundExecute;
        private IReadOnlyList<Table>? _tables;
        private List<Table>? _dataTables;
        private PokerCalculationResult _flopResult;
        private PokerCalculationResult _turnResult;
        private TurnBoardTexture _turnBoardTexture;
        private PokerCalculationResult _riverResult;
        private RiverBoardTexture _riverBoardTexture;
        #endregion

        #region [Services and UseCases]
        private readonly GetWindowsScreenUseCase _useCase;
        private readonly ActionScenarioUseCases _actionScenarioUseCases;
        private readonly RegionTableMapUseCases _regionTableMapUseCases;
        private readonly ISetPreflopActionUseCase _setPreflopActionUseCase;
        private readonly ImageCropperService _imageCropperService;
        private List<CardDTO>? _cardsImages;
        private readonly IDocumentStore _dataBase;
        // _sessionDB eliminado: se usan sesiones locales con using para evitar connection leak
        private readonly ISetFlopForceBoardUseCase _setFlopForceBoardUseCase;
        private readonly IGetHashImageUseCase _getHashImageUseCase;
        private readonly IGetCropImageUseCase _getCropImageUseCase;
        private readonly IGetCardsFlopUseCase _getCardsFlopUseCase;
        private readonly IGetCardsTurnUseCase _getCardsTurnUseCase;
        private readonly IGetCardsRiverUseCase _getCardsRiverUseCase;
        private readonly IOutsCalculatorUseCase _outsCalculatorUseCase;
        private readonly IPokerCalculator _pokerCalculator;
        private readonly IBetSizingService _betSizingService;
        private readonly ColorDetectionService _colorDetectionService;
        private readonly OcrService _ocrService;
        private readonly CardUseCases _cardUseCases;
        private readonly GameLoggerService _gameLoggerService;
        private readonly DetectionLoggerService _detectionLoggerService;
        private readonly GameLoopStateMachine _gameLoopStateMachine;
        private readonly StrategyProfileService _strategyProfileService;
        private readonly IPostflopDecisionService _postflopDecisionService;
        private readonly IExploitabilityCalculator _exploitabilityCalculator;
        private readonly IAutoCalibrationService _autoCalibrationService;
        private readonly IBankrollTrackerService _bankrollTrackerService;
        private readonly BoardTextureAnalyzer _boardTextureAnalyzer;
        private readonly IOpponentTracker _opponentTracker;
        private readonly OverlayConfig _overlayConfig;
        private readonly IGameCoordinator _coordinator;
        private readonly IScreenReaderService _screenReader;
        private readonly ITableLayoutService _tableLayout;
        private readonly IPostflopContextHolder _contextHolder;
        private readonly ILogger<FrmMain> _logger;
        private readonly TextBoxLoggerProvider _textBoxLoggerProvider;

        // refactor-frmmain-coordinators Fase 6: inyectados pero solo se activan con feature flag
        private readonly IGameLoopCoordinator _gameLoopCoordinator;
        private readonly IUiSyncService _uiSyncService;
        private readonly FeatureFlags _featureFlags;
        private CancellationTokenSource? _gameLoopCts;

        // extract-frmmain-testable-logic Fase 2-3: helpers de lógica pura extraídos
        private readonly IActionFormatter _actionFormatter;
        private readonly IOverlayPositioner _overlayPositioner;
        #endregion

        /// <summary>
        /// Constructor del formulario principal
        /// </summary>
        public FrmMain(IDocumentStore dataBase,
                        ActionScenarioUseCases actionScenarioUseCases,
                        CardUseCases cardUseCases,
                        RegionTableMapUseCases regionTableMapUseCases,
                        IPokerCalculator pokerCalculator,
                        IBetSizingService betSizingService,
                        GameLoggerService gameLoggerService,
                        GameLoopStateMachine gameLoopStateMachine,
                        StrategyProfileService strategyProfileService,
                        IPostflopDecisionService postflopDecisionService,
                        IExploitabilityCalculator exploitabilityCalculator,
                        IAutoCalibrationService autoCalibrationService,
                        IBankrollTrackerService bankrollTrackerService,
                        BoardTextureAnalyzer boardTextureAnalyzer,
                        IOpponentTracker opponentTracker,
                        IOptions<OverlayConfig> overlayConfigOptions,
                        RegionLookupCache regionLookupCache,
                        CardCacheService cardCacheService,
                        ICoordinateScaler coordinateScaler,
                        OcrService ocrService,
                        ColorDetectionService colorDetectionService,
                        ImageCropperService imageCropperService,
                        DetectionLoggerService detectionLoggerService,
                        ISetFlopForceBoardUseCase setFlopForceBoardUseCase,
                        IGetHashImageUseCase getHashImageUseCase,
                        IGetCropImageUseCase getCropImageUseCase,
                        IOutsCalculatorUseCase outsCalculatorUseCase,
                        GetWindowsScreenUseCase getWindowsScreenUseCase,
                        IGetCardsFlopUseCase getCardsFlopUseCase,
                        IGetCardsTurnUseCase getCardsTurnUseCase,
                        IGetCardsRiverUseCase getCardsRiverUseCase,
                        ISetPreflopActionUseCase setPreflopActionUseCase,
                        IGameCoordinator coordinator,
                        IScreenReaderService screenReader,
                        ITableLayoutService tableLayout,
                        IGameLoopCoordinator gameLoopCoordinator,
                        IUiSyncService uiSyncService,
                        IOptions<FeatureFlags> featureFlags,
                        IActionFormatter actionFormatter,
                        IOverlayPositioner overlayPositioner,
                        IPostflopContextHolder contextHolder,
                        ILogger<FrmMain> logger,
                        TextBoxLoggerProvider textBoxLoggerProvider,
                        IMetricsCollector metrics)
        {
            InitializeComponent();
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _gameLoopCoordinator = gameLoopCoordinator ?? throw new ArgumentNullException(nameof(gameLoopCoordinator));
            _uiSyncService = uiSyncService ?? throw new ArgumentNullException(nameof(uiSyncService));
            _featureFlags = featureFlags?.Value ?? throw new ArgumentNullException(nameof(featureFlags));
            _actionFormatter = actionFormatter ?? throw new ArgumentNullException(nameof(actionFormatter));
            _overlayPositioner = overlayPositioner ?? throw new ArgumentNullException(nameof(overlayPositioner));
            _contextHolder = contextHolder ?? throw new ArgumentNullException(nameof(contextHolder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _textBoxLoggerProvider = textBoxLoggerProvider ?? throw new ArgumentNullException(nameof(textBoxLoggerProvider));
            _textBoxLoggerProvider.SetTextBoxTarget(tbResume);

            // NUEVO: Aplicar estilos visuales ANTES de la inicialización
            //InitializeVisualStyles();

            _dataBase = dataBase ?? throw new ArgumentNullException(nameof(dataBase));
            _actionScenarioUseCases = actionScenarioUseCases ?? throw new ArgumentNullException(nameof(actionScenarioUseCases));
            _regionTableMapUseCases = regionTableMapUseCases ?? throw new ArgumentNullException(nameof(regionTableMapUseCases));
            _cardUseCases = cardUseCases ?? throw new ArgumentNullException(nameof(cardUseCases));
            _pokerCalculator = pokerCalculator ?? throw new ArgumentNullException(nameof(pokerCalculator));
            _betSizingService = betSizingService ?? throw new ArgumentNullException(nameof(betSizingService));
            _gameLoggerService = gameLoggerService ?? throw new ArgumentNullException(nameof(gameLoggerService));
            _detectionLoggerService = detectionLoggerService ?? throw new ArgumentNullException(nameof(detectionLoggerService));
            _gameLoopStateMachine = gameLoopStateMachine ?? throw new ArgumentNullException(nameof(gameLoopStateMachine));
            _strategyProfileService = strategyProfileService ?? throw new ArgumentNullException(nameof(strategyProfileService));
            _postflopDecisionService = postflopDecisionService ?? throw new ArgumentNullException(nameof(postflopDecisionService));
            _exploitabilityCalculator = exploitabilityCalculator ?? throw new ArgumentNullException(nameof(exploitabilityCalculator));
            _autoCalibrationService = autoCalibrationService ?? throw new ArgumentNullException(nameof(autoCalibrationService));
            _bankrollTrackerService = bankrollTrackerService ?? throw new ArgumentNullException(nameof(bankrollTrackerService));
            _boardTextureAnalyzer = boardTextureAnalyzer ?? throw new ArgumentNullException(nameof(boardTextureAnalyzer));
            _opponentTracker = opponentTracker ?? throw new ArgumentNullException(nameof(opponentTracker));
            _overlayConfig = overlayConfigOptions?.Value ?? new OverlayConfig();
            _regionLookupCache = regionLookupCache ?? throw new ArgumentNullException(nameof(regionLookupCache));
            _cardCacheService = cardCacheService ?? throw new ArgumentNullException(nameof(cardCacheService));
            _coordinateScaler = coordinateScaler ?? throw new ArgumentNullException(nameof(coordinateScaler));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _colorDetectionService = colorDetectionService ?? throw new ArgumentNullException(nameof(colorDetectionService));
            _imageCropperService = imageCropperService ?? throw new ArgumentNullException(nameof(imageCropperService));
            _setFlopForceBoardUseCase = setFlopForceBoardUseCase ?? throw new ArgumentNullException(nameof(setFlopForceBoardUseCase));
            _getHashImageUseCase = getHashImageUseCase ?? throw new ArgumentNullException(nameof(getHashImageUseCase));
            _getCropImageUseCase = getCropImageUseCase ?? throw new ArgumentNullException(nameof(getCropImageUseCase));
            _outsCalculatorUseCase = outsCalculatorUseCase ?? throw new ArgumentNullException(nameof(outsCalculatorUseCase));
            _useCase = getWindowsScreenUseCase ?? throw new ArgumentNullException(nameof(getWindowsScreenUseCase));
            _getCardsFlopUseCase = getCardsFlopUseCase ?? throw new ArgumentNullException(nameof(getCardsFlopUseCase));
            _getCardsTurnUseCase = getCardsTurnUseCase ?? throw new ArgumentNullException(nameof(getCardsTurnUseCase));
            _getCardsRiverUseCase = getCardsRiverUseCase ?? throw new ArgumentNullException(nameof(getCardsRiverUseCase));
            _setPreflopActionUseCase = setPreflopActionUseCase ?? throw new ArgumentNullException(nameof(setPreflopActionUseCase));
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _screenReader = screenReader ?? throw new ArgumentNullException(nameof(screenReader));
            _tableLayout = tableLayout ?? throw new ArgumentNullException(nameof(tableLayout));

            // Resto de inicialización existente...
            _session = GenerateRandomNumbers();
            _lastChecked = new RadioButton();

            _pathResume = Path.Combine(DEFAULT_SAMPLES_PATH,
                $"resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt");

            FormClosing += FrmMain_FormClosing;

            InitializeHistorialTab();
            InitializeBankrollTab();
            InitializeMetricsTab();
        }

        /// <summary>
        /// Al cerrar la aplicación, persiste la última mano y sesión activas para no perder datos.
        /// </summary>
        private bool _isClosing;

        private async void FrmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isClosing)
                return; // Ya estamos cerrando programáticamente

            // refactor-frmmain-coordinators Fase 6: parada limpia del coordinator si estaba activo.
            if (_gameLoopCoordinator.IsRunning)
            {
                try
                {
                    _uiSyncService.Detach();
                    _gameLoopCts?.Cancel();
                    await _gameLoopCoordinator.StopAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deteniendo GameLoopCoordinator al cerrar");
                }
            }

            if (_gameLoggerService.HasActiveHand || _gameLoggerService.HasActiveSession)
            {
                e.Cancel = true; // Cancelar el cierre para esperar al save
                _isClosing = true;

                try
                {
                    if (_gameLoggerService.HasActiveHand)
                    {
                        var closingStack = _heroStackPreRebuy > 0 ? _heroStackPreRebuy : (_playerGameState?.HeroStack ?? 0);
                        _gameLoggerService.EndHand(closingStack);
                    }

                    if (_gameLoggerService.HasActiveSession)
                        await _gameLoggerService.SaveSessionAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al guardar datos al cerrar");
                }

                Close(); // Ahora sí cerrar (entrará de nuevo pero _isClosing = true)
            }
        }

        /// <summary>
        /// Evento de carga del formulario
        /// </summary>
        private async void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                _formImage = new FormImage();
                _frmOverlay = new FrmOverlay(_overlayConfig);
                cbSpeed.SelectedIndex = 0;

                await using var sessionDB = _dataBase.LightweightSession();
                await LoadRegionTableMapAsync(sessionDB);
                await LoadTablesAsync(sessionDB);

                var allCards = await sessionDB.Query<Card>().ToListAsync();
                _cards.AddRange(allCards);

                _formImage.Location = new Point(Width, Location.Y);
                _formImage.Show();

                UpdateBankrollDashboard();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el formulario");
            }
        }

        /// <summary>
        /// Carga la configuración de regiones de la tabla desde la base de datos
        /// </summary>
        private async Task LoadRegionTableMapAsync()
        {
            await using var session = _dataBase.LightweightSession();
            await LoadRegionTableMapAsync(session);
        }

        private async Task LoadRegionTableMapAsync(IDocumentSession session)
        {
            try
            {
                var regions = new List<Domain.ValueObjects.Region>();
                var regionsTableMap = await session.Query<RegionTableMap>().ToListAsync();

                var categories = regionsTableMap
                    .Where(x => x.Regions != null)
                    .SelectMany(x => x.Regions!)
                    .Distinct()
                    .ToList();

                regions.AddRange(categories);
                _regionsTableMap = regionsTableMap.ToList();
                _regionLookupCache.Initialize(_regionsTableMap);

                LoadTreeViewRegions(regionsTableMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar regiones");
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
                _logger.LogError(ex, "Error al cargar tablas");
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
                _logger.LogError(ex, "Error selecting region");
            }
        }

        /// <summary>
        /// Actualiza la visualización de la región seleccionada
        /// </summary>
        private void UpdateRegionDisplay()
        {
            using (var lapiz = new Pen(Color.Red))
            using (_papel = _formImage.pbImage.CreateGraphics())
            {
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
                    _logger.LogInformation("Error de validación: umbral={Umbral}, inact={Inact}, X={X}, Y={Y}, W={W}, H={H}", umbral, inactUmbral, tbX.Text, tbY.Text, tbWidth.Text, tbHeight.Text);
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
                _logger.LogInformation("Región actualizada: {Region}", _selectedRegion.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la región");
            }
        }

        #endregion

        /// <summary>
        /// Captura y procesa la información de la mesa de póker
        /// </summary>
        private async void btnCapture_Click(object sender, EventArgs e)
        {
            using var _cycleTimer = _metrics?.Measure(TelemetryCategories.CycleTotal);
            Interlocked.Increment(ref _cycleCounter);
            try
            {
                lbAction.Text = string.Empty;
                _frmOverlay.UpdateEquityPercentage(string.Empty);
                _frmOverlay.UpdatePotOddsPercentage(string.Empty);
                _frmOverlay.UpdateStreetPhase(string.Empty);
                _frmOverlay.UpdateTableName(_tableName);
                lbPositionAction.Text = string.Empty;
                _executeCapture = true;
                var potOddsResult = new PokerCalculationResult();

                if (!cbTest.Checked)
                {
                    _frmOverlay.UpdateAction(string.Empty);

                    if (cbMark.Checked)
                        CreateLogWithMarkedHands();

                    using var _captureTimer = _metrics?.Measure(TelemetryCategories.CaptureScreenshot);
                    await GetImageWhilePlaying();
                    _formImage.WindowState = FormWindowState.Minimized;
                }

                if (cbTest.Checked)
                    _frmOverlay.Show();

                // En modo test, determinar si es postflop (flop/turn/river seleccionado)
                bool isTestPostflop = cbTest.Checked && (rbFlop.Checked || rbTurn.Checked || rbRiver.Checked);

                // Guardar el número de mano ANTES de SetTableHand para detectar si cambió
                var handNumberBeforeUpdate = _tableHand;

                await SetTableHand();

                if (_newHand && !isTestPostflop)
                {
                    // Guardar datos antes de resetear PlayerGameState para poder loguearlos y
                    // pasarlos a StartNewHand (SetTableHand ya los leyó via ObtainCardsPlayerAsync)
                    var prevPot = _playerGameState?.PotSize ?? 0;
                    var prevHoleCards = $"{_playerGameState?.HoleCard1Face} {_playerGameState?.HoleCard2Face}".Trim();
                    var prevHoleCard1 = _playerGameState?.HoleCard1Face ?? string.Empty;
                    var prevHoleCard2 = _playerGameState?.HoleCard2Face ?? string.Empty;
                    var prevPosition = _playerGameState?.Position ?? TablePosition.None;
                    // Usar stack pre-rebuy para calcular profit real (auto-rebuy a 100BB no contamina)
                    var prevHeroStack = _heroStackPreRebuy > 0 ? _heroStackPreRebuy : (_playerGameState?.HeroStack ?? 0);

                    // Guardar estado postflop ANTES de resetear PlayerGameState, si aplica.
                    // Solo se guarda si es la misma mano (mismo hand number).
                    GameState? savedPostflopState = null;
                    List<BoardData>? savedBoardCards = null;
                    bool sameHand = !string.IsNullOrEmpty(handNumberBeforeUpdate) && handNumberBeforeUpdate == _tableHand;
                    if (!cbTest.Checked && sameHand &&
                        (_gameLoopStateMachine.IsFlop ||
                         _gameLoopStateMachine.IsTurn ||
                         _gameLoopStateMachine.IsRiver))
                    {
                        savedPostflopState = _gameLoopStateMachine.CurrentState;
                        // Preservar las cartas del board para no perderlas al resetear PlayerGameState
                        savedBoardCards = _playerGameState?.BoardCards?
                            .Where(b => b.Position != BoardPosition.Hand)
                            .ToList();
                        _logger.LogInformation("Guardando estado postflop antes de reset: {State}, BoardCards: {Count}", savedPostflopState, savedBoardCards?.Count ?? 0);
                    }

                    _frmOverlay?.ClearAll();
                    _playerGameState = new PlayerGameState();
                    _responseAction = new ResponseAction();
                    _heroStackPreRebuy = 0; // Reset para nueva mano
                    _preflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
                    _tableLayout.ResetDealerState();
                    _newHand = false;

                    // State machine: transicionar a nueva mano
                    _gameLoopStateMachine.Reset();
                    _gameLoopStateMachine.TryTransition(GameState.HandDetected);

                    if (!cbTest.Checked)
                        await HandleNewHandAsync(prevPot, prevHoleCards, prevHoleCard1, prevHoleCard2, prevPosition, prevHeroStack);

                    // Restaurar estado postflop solo si es la misma mano.
                    // Si el número de mano cambió es una mano nueva y debe fluir por preflop.
                    // Usar ForceState porque HandDetected → FlopDetected/TurnDetected/RiverDetected
                    // no son transiciones válidas en la máquina de estados.
                    // Siempre resetear contexto postflop para evitar state bleed entre manos
                    _contextHolder.StartNewHand();

                    if (savedPostflopState.HasValue)
                    {
                        _logger.LogInformation("Restaurando estado postflop: {State}, BoardCards: {Count}", savedPostflopState, savedBoardCards?.Count ?? 0);
                        _gameLoopStateMachine.ForceState(savedPostflopState.Value);
                        // Restaurar board cards solo si son válidas (tienen nombres no vacíos)
                        if (savedBoardCards != null && savedBoardCards.Count > 0 &&
                            savedBoardCards.All(b => !string.IsNullOrEmpty(b.Name)))
                            _playerGameState.BoardCards = savedBoardCards;
                    }
                }
                else if (_newHand && isTestPostflop)
                {
                    _newHand = false;
                }

                // ForceState después del reset para que no se sobreescriba el estado forzado
                if (cbTest.Checked)
                {
                    if (rbFlop.Checked)
                        _gameLoopStateMachine.ForceState(GameState.FlopDetected);
                    else if (rbTurn.Checked)
                        _gameLoopStateMachine.ForceState(GameState.TurnDetected);
                    else if (rbRiver.Checked)
                        _gameLoopStateMachine.ForceState(GameState.RiverDetected);
                }

                _logger.LogInformation("ProcessNewHand: Players={PlayerCount}, cbTest={Test}, isTestPostflop={TestPostflop}", _playerGameState.Players.Count(), cbTest.Checked, isTestPostflop);

                // Inicializar jugadores si:
                // 1. No hay jugadores, O
                // 2. Es modo Test y no es postflop, O
                // 3. El dealer no se ha detectado correctamente
                bool needsInitialization = _playerGameState.Players.Count() == 0 ||
                                          (cbTest.Checked && !isTestPostflop) ||
                                          _playerGameState.Position == TablePosition.None;

                if (needsInitialization)
                {
                    _tableLayout.SetEmptyPlayer(_formImage.pbImage.Image, _playerGameState);
                    _tableLayout.SetSitOutPlayer(_formImage.pbImage.Image, _playerGameState);
                    _tableLayout.SetActivePlayer(_formImage.pbImage.Image, _playerGameState);
                    _tableLayout.InitializePlayers(_formImage.pbImage.Image, _playerGameState);
                }
                else
                {
                    _tableLayout.SetEmptyPlayer(_formImage.pbImage.Image, _playerGameState);
                    _tableLayout.SetActivePlayer(_formImage.pbImage.Image, _playerGameState);
                    _tableLayout.RefreshPlayerStates(_formImage.pbImage.Image, _playerGameState);

                    int currentActiveCount = _playerGameState.Players.Count(p => !p.Empty && !p.SitOut && p.ValuePosition != 0);
                    bool playerCountChanged = currentActiveCount != _lastActivePlayerCount;
                    _lastActivePlayerCount = currentActiveCount;

                    _logger.LogDebug("Jugadores activos: {Active}, Cambió: {Changed}, Posición actual: {Position}", currentActiveCount, playerCountChanged, _playerGameState.Position);

                    bool shouldRecalculate = _playerGameState.Position == TablePosition.None || playerCountChanged;

                    if (shouldRecalculate)
                    {
                        _tableLayout.SetDealerPlayer(_formImage.pbImage.Image, _playerGameState);
                        if (_tableLayout.DealerValuePosition >= 0 && _playerGameState.Position != TablePosition.None)
                            _tableLayout.SetVillainPosition(_playerGameState, _playerGameState.Position, _tableLayout.DealerValuePosition);
                    }
                }

                SetBetPlayer();
                SetHeroStack();
                _tableLayout.RetryEmptyAliases(_formImage.pbImage.Image, _playerGameState);
                _tableLayout.ValidatePlayerStates(_playerGameState);

                // Procesar la información de la mesa
                await ProcessTableInfoAsync(potOddsResult);

                // Actualizar la interfaz con los resultados
                UpdateUIWithResults(potOddsResult);

                // Telemetry: medir render del overlay
                using var _overlayTimer = _metrics?.Measure(TelemetryCategories.OverlayRender);

                var telemetry = _metrics?.EndHand();
                if (telemetry != null)
                {
                    _gameLoggerService.SetTelemetry(telemetry);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la captura");
            }
        }

        /// <summary>
        /// Abre la ventana de debug de detección de turnos
        /// </summary>
        private void BtnDetectionDebug_Click(object sender, EventArgs e)
        {
            try
            {
                // Obtener la región uAction
                var regionAction = _regionLookupCache.GetRegion("User", "uAction");

                if (regionAction == null)
                {
                    MessageBox.Show("No se encontró la región 'uAction'. Por favor, carga la configuración de regiones primero.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_handle == IntPtr.Zero)
                {
                    MessageBox.Show("No hay una ventana de poker seleccionada. Por favor, selecciona la ventana primero.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Crear y mostrar la ventana de debug
                var debugForm = new FrmDetectionDebug(_handle, regionAction);
                debugForm.Show();

                _detectionLoggerService.LogDetectionError("Ventana de debug de detección abierta");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la ventana de debug: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _detectionLoggerService.LogDetectionError($"Error al abrir ventana de debug: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Muestra el análisis de exploitabilidad de la sesión
        /// </summary>
        private void BtnExploitability_Click(object sender, EventArgs e)
        {
            try
            {
                var gtoDistance = _exploitabilityCalculator.CalculateGTODistance();
                var sessionAnalysis = _exploitabilityCalculator.CalculateSessionAnalysis();

                string message = $"=== Análisis GTO ===\n\n" +
                    $"Status: {gtoDistance.Status}\n" +
                    $"Distancia: {gtoDistance.DistanceMbb:F1} mbb/hand\n" +
                    $"Decisiones analizadas: {gtoDistance.TotalDecisionsAnalyzed}\n" +
                    $"% Exploitables: {gtoDistance.ExploitablePercentage:F1}%\n\n" +
                    $"=== Sesión ===\n\n" +
                    $"Total decisiones: {sessionAnalysis.TotalDecisions}\n" +
                    $"Promedio mbb: {sessionAnalysis.AverageExploitabilityMbb:F1}\n" +
                    $"Máximo mbb: {sessionAnalysis.MaxExploitabilityMbb:F1}\n";

                if (sessionAnalysis.ExploitabilityByStreet.Any())
                {
                    message += $"\nPor calle:\n";
                    foreach (var kvp in sessionAnalysis.ExploitabilityByStreet)
                    {
                        message += $"  {kvp.Key}: {kvp.Value:F1} mbb\n";
                    }
                }

                if (sessionAnalysis.ExploitabilityByPosition.Any())
                {
                    message += $"\nPor posición:\n";
                    foreach (var kvp in sessionAnalysis.ExploitabilityByPosition)
                    {
                        message += $"  {kvp.Key}: {kvp.Value:F1} mbb\n";
                    }
                }

                if (sessionAnalysis.TopLeaks.Any())
                {
                    message += $"\nTop leaks:\n";
                    foreach (var leak in sessionAnalysis.TopLeaks.Take(3))
                    {
                        message += $"  {leak.Category}: {leak.Frequency} veces, {leak.AverageExploitabilityMbb:F1} mbb avg\n";
                    }
                }

                if (gtoDistance.Recommendations.Any())
                {
                    message += $"\nRecomendaciones:\n";
                    foreach (var rec in gtoDistance.Recommendations)
                    {
                        message += $"  - {rec}\n";
                    }
                }

                MessageBox.Show(message, "Análisis GTO", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al analizar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Ejecuta la calibración automática de parámetros
        /// </summary>
        private void BtnCalibrate_Click(object sender, EventArgs e)
        {
            try
            {
                var preview = _autoCalibrationService.GetPreview(
                    _exploitabilityCalculator,
                    _strategyProfileService.Profile);

                if (preview.ProposedAdjustments.Count == 0)
                {
                    MessageBox.Show(
                        $"No hay suficientes datos para calibrar.\n" +
                        $"Decisiones actuales: {_exploitabilityCalculator.CalculateSessionAnalysis().TotalDecisions}\n" +
                        $"Mínimo requerido: 20",
                        "Calibración",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string message = "=== Vista Previa de Calibración ===\n\n";
                message += $"Explotabilidad actual: {preview.CurrentExploitability:F1} mbb/hand\n";
                message += $"Explotabilidad estimada: {preview.EstimatedNewExploitability:F1} mbb/hand\n\n";
                message += "Ajustes propuestos:\n";

                foreach (var adj in preview.ProposedAdjustments)
                {
                    message += $"\n{adj.ParameterName}:\n";
                    message += $"  Anterior: {adj.OldValue:F1}\n";
                    message += $"  Nuevo: {adj.NewValue:F1}\n";
                    message += $"  Razón: {adj.Reason}\n";
                }

                var result = MessageBox.Show(
                    message + "\n\n¿Aplicar ajustes?",
                    "Confirmar Calibración",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var calibrationResult = _autoCalibrationService.Calibrate(
                        _exploitabilityCalculator,
                        _strategyProfileService.Profile);

                    MessageBox.Show(
                        calibrationResult.Message,
                        "Calibración",
                        MessageBoxButtons.OK,
                        calibrationResult.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calibrar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Actualiza el dashboard de bankroll con las métricas actuales
        /// </summary>
        private void UpdateBankrollDashboard()
        {
            try
            {
                var stats = _bankrollTrackerService.GetBankrollStats();

                // Panel Bankroll
                lblBankrollCurrent.Text = $"Bankroll: €{stats.CurrentBankroll:N2}";
                lblBankrollPeak.Text = $"Peak: €{stats.PeakBankroll:N2}";
                lblBankrollMaxDD.Text = $"Max Drawdown: -{stats.MaxDrawdownPercent:F1}%";

                // Color MaxDD según spec
                lblBankrollMaxDD.ForeColor = stats.MaxDrawdownPercent switch
                {
                    < 10 => AppThemeHelper.Success,
                    < 20 => AppThemeHelper.Warning,
                    _ => AppThemeHelper.Danger
                };

                // Panel Rendimiento
                lblWinRate.Text = $"Win Rate: {stats.WinRateBB100:F1} BB/100";
                lblWinRate.ForeColor = stats.WinRateBB100 switch
                {
                    > 5 => AppThemeHelper.Success,
                    >= 0 => AppThemeHelper.Warning,
                    _ => AppThemeHelper.Danger
                };

                lblStdDev.Text = $"Std Dev: {stats.StdDeviation:F1} BB/100";

                var winPct = stats.TotalSessions > 0
                    ? (double)stats.WinningSessions / stats.TotalSessions * 100
                    : 0;
                lblTotalSessions.Text = $"Sesiones: {stats.TotalSessions} ({stats.WinningSessions} ganadas, {winPct:F0}%)";
                lblTotalHands.Text = $"Manos: {stats.TotalHands}";
                lblAvgSessionProfit.Text = $"Media: €{stats.AverageSessionProfit:N2} / sesión";

                // Panel Riesgo
                var rorPercent = stats.RiskOfRuin * 100;
                lblRiskOfRuin.Text = $"Risk of Ruin: {rorPercent:F1}%";
                lblRiskOfRuin.ForeColor = stats.RiskLevel switch
                {
                    "Green" => AppThemeHelper.Success,
                    "Yellow" => AppThemeHelper.Warning,
                    _ => AppThemeHelper.Danger
                };

                lblRecommendation.Text = stats.LimitRecommendation;
                lblRecommendation.ForeColor = stats.LimitRecommendation switch
                {
                    var r when r.StartsWith("SUBIR") => AppThemeHelper.Success,
                    var r when r.StartsWith("BAJAR") => AppThemeHelper.Danger,
                    _ => AppThemeHelper.Warning
                };

                // Historial en DataGridView
                var history = _bankrollTrackerService.GetHistory(50);
                var displayData = history.Select(h => new
                {
                    Date = h.Date.ToString("dd/MM/yyyy"),
                    h.Hands,
                    Profit = h.Profit.ToString("+0.00;-0.00"),
                    BBPer100 = h.BBPer100.ToString("+0.0;-0.0"),
                    BankrollAfter = h.BankrollAfter.ToString("N2")
                }).ToList();

                dgvBankrollHistory.DataSource = displayData;
            }
            catch
            {
                lblBankrollCurrent.Text = "Bankroll: €0.00";
                lblRecommendation.Text = "Error al cargar stats";
            }
        }

        /// <summary>
        /// Procesa la información de la mesa
        /// </summary>
        private async Task ProcessTableInfoAsync(PokerCalculationResult potOddsResult)
        {
            SetPotValue();
            _gameLoggerService.UpdatePotSize(_playerGameState.PotSize);
            _preflopHeroPosition = GetPreflopHeroPosition();

            if (!IsFlop && !IsTurn && !IsRiver)
            {
                bool hasHoleCards = !string.IsNullOrEmpty(_playerGameState?.HoleCard1Face) &&
                                    !string.IsNullOrEmpty(_playerGameState?.HoleCard2Face);

                if (!hasHoleCards)
                {
                    int maxRetries = 2;
                    int retryDelayMs = 200;

                    for (int retry = 0; retry < maxRetries && !hasHoleCards; retry++)
                    {
                        if (retry > 0)
                        {
                            await Task.Delay(retryDelayMs);
                        }

                        await ObtainCardsPlayerAsync();

                        hasHoleCards = !string.IsNullOrEmpty(_playerGameState?.HoleCard1Face) &&
                                       !string.IsNullOrEmpty(_playerGameState?.HoleCard2Face);
                    }

                    if (!hasHoleCards)
                    {
                        _logger.LogError("HoleCards no detectadas después de reintentos, saltando procesamiento preflop");
                        return;
                    }
                }

                _gameLoopStateMachine.TryTransition(GameState.PreflopAction);
                await ProcessPreflopAsync();
            }
            else
            {
                // Asegurar que las hole cards estén leídas (pueden perderse si _newHand resetea PlayerGameState)
                bool hasHoleCards = !string.IsNullOrEmpty(_playerGameState?.HoleCard1Face) &&
                                    !string.IsNullOrEmpty(_playerGameState?.HoleCard2Face);

                if (!hasHoleCards)
                {
                    for (int retry = 0; retry < 2 && !hasHoleCards; retry++)
                    {
                        if (retry > 0)
                            await Task.Delay(200);

                        await ObtainCardsPlayerAsync();

                        hasHoleCards = !string.IsNullOrEmpty(_playerGameState?.HoleCard1Face) &&
                                       !string.IsNullOrEmpty(_playerGameState?.HoleCard2Face);
                    }

                    if (!hasHoleCards)
                    {
                        _logger.LogError("HoleCards no detectadas para postflop, saltando procesamiento");
                        return;
                    }
                }

                await ProcessPostFlopAsync(potOddsResult);
            }
        }

        /// <summary>
        /// Procesa la fase de preflop
        /// </summary>
        private async Task ProcessPreflopAsync()
        {
            if (_playerGameState.Position == TablePosition.None)
            {
                _logger.LogError("Posición del jugador no detectada, saltando procesamiento preflop");
                return;
            }

            if (_preflopHeroPosition == null || !_preflopHeroPosition.ContainsKey(_playerGameState.Position))
            {
                _logger.LogError("PreflopHeroPosition no tiene datos para posición {Position}, reconstruyendo...", _playerGameState.Position);
                _preflopHeroPosition = GetPreflopHeroPosition();

                if (!_preflopHeroPosition.ContainsKey(_playerGameState.Position))
                {
                    _logger.LogError("Sigue sin tener datos para posición {Position}, saltando preflop", _playerGameState.Position);
                    return;
                }
            }

            var responseFlop = await _setPreflopActionUseCase.Execute(new SetPreflopActionUseCaseRequest
            {
                ResponseAction = _responseAction,
                PlayerState = _playerGameState,
                PreflopHeroPosition = _preflopHeroPosition
            });

            _responseAction = responseFlop.ResponseAction;
            _playerGameState = responseFlop.PlayerState;

            _frmOverlay.UpdateStreetPhase("Pre-Flop");
            SetPreflopAggressors();
        }

        private void SetPreflopAggressors()
        {
            var maxBet = _playerGameState.Players.Max(p => p.Bet);
            if (maxBet <= 1)
                return;

            var bigBlind = 1m;
            var playersWhoRaised = _playerGameState.Players
                .Where(p => p.Active && p.Bet > bigBlind)
                .ToList();

            foreach (var player in playersWhoRaised)
            {
                player.WasPreflopAggressor = true;
            }

            if (playersWhoRaised.Count > 0)
            {
                _logger.LogError("[PREFLOP] Aggressors set: {Aggressors}", string.Join(", ", playersWhoRaised.Select(p => $"{p.Name}({p.Position}):{p.Bet}")));
            }
        }

        /// <summary>
        /// Procesa las fases posteriores al flop (flop, turn, river)
        /// </summary>
        private async Task ProcessPostFlopAsync(PokerCalculationResult potOddsResult)
        {
            _logger.LogInformation("ProcessPostFlopAsync: Estado actual = {State}", _gameLoopStateMachine.CurrentState);

            // Detectar villanos que foldearon mid-hand (actualiza Active/numOpponents)
            _tableLayout.DetectFoldedPlayers(_formImage.pbImage.Image, _playerGameState, _gameLoopStateMachine.CurrentState);

            // Detectar transición a nueva calle verificando cartas visibles en el board.
            // Validar con conteo REAL de cartas (no hardcodeado) para evitar falsos positivos.
            if (_gameLoopStateMachine.CurrentState == GameState.FlopAction)
            {
                bool card4Visible = IsBoardCardVisible("Card4");
                bool transitioned = false;

                if (card4Visible)
                {
                    int visibleCards = CountVisibleBoardCards();
                    transitioned = _gameLoopStateMachine.TryTransition(GameState.TurnDetected, visibleCards);
                    if (!transitioned)
                        _logger.LogInformation("Transición a Turn bloqueada: Card4 visible pero solo {Visible} cartas detectadas (necesita 4)", visibleCards);
                }

                if (!transitioned)
                {
                    // Misma calle, reprocessar flop con info actualizada (pot y bets pueden haber cambiado)
                    SetPotValue();
                    var reprocessMaxBet = _playerGameState.Players.Max(m => m.Bet);
                    var reprocessFlopBetSize = GetOpponentBetSize(reprocessMaxBet, _playerGameState.PotSize);
                    _contextHolder.Update(c => c with
                    {
                        VillainBetSizeFlop = reprocessFlopBetSize,
                        VillainBetFlop = reprocessMaxBet > 0
                    });
                    await ProcessFlopAsync(potOddsResult);
                }
            }

            if (_gameLoopStateMachine.CurrentState == GameState.TurnAction)
            {
                bool card5Visible = IsBoardCardVisible("Card5");
                bool transitioned = false;

                if (card5Visible)
                {
                    int visibleCards = CountVisibleBoardCards();
                    transitioned = _gameLoopStateMachine.TryTransition(GameState.RiverDetected, visibleCards);
                    if (!transitioned)
                        _logger.LogInformation("Transición a River bloqueada: Card5 visible pero solo {Visible} cartas detectadas (necesita 5)", visibleCards);
                }

                if (!transitioned)
                {
                    // Misma calle, reprocessar turn con info actualizada (pot y bets pueden haber cambiado)
                    SetPotValue();
                    var reprocessMaxBet = _playerGameState.Players.Max(m => m.Bet);
                    var reprocessTurnBetSize = GetOpponentBetSize(reprocessMaxBet, _playerGameState.PotSize);
                    _contextHolder.Update(c => c with
                    {
                        VillainBetSizeTurn = reprocessTurnBetSize,
                        VillainBetTurn = reprocessMaxBet > 0
                    });
                    await ProcessTurnAsync();
                }
            }

            if (_gameLoopStateMachine.CurrentState == GameState.FlopDetected)
            {
                _gameLoopStateMachine.TryTransition(GameState.FlopAction);
                await ProcessFlopAsync(potOddsResult);
            }

            if (_gameLoopStateMachine.CurrentState == GameState.TurnDetected)
            {
                _gameLoopStateMachine.TryTransition(GameState.TurnAction);
                await ProcessTurnAsync();
            }

            if (_gameLoopStateMachine.CurrentState == GameState.RiverDetected)
            {
                _gameLoopStateMachine.TryTransition(GameState.RiverAction);
                await ProcessRiverAsync();
            }

            _responseAction.Action = string.IsNullOrEmpty(_responseAction.Action)
                ? "No Preflop action"
                : _responseAction.Action;

            _frmOverlay.UpdateAction(_responseAction.Action);
        }

        /// <summary>
        /// Verifica si una carta del board (Card4=turn, Card5=river) es visible en la mesa
        /// comparando la imagen de la región contra las cartas conocidas.
        /// </summary>
        private bool IsBoardCardVisible(string cardRegionName)
        {
            if (_formImage.pbImage.Image == null || _cardsImages == null || !_cardsImages.Any())
                return false;

            var cardRegion = _regionLookupCache.GetRegion("Board", cardRegionName);
            if (cardRegion != null && cardRegion.IsHash != true)
                cardRegion = null;
            if (cardRegion == null)
                return false;

            var scaled = GetScaledRegion(cardRegion);
            var imageToBase64 = _imageCropperService.CropImageToBase64(
                _formImage.pbImage.Image, scaled.X, scaled.Y, scaled.Width, scaled.Height);

            var bestMatch = _cardsImages
                .Where(item => !string.IsNullOrEmpty(item.ImageBase64))
                .Select(item => _imageCropperService.CompareCardsBase64(item.ImageBase64, imageToBase64))
                .DefaultIfEmpty(0)
                .Max();

            // Umbral de confianza: >80% indica carta real, <80% indica fondo de mesa
            _logger.LogDebug("IsBoardCardVisible({Region}): bestMatch={Match:F1}%, visible={Visible}", cardRegionName, bestMatch, bestMatch > 80.0);
            return bestMatch > 80.0;
        }

        /// <summary>
        /// Cuenta cuántas cartas del board son realmente visibles (Card1 a Card5).
        /// Las cartas son secuenciales: si Card3 no es visible, Card4/5 tampoco.
        /// </summary>
        private int CountVisibleBoardCards()
        {
            int count = 0;
            string[] cardRegions = { "Card1", "Card2", "Card3", "Card4", "Card5" };
            foreach (var region in cardRegions)
            {
                if (IsBoardCardVisible(region))
                    count++;
                else
                    break;
            }
            return count;
        }

        #region [Legacy Turn/River Handlers - REMOVED]
        // 10 handlers legacy eliminados (HandleOpenRaiseTurnAction, HandleCallTurnAction, etc.)
        // Reemplazados por PostflopDecisionService.DetermineAction()
        #endregion


        /// <summary>
        /// Determina la acción del river delegando a GameCoordinator.
        /// </summary>
        private void DetermineRiverAction()
        {
            _coordinator.SetRiverResult(_riverResult);
            var result = _coordinator.DetermineRiverAction(_playerGameState);
            _responseAction.Action = result.Action;
            _logger.LogError("{LogText}", result.LogText);
        }

        /// <summary>
        /// Analiza la textura del board del turn
        /// </summary>
        /// <summary>
        /// Analiza el cambio de board al caer una nueva carta.
        /// previousCardCount indica cuántas cartas había antes (3 para turn, 4 para river).
        /// </summary>
        // CombineBoardChanges extraído a PostflopGameContext

        private BoardChangeResult AnalyzeBoardChange(List<BoardData> boardCards, int previousCardCount)
            => _coordinator.AnalyzeBoardChange(boardCards, previousCardCount);

        private TurnBoardTexture AnalyzeTurnBoardTexture(List<BoardData> boardCards)
            => _coordinator.AnalyzeTurnBoardTexture(boardCards);

        private RiverBoardTexture AnalyzeRiverBoardTexture(List<BoardData> boardCards)
            => _coordinator.AnalyzeRiverBoardTexture(boardCards);

        /// <summary>
        /// Detecta si se ha iniciado una nueva mano usando múltiples indicadores
        /// </summary>
        private bool DetectNewHand(bool handNumberChanged, string currentHand)
        {
            // Indicador 1: Número de mano cambió
            bool indicator1 = handNumberChanged;

            // Indicador 2: Hole cards detectadas (nueva mano)
            bool indicator2 = !string.IsNullOrEmpty(_playerGameState?.HoleCard1Face) &&
                              !string.IsNullOrEmpty(_playerGameState?.HoleCard2Face);

            // Indicador 3: Bote bajo (reset típico de nueva mano)
            bool indicator3 = _playerGameState != null && _playerGameState.PotSize < 10; // Ajustar umbral según blinds

            // Indicador 4: Board vacío o solo preflop
            bool indicator4 = _playerGameState != null &&
                              _playerGameState.BoardCards.Count(c => c.Position != BoardPosition.Hand) == 0;

            // Indicador 5: Dealer cambió (nueva ronda)
            string currentDealerPlayerName = _playerGameState?.Players.FirstOrDefault(d => d.Dealer == true)?.Name ?? "";
            bool indicator5 = !string.IsNullOrEmpty(currentDealerPlayerName) && currentDealerPlayerName != _tableLayout.PreviousDealerPlayerName;

            // Indicador 6: SB cambió
            string currentSBPlayerName = _playerGameState?.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "";
            bool indicator6 = !string.IsNullOrEmpty(currentSBPlayerName) && currentSBPlayerName != _previousSBPlayerName;

            // Indicador 7: BB cambió
            string currentBBPlayerName = _playerGameState?.Players.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "";
            bool indicator7 = !string.IsNullOrEmpty(currentBBPlayerName) && currentBBPlayerName != _previousBBPlayerName;

            // Log indicadores para debugging
            _logger.LogDebug("DetectNewHand - HandChanged: {I1}, HoleCards: {I2}, PotLow: {I3}, BoardEmpty: {I4}, DealerChanged: {I5}, SBChanged: {I6}, BBChanged: {I7}", indicator1, indicator2, indicator3, indicator4, indicator5, indicator6, indicator7);

            int secondaryCount = (indicator2 ? 1 : 0) + (indicator3 ? 1 : 0) + (indicator4 ? 1 : 0) + (indicator5 ? 1 : 0) + (indicator6 ? 1 : 0) + (indicator7 ? 1 : 0);

            // Ruta principal: hand number cambió + al menos 1 indicador secundario
            // Ruta fallback: hand number no disponible (OCR falló) + al menos 3 indicadores secundarios
            bool isNewHand = indicator1
                ? secondaryCount >= 1
                : secondaryCount >= 3;

            // Actualizar nombres previos si se detectó nueva mano
            if (isNewHand)
            {
                _tableLayout.SavePreviousDealer();
                _previousSBPlayerName = currentSBPlayerName;
                _previousBBPlayerName = currentBBPlayerName;
            }

            return isNewHand;
        }

        /// <summary>
        /// Procesa la fase de flop
        /// </summary>
        private async Task ProcessFlopAsync(PokerCalculationResult potOddsResult)
        {
            _logger.LogInformation("ProcessFlopAsync: Iniciando procesamiento de flop - Estado actual: {State}", _gameLoopStateMachine.CurrentState);

            // Capturar cartas del flop con retry
            List<BoardData> dataBoard = null!;
            for (int attempt = 0; attempt <= GameLoopStateMachine.MaxOcrRetries; attempt++)
            {
                using var bitmap = new Bitmap(_formImage.pbImage.Image);
                var flopResponse = await _getCardsFlopUseCase.ExecuteAsync(new GetCardsFlopUseCaseRequest
                {
                    Image = bitmap,
                    RegionsTableMap = _regionsTableMap,
                    CurrentImageWidth = bitmap.Width,
                    CurrentImageHeight = bitmap.Height
                });

                dataBoard = flopResponse.DataBoard;
                _logger.LogInformation("ProcessFlopAsync: Intento {Attempt} - Cartas detectadas: {Total}, Flop cards: {FlopCount}", attempt + 1, dataBoard.Count, dataBoard.Count(d => d.Position == BoardPosition.Flop));

                if (dataBoard.Count(d => d.Position == BoardPosition.Flop) >= 3)
                    break;

                if (attempt < GameLoopStateMachine.MaxOcrRetries)
                {
                    _logger.LogError("OCR flop: intento {Attempt} falló, reintentando...", attempt + 1);
                    await Task.Delay(200);
                }
            }

            _playerGameState.BoardCards = dataBoard;

            dataBoard.Add(new BoardData { Force = _playerGameState.HoleCard1Rank, Suit = _playerGameState.HoleCard1Suit, Position = BoardPosition.Hand, Name = _playerGameState.HoleCard1Face, Location = 0 });
            dataBoard.Add(new BoardData { Force = _playerGameState.HoleCard2Rank, Suit = _playerGameState.HoleCard2Suit, Position = BoardPosition.Hand, Name = _playerGameState.HoleCard2Face, Location = 0 });

            // Procesar el flop
            var setFlopForceBoardResponse = _setFlopForceBoardUseCase.Execute(
                new SetFlopForceBoardUseCaseRequest
                {
                    PlayerState = _playerGameState,
                    TableScrapeFlopResult = _scrapeFlopResult
                });

            _playerGameState = setFlopForceBoardResponse.PlayerState;
            _scrapeFlopResult = setFlopForceBoardResponse.TableScrapeFlopResult;

            var myCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                new CardDataOuts((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
            };

            // Verificar que hay suficientes cartas del flop tras reintentos
            if (dataBoard.Count(d => d.Position == BoardPosition.Flop) < 3)
            {
                _logger.LogError("No se detectaron suficientes cartas del flop tras reintentos.");
                _responseAction.Action = "Error: No se pudieron detectar cartas del flop";
                UpdateOverlayWithPotOdds(new PokerCalculationResult());
                return;
            }

            var communityCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)dataBoard.First(d => d.Position == BoardPosition.Flop && d.Location == 1).Suit, (Rank)dataBoard.First(d => d.Position == BoardPosition.Flop && d.Location == 1).Force),
                new CardDataOuts((Suit)dataBoard.First(d => d.Position == BoardPosition.Flop && d.Location == 2).Suit, (Rank)dataBoard.First(d => d.Position == BoardPosition.Flop && d.Location == 2).Force),
                new CardDataOuts((Suit)dataBoard.First(d => d.Position == BoardPosition.Flop && d.Location == 3).Suit, (Rank)dataBoard.First(d => d.Position == BoardPosition.Flop && d.Location == 3).Force)
            };

            var result = _pokerCalculator.Calculate(
                myCards,
                communityCards,
                _playerGameState.PotSize,
                _playerGameState.Players.Max(m => m.Bet),
                isInPosition: _playerGameState.IsInPosition,
                heroStack: _playerGameState.HeroStack,
                villainStack: GetVillainStack(),
                handSituation: _playerGameState.HandSituation.ToString(),
                opponentProfile: _coordinator.GetActiveVillainProfile(_playerGameState));

            _flopResult = result;
            UpdateOverlayWithPotOdds(result);

            // Determinar si estamos en posición
            _tableLayout.SetIsInPosition(_playerGameState);

            // Analizar el flop y determinar acción usando PostflopDecisionService (unificado con turn/river)
            DetermineFlopActionUnified();

            // Persistir flop board y decisión
            var flopCardNames = _playerGameState.BoardCards
                .Where(b => b.Position == BoardPosition.Flop)
                .OrderBy(b => b.Location)
                .Select(b => b.Name ?? string.Empty)
                .ToList();
            _gameLoggerService.UpdateBoard(flopCardNames);
            decimal flopMaxBet = _playerGameState.Players.Max(m => m.Bet);
            double flopSpr = _playerGameState.PotSize > 0
                ? (double)(_playerGameState.HeroStack / _playerGameState.PotSize) : 0;
            _gameLoggerService.LogStreetDecision(new StreetDecision(
                BoardPosition.Flop, result.EquityPercentage, result.PotOddsPercentage, result.ExpectedValue,
                result.RecommendedAction, _responseAction.Action ?? "Unknown", _playerGameState.PotSize,
                flopMaxBet, _playerGameState.HandSituation, _playerGameState.IsInPosition,
                Reason: result.RecommendedAction, TotalOuts: result.TotalOuts, SPR: flopSpr));
            _gameLoggerService.UpdateSituation(_playerGameState.HandSituation);
        }

        /// <summary>
        /// Determina la acción del flop delegando a GameCoordinator.
        /// </summary>
        private void DetermineFlopActionUnified()
        {
            _coordinator.SetFlopResult(_flopResult);
            var result = _coordinator.DetermineFlopAction(_playerGameState);
            _responseAction.Action = result.Action;
            _logger.LogError("{LogText}", result.LogText);
        }

        /// <summary>
        /// Determina si hero fue el agresor preflop basado en la HandSituation.
        /// </summary>
        // IsPreflopAggressor, HasRangeAdvantageOnBoard, CalculateCbetAdjustment
        // extraídos a PreflopAnalyzer en DecisionMaker

        /// <summary>
        /// Determina la acción del turn delegando a GameCoordinator.
        /// </summary>
        private void DetermineTurnAction()
        {
            _coordinator.SetTurnResult(_turnResult);
            _coordinator.TurnBoardTexture = _turnBoardTexture;
            var result = _coordinator.DetermineTurnAction(_playerGameState);
            _responseAction.Action = result.Action;
            _logger.LogError("{LogText}", result.LogText);
        }

        /// <summary>
        /// <summary>
        /// Procesa la fase de turn
        /// </summary>
        private async Task ProcessTurnAsync()
        {
            _logger.LogInformation("ProcessTurnAsync: Iniciando procesamiento de turn - Estado actual: {State}", _gameLoopStateMachine.CurrentState);

            // Capturar carta del turn con retry
            List<BoardData> dataBoard = null!;
            for (int attempt = 0; attempt <= GameLoopStateMachine.MaxOcrRetries; attempt++)
            {
                using var bitmap = new Bitmap(_formImage.pbImage.Image);
                var turnResponse = await _getCardsTurnUseCase.ExecuteAsync(new GetCardsTurnUseCaseRequest
                {
                    Image = bitmap,
                    RegionsTableMap = _regionsTableMap,
                    DataBoard = _playerGameState.BoardCards,
                    CurrentImageWidth = bitmap.Width,
                    CurrentImageHeight = bitmap.Height
                });

                dataBoard = turnResponse.DataBoard;
                _logger.LogInformation("ProcessTurnAsync: Intento {Attempt} - Cartas totales: {Total}", attempt + 1, dataBoard.Count);

                if (dataBoard.Count >= 4)
                    break;

                if (attempt < GameLoopStateMachine.MaxOcrRetries)
                {
                    _logger.LogError("OCR turn: intento {Attempt} falló ({Count} cartas), reintentando...", attempt + 1, dataBoard.Count);
                    await Task.Delay(200);
                }
            }

            if (dataBoard.Count < 4)
            {
                _logger.LogError("No se detectó la carta del turn tras reintentos.");
                _responseAction.Action = "Error: No se pudo detectar carta del turn";
                return;
            }

            _playerGameState.BoardCards = dataBoard;

            // Analizar textura del board del turn
            _turnBoardTexture = AnalyzeTurnBoardTexture(dataBoard);

            var myCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                new CardDataOuts((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
            };

            // Filtrar solo cartas comunitarias (excluir hole cards que se añaden en el flop)
            var boardOnly = dataBoard
                .Where(d => d.Position != BoardPosition.Hand)
                .OrderBy(d => d.Location)
                .ToList();
            var communityCards = boardOnly.Select(d =>
                new CardDataOuts((Suit)d.Suit, (Rank)d.Force)).ToList();

            var result = _pokerCalculator.Calculate(
                myCards,
                communityCards,
                _playerGameState.PotSize,
                _playerGameState.Players.Max(m => m.Bet),
                isInPosition: _playerGameState.IsInPosition,
                heroStack: _playerGameState.HeroStack,
                villainStack: GetVillainStack(),
                handSituation: _playerGameState.HandSituation.ToString(),
                opponentProfile: _coordinator.GetActiveVillainProfile(_playerGameState));

            _turnResult = result;

            UpdateOverlayWithPotOdds(result);

            // Determinar si estamos en posición
            _tableLayout.SetIsInPosition(_playerGameState);

            // Determinar acción en el turn
            DetermineTurnAction();
        }

        /// <summary>
        /// Procesa la fase de river
        /// </summary>
        private async Task ProcessRiverAsync()
        {
            _logger.LogInformation("ProcessRiverAsync: Iniciando procesamiento de river - Estado actual: {State}", _gameLoopStateMachine.CurrentState);

            // Capturar carta del river con retry
            List<BoardData> dataBoard = null!;
            for (int attempt = 0; attempt <= GameLoopStateMachine.MaxOcrRetries; attempt++)
            {
                using var bitmap = new Bitmap(_formImage.pbImage.Image);
                var riverResponse = await _getCardsRiverUseCase.ExecuteAsync(new GetCardsRiverUseCaseRequest
                {
                    Image = bitmap,
                    RegionsTableMap = _regionsTableMap,
                    DataBoard = _playerGameState.BoardCards,
                    CurrentImageWidth = bitmap.Width,
                    CurrentImageHeight = bitmap.Height
                });

                dataBoard = riverResponse.DataBoard;
                // Contar solo cartas con nombre válido (no vacío)
                int validCards = dataBoard.Count(d => !string.IsNullOrEmpty(d.Name));
                _logger.LogInformation("ProcessRiverAsync: Intento {Attempt} - Cartas totales: {Total}, válidas: {Valid}", attempt + 1, dataBoard.Count, validCards);

                if (validCards >= 5)
                    break;

                if (attempt < GameLoopStateMachine.MaxOcrRetries)
                {
                    _logger.LogError("OCR river: intento {Attempt} falló ({Count} cartas), reintentando...", attempt + 1, dataBoard.Count);
                    await Task.Delay(200);
                }
            }

            int finalValidCards = dataBoard.Count(d => !string.IsNullOrEmpty(d.Name));
            if (finalValidCards < 5)
            {
                _logger.LogError("No se detectó la carta del river tras reintentos (válidas={Valid}/{Total}).", finalValidCards, dataBoard.Count);
                _responseAction.Action = "Error: No se pudo detectar carta del river";
                return;
            }

            _playerGameState.BoardCards = dataBoard;

            var myCards = new List<CardDataOuts>
            {
                new CardDataOuts((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                new CardDataOuts((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
            };

            // Filtrar solo cartas comunitarias (excluir hole cards que se añaden en el flop)
            var boardOnly = dataBoard
                .Where(d => d.Position != BoardPosition.Hand)
                .OrderBy(d => d.Location)
                .ToList();
            var communityCards = boardOnly.Select(d =>
                new CardDataOuts((Suit)d.Suit, (Rank)d.Force)).ToList();

            var result = _pokerCalculator.Calculate(
                myCards,
                communityCards,
                _playerGameState.PotSize,
                _playerGameState.Players.Max(m => m.Bet),
                isInPosition: _playerGameState.IsInPosition,
                heroStack: _playerGameState.HeroStack,
                villainStack: GetVillainStack(),
                handSituation: _playerGameState.HandSituation.ToString(),
                opponentProfile: _coordinator.GetActiveVillainProfile(_playerGameState));

            _riverResult = result;

            UpdateOverlayWithPotOdds(result);

            // Analizar textura del board del river
            _riverBoardTexture = AnalyzeRiverBoardTexture(dataBoard);

            // Determinar si estamos en posición
            _tableLayout.SetIsInPosition(_playerGameState);

            // Determinar acción en el river
            DetermineRiverAction();
        }

        /// <summary>
        /// Maneja la inicialización de una nueva mano
        /// </summary>
        private async Task HandleNewHandAsync(
            decimal prevPot = 0,
            string prevHoleCards = "",
            string prevHoleCard1 = "",
            string prevHoleCard2 = "",
            TablePosition prevPosition = TablePosition.None,
            decimal prevHeroStack = 0)
        {
            // Finalizar la mano anterior y guardar sesión.
            // Usar prevHeroStack porque _playerGameState ya fue reseteado antes de esta llamada.
            if (_gameLoggerService.HasActiveHand)
            {
                _gameLoggerService.EndHand(prevHeroStack);
                await _gameLoggerService.SaveSessionAsync();
            }

            // Registrar mano jugada para todos los villanos activos (opponent tracking)
            foreach (var player in _playerGameState.Players.Where(p => p.Active && !string.IsNullOrEmpty(p.Name)))
            {
                // S22.3: pasar posición para stats posicionales
                _opponentTracker.RecordHandPlayed(player.Name!, player.Position);
                // Si villain puso dinero preflop → VPIP
                if (player.Bet > 0)
                    _opponentTracker.RecordVPIP(player.Name!, player.Position);
                // Si villain hizo raise (bet significativa) → PFR
                if (player.Bet > 0 && _playerGameState.PotSize > 0 &&
                    player.Bet > _playerGameState.PotSize * 0.3m)
                    _opponentTracker.RecordPFR(player.Name!, player.Position);
            }

            // _contextHolder.StartNewHand() se hace condicionalmente en btnCapture_Click
            // para no perder el contexto cuando se restaura un estado postflop guardado
            _logger.LogInformation("Nueva mano detectada: Hand {Hand}, Pot: {Pot}, HoleCards: {HoleCards}", _tableHand, prevPot, prevHoleCards);

            // Asegurar que hay sesión activa e iniciar nueva mano
            if (!_gameLoggerService.HasActiveSession)
                await _gameLoggerService.StartSessionAsync(_session, _tableName);

            if (long.TryParse(_tableHand, out var handNum))
            {
                var activePlayers = _playerGameState?.Players?.Count(p => !p.Empty) ?? 0;

                // Calcular ciega obligatoria según posición del hero
                decimal blindPosted = prevPosition switch
                {
                    TablePosition.BigBlind => _gameLoggerService.CurrentBigBlind,
                    TablePosition.SmallBlind => _gameLoggerService.CurrentBigBlind / 2,
                    _ => 0
                };

                await _gameLoggerService.StartNewHandAsync(
                    handNum,
                    prevHoleCard1,
                    prevHoleCard2,
                    prevPosition,
                    prevHeroStack,
                    activePlayers,
                    blindPosted);
            }

            _folderPath = Path.Combine(
                DEFAULT_SAMPLES_PATH,
                "games",
                $"{DateTime.Now:yyyyMMdd}_Game",
                _session);

            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            var path = Path.Combine(_folderPath, "resume.txt");
            await File.AppendAllTextAsync(path, tbResume.Text + Environment.NewLine);
            // tbResume.Text = string.Empty;
        }

        /// <summary>
        /// Actualiza el overlay con la información de pot odds
        /// </summary>
        private void UpdateOverlayWithPotOdds(PokerCalculationResult potOddsResult)
        {
            _frmOverlay.UpdateWithCalculationResult(potOddsResult);
        }

        /// <summary>
        /// Actualiza la interfaz con los resultados del análisis
        /// </summary>
        private void UpdateUIWithResults(PokerCalculationResult potOddsResult)
        {
            _playerGameState.HandSituation = _responseAction.HandSituation;

            if (_playerGameState != null)
            {
                if (IsPreflop)
                {
                    UpdateResumeTextForPreflop(potOddsResult);
                    // Transición a PreflopAction ya se hizo en ProcessTableInfoAsync
                }

                if (_gameLoopStateMachine.IsFlop)
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
            {
                var actionText = _responseAction?.Action ?? string.Empty;
                var villains = _playerGameState.Players.Where(p => p.Name != "P0").ToList();
                _frmOverlay.UpdateAction(_actionFormatter.EnrichActionWithBBAmount(actionText, villains, _gameLoggerService.CurrentBigBlind));
            }

        }

        /// <summary>
        /// Actualiza el texto de resumen para la fase de preflop
        /// </summary>
        private void UpdateResumeTextForPreflop(PokerCalculationResult potOddsResult)
        {
            var enMesa = _playerGameState.Players.Count(e => !e.Empty) + 1;
            var sitout = _playerGameState.Players.Count(s => s.SitOut);
            var playing = _playerGameState.Players.Count(p => p.Active) + 1;

            // Calcular equity preflop si hay hole cards
            double preflopEquity = 0;
            if (_playerGameState.HoleCard1Rank > 0 && _playerGameState.HoleCard2Rank > 0)
            {
                try
                {
                    var heroCards = new List<CardDataOuts>
                    {
                        new((Suit)_playerGameState.HoleCard1Suit, (Rank)_playerGameState.HoleCard1Rank),
                        new((Suit)_playerGameState.HoleCard2Suit, (Rank)_playerGameState.HoleCard2Rank)
                    };
                    // En preflop, numOpp = jugadores con bet voluntaria (> big blind),
                    // excluyendo blinds obligatorias que no representan manos en la mano.
                    int voluntaryBettors = _playerGameState.Players.Count(p => p.Active && p.Bet > 1m);
                    var numOpp = Math.Max(1, voluntaryBettors);
                    var preflopResult = _pokerCalculator.Calculate(
                        heroCards, new List<CardDataOuts>(),
                        _playerGameState.PotSize,
                        _playerGameState.Players.Max(m => m.Bet),
                        numOpponents: numOpp,
                        heroStack: _playerGameState.HeroStack,
                        villainStack: GetVillainStack(),
                        handSituation: _playerGameState.HandSituation.ToString(),
                        opponentProfile: _coordinator.GetActiveVillainProfile(_playerGameState));
                    preflopEquity = preflopResult.EquityPercentage;

                    // Ajuste posicional: posiciones tardías tienen ventaja de información
                    double positionAdjust = _playerGameState.Position switch
                    {
                        TablePosition.Button => 3.0,
                        TablePosition.CutOff => 1.0,
                        TablePosition.Middle => 0.0,
                        TablePosition.Early => -2.0,
                        TablePosition.SmallBlind => -3.0,
                        TablePosition.BigBlind => -1.0,
                        _ => 0.0
                    };
                    preflopEquity = Math.Max(0, Math.Min(100, preflopEquity + positionAdjust));
                }
                catch { /* OCR puede dar valores inválidos */ }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Hand #{_tableHand}: Hold'em No Limit");
            sb.AppendLine($"#{_playerGameState.Players.FirstOrDefault(d => d.Dealer)?.Name ?? "Hero"} is the Dealer");
            sb.AppendLine($"{_playerGameState.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "Hero"}: posts small blind");
            sb.AppendLine($"{_playerGameState.Players.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "Hero"}: posts big blind");
            sb.AppendLine($"Pot: {_playerGameState.PotSize}");
            sb.AppendLine($"*** STATISTICS ***");
            sb.AppendLine($"PotOdds: {(decimal)potOddsResult.PotOddsPercentage}%");
            sb.AppendLine($"Equity: {preflopEquity:F1}%");
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

            // Siempre mostrar la acción recomendada de Hero al final
            var heroAction = _responseAction?.Action ?? "No action";
            var heroSituation = _playerGameState.HandSituation;
            sb.AppendLine($"*** HERO ACTION ***");
            sb.AppendLine($"Position: {_playerGameState.Position}  |  Situation: {heroSituation}");
            sb.AppendLine($"Equity preflop: {preflopEquity:F1}%");

            // Enriquecer acción con BB amount si tiene multiplicador (ej: "3Bet x6" → "3Bet x6 (15BB)")
            var villainsSnapshot = _playerGameState.Players.Where(p => p.Name != "P0").ToList();
            var displayAction = _actionFormatter.EnrichActionWithBBAmount(heroAction, villainsSnapshot, _gameLoggerService.CurrentBigBlind);
            sb.AppendLine($"▶ DECISIÓN: {displayAction}");

            tbResume.AppendText(sb.ToString() + Environment.NewLine);
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
                heroStack: _playerGameState.HeroStack,
                villainStack: GetVillainStack(),
                handSituation: _playerGameState.HandSituation.ToString());

            return result;
        }

        /// <summary>
        /// Establece las apuestas de los jugadores
        /// </summary>
        private void SetBetPlayer()
        {
            using var binaryImage = PixConverter.ToPix(CaptureWindowsHelper.BinaryImage(new Bitmap(_formImage.pbImage.Image), _pictureUmbralBet));
            var betsRegions = _regionLookupCache.GetRegions("Bets");

            _logger.LogInformation("[SetBetPlayer] Total regions: {Count}", betsRegions?.Count ?? 0);

            if (betsRegions == null || _formImage.pbImage.Image == null)
                return;

            foreach (var region in betsRegions)
            {
                var playerNumber = PlayerRegionParser.GetPlayerNumber(region.Name, "bet");
                _logger.LogInformation("[SetBetPlayer] Region: {Region}, parsed playerNumber: {PlayerNumber}", region.Name, playerNumber);

                if (playerNumber == null) continue;

                var scaled = GetScaledRegion(region);
                var betValue = _screenReader.ReadBetValue(_formImage.pbImage.Image,
                    scaled.X, scaled.Y, scaled.Width, scaled.Height,
                    region.Umbral, region.InactiveUmbral, region.IsOnlyNumber, playerNumber);

                // Log para debug de bets
                _logger.LogInformation("[SetBetValue] Region: {Region}, Player: P{PlayerNumber}, Value: {BetValue}", region.Name, playerNumber, betValue);

                // Normalizar: detecta decimal separator perdido (593 → 5,93), artefacto "8"
                betValue = _screenReader.NormalizeBetValue(betValue, _playerGameState.PotSize);

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
        /// Establece el stack del héroe
        /// </summary>
        private void SetHeroStack()
        {
            var region = _regionLookupCache.GetRegion("User", "uStack");
            if (region == null || _formImage.pbImage.Image == null)
                return;

            // Limpiar cache OCR para evitar colisiones dHash entre valores similares (ej: 97 vs 92)
            _ocrService.ClearCache();

            // Intentar leer el stack con retry
            decimal stackValue = 0;
            int maxRetries = 2;

            var scaled = GetScaledRegion(region);

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var rawValue = _screenReader.ReadStackValue(_formImage.pbImage.Image,
                    scaled.X, scaled.Y, scaled.Width, scaled.Height,
                    region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);

                stackValue = _screenReader.NormalizeStackValue(rawValue);

                if (stackValue > 0)
                    break;

                if (attempt < maxRetries)
                    Thread.Sleep(100);
            }

            // Si no se obtuvo valor válido, mantener el anterior
            if (stackValue <= 0 && _playerGameState.HeroStack > 0)
            {
                _logger.LogError("[STACK] OCR falló tras reintentos, manteniendo valor anterior: {HeroStack}", _playerGameState.HeroStack);
                lbUserStack.Text = _playerGameState.HeroStack.ToString();
                return;
            }

            // Detección de auto-rebuy: si el stack sube más de lo que el pot podría explicar,
            // es un auto-rebuy a 100BB. No actualizar _heroStackPreRebuy en ese caso.
            bool isHandActive = !_gameLoopStateMachine.IsWaiting && !_gameLoopStateMachine.IsHandComplete;
            decimal previousStack = _playerGameState.HeroStack;
            decimal maxPossibleWin = previousStack + _playerGameState.PotSize;

            if (isHandActive && previousStack > 0 && stackValue > maxPossibleWin + 1)
            {
                // Auto-rebuy detectado: stack subió más de lo posible por ganar el pot
                _logger.LogInformation("[STACK] Auto-rebuy detectado: {Previous} → {Current} (pot={Pot})", previousStack, stackValue, _playerGameState.PotSize);
                _gameLoggerService.RegisterAutoRebuy(100);
            }
            else if (isHandActive && stackValue > 0)
            {
                // Stack normal durante mano activa: actualizar pre-rebuy tracker
                _heroStackPreRebuy = stackValue;
            }

            _playerGameState.HeroStack = stackValue;
            lbUserStack.Text = stackValue.ToString();
        }

        /// <summary>
        /// Establece el valor del bote
        /// </summary>
        private void SetPotValue()
        {
            var regionPot = _regionLookupCache.GetRegion("Table", "pot");
            if (_formImage.pbImage.Image == null)
                return;
            if (regionPot != null)
            {
                decimal potValue = 0;
                var scaled = GetScaledRegion(regionPot);
                var pot = _screenReader.ReadText(_formImage.pbImage.Image,
                    scaled.X, scaled.Y, scaled.Width, scaled.Height,
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
        private async Task SetTableHand()
        {
            if (_playerGameState == null)
                _playerGameState = new PlayerGameState();

            SetPotValue();
            await ObtainCardsPlayerAsync();
            // SetDealerPlayer se llama desde InitializePlayersAsync después de crear los jugadores
            // SetDealerPlayer();

            var regionTableHand = _regionLookupCache.GetRegion("Table", "tablehand");
            if (regionTableHand != null)
            {
                var scaled = GetScaledRegion(regionTableHand);
                if (string.IsNullOrEmpty(_tableHand))
                {
                    _tableHand = _screenReader.ReadHandNumber(_formImage.pbImage.Image,
                        scaled.X, scaled.Y, scaled.Width, scaled.Height,
                        regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber);
                    _newHand = true;
                    _metrics?.StartHand(_tableHand);
                    _cycleCounter++;
                }
                else
                {
                    var currentHand = _screenReader.ReadHandNumber(_formImage.pbImage.Image,
                        scaled.X, scaled.Y, scaled.Width, scaled.Height,
                        regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber);

                    bool handNumberChanged = false;
                    long oldTableHand = 0;
                    long newTableHand = 0;
                    bool handNumberParseable = long.TryParse(_tableHand, out oldTableHand) &&
                                               long.TryParse(currentHand, out newTableHand);

                    if (handNumberParseable)
                    {
                        if (oldTableHand != newTableHand)
                        {
                            handNumberChanged = true;
                        }
                        else if (newTableHand == 0)
                        {
                            handNumberChanged = true;
                        }
                    }
                    else
                    {
                        // OCR no pudo parsear el hand number — comparar como texto
                        _logger.LogDebug("[HAND#] Parsing fallido: prev='{Prev}', current='{Current}' — comparando como texto", _tableHand, currentHand);
                        if (!string.IsNullOrEmpty(currentHand) && _tableHand != currentHand)
                            handNumberChanged = true;
                    }

                    if (handNumberChanged)
                    {
                        // Protección contra falsos positivos de OCR en postflop:
                        // Si estamos en postflop y el número de mano cambió drásticamente
                        // (longitud diferente o cambio >100x), probablemente es un error de OCR
                        // al re-leer el número cuando aparecen cartas nuevas en el board.
                        // En ese caso requerimos más indicadores secundarios (3+).
                        bool inPostflop = _gameLoopStateMachine.IsFlop ||
                                          _gameLoopStateMachine.IsTurn ||
                                          _gameLoopStateMachine.IsRiver;

                        bool suspiciousOcrChange = false;
                        if (inPostflop && handNumberParseable && oldTableHand > 0 && newTableHand > 0)
                        {
                            double ratio = (double)newTableHand / oldTableHand;
                            bool lengthDiffers = newTableHand.ToString().Length != oldTableHand.ToString().Length;
                            suspiciousOcrChange = lengthDiffers || ratio > 100 || ratio < 0.01;
                        }
                        else if (inPostflop && !handNumberParseable)
                        {
                            // En postflop con OCR no parseable, también es sospechoso
                            suspiciousOcrChange = true;
                        }

                        if (suspiciousOcrChange)
                        {
                            _logger.LogInformation("[HAND#] Cambio sospechoso de OCR en postflop: prev='{Prev}', current='{Current}' — requiriendo más indicadores", _tableHand, currentHand);
                            _newHand = DetectNewHand(false, currentHand);
                        }
                        else
                        {
                            _newHand = DetectNewHand(true, currentHand);
                        }

                        if (_newHand)
                        {
                            if (handNumberParseable && newTableHand > 0)
                                _tableHand = newTableHand.ToString();
                            else if (!string.IsNullOrEmpty(currentHand))
                                _tableHand = currentHand;
                            else
                            {
                                _newTableHand++;
                                _tableHand = _newTableHand.ToString();
                            }
                        }
                    }
                    else if (!handNumberParseable && string.IsNullOrEmpty(currentHand))
                    {
                        // OCR falló completamente — evaluar indicadores secundarios
                        _logger.LogDebug("[HAND#] OCR falló completamente, evaluando indicadores secundarios");
                        _newHand = DetectNewHand(false, currentHand);
                        if (_newHand)
                        {
                            _newTableHand++;
                            _tableHand = _newTableHand.ToString();
                        }
                    }
                }
            }

            var regionTableName = _regionLookupCache.GetRegion("Table", "tablename");
            if (regionTableName != null && string.IsNullOrEmpty(_tableName))
            {
                var scaledName = GetScaledRegion(regionTableName);
                _tableName = _screenReader.ReadText(_formImage.pbImage.Image,
                    scaledName.X, scaledName.Y, scaledName.Width, scaledName.Height,
                    regionTableName.Umbral, regionTableName.InactiveUmbral, regionTableName.IsOnlyNumber);
                // Remove numbers from table name
                _tableName = Regex.Replace(_tableName, @"\d", "");
            }
        }


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
                    _logger.LogError("El directorio {Path} no existe", _folderPath);
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
                _logger.LogError(ex, "Error al crear log de manos marcadas");
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
            var userRegionsList = _regionLookupCache.GetRegions("User");
            if (userRegionsList == null || _formImage.pbImage.Image == null)
                return;

            var hashRegions = userRegionsList.Where(w => w.IsHash == true).ToList();

            // Carga de cartas desde cache singleton
            _cardsImages ??= await _cardCacheService.GetCardsAsync();

            foreach (var region in hashRegions)
            {
                var scaled = GetScaledRegion(region);

                var imageToBase64 = _imageCropperService.CropImageToBase64(
                    _formImage.pbImage.Image,
                    scaled.X,
                    scaled.Y,
                    scaled.Width,
                    scaled.Height);

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
        /// Obtiene una imagen mientras se está jugando
        /// </summary>
        private async Task GetImageWhilePlaying()
        {
            try
            {
                if (_handle == IntPtr.Zero)
                {
                    _logger.LogError("GetImageWhilePlaying: _handle es IntPtr.Zero, no se puede capturar");
                    return;
                }

                string baseFolder = Path.Combine(
                    "C:", "Code", "Poker", "ScrapePoker", "output", "samples", "games",
                    $"{DateTime.Now:yyyyMMdd}_Game");

                _folderPath = Path.Combine(baseFolder, _session);

                // Crear directorio si no existe
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }

                var path = Path.Combine(_folderPath, $"game_{DateTime.Now.Ticks}.png");

                using var capturedBitmap = _useCase.ExecuteImage(path);

                if (capturedBitmap.Width <= 1 || capturedBitmap.Height <= 1)
                {
                    _logger.LogError("GetImageWhilePlaying: Captura inválida ({Width}x{Height})", capturedBitmap.Width, capturedBitmap.Height);
                    return;
                }

                // Inicializar CoordinateScaler en primera captura y guardar en configuración
                if (!_coordinateScaler.IsInitialized)
                {
                    _coordinateScaler.Initialize(capturedBitmap.Width, capturedBitmap.Height);
                    SaveReferenceDimensionsToConfig(capturedBitmap.Width, capturedBitmap.Height);
                }

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
                _logger.LogError(ex, "Error al obtener imagen");
            }
        }

        /// <summary>
        /// Guarda las dimensiones de referencia en appsettings.json
        /// </summary>
        private void SaveReferenceDimensionsToConfig(int width, int height)
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (!File.Exists(configPath))
                {
                    _logger.LogError("SaveReferenceDimensionsToConfig: appsettings.json no encontrado");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                using var ms = new MemoryStream();
                using var writer = new StreamWriter(ms);
                using var reader = new StreamReader(ms);

                writer.Write("{");
                bool isFirst = true;

                foreach (var property in root.EnumerateObject())
                {
                    if (!isFirst) writer.Write(",");
                    isFirst = false;

                    if (property.Name == "CaptureSettings")
                    {
                        writer.Write($"\"CaptureSettings\":{{");
                        bool innerFirst = true;
                        foreach (var captureProp in property.Value.EnumerateObject())
                        {
                            if (!innerFirst) writer.Write(",");
                            innerFirst = false;

                            if (captureProp.Name == "ReferenceImageWidth")
                                writer.Write($"\"ReferenceImageWidth\":{width}");
                            else if (captureProp.Name == "ReferenceImageHeight")
                                writer.Write($"\"ReferenceImageHeight\":{height}");
                            else if (captureProp.Name == "IsReferenceSet")
                                writer.Write($"\"IsReferenceSet\":true");
                            else
                                writer.Write($"\"{captureProp.Name}\":{GetJsonValue(captureProp.Value)}");
                        }
                        writer.Write("}");
                    }
                    else
                    {
                        writer.Write($"\"{property.Name}\":{GetJsonValue(property.Value)}");
                    }
                }

                writer.Write("}");
                writer.Flush();

                ms.Position = 0;
                File.WriteAllText(configPath, reader.ReadToEnd());

                _logger.LogInformation("Guardadas dimensiones de referencia: {Width}x{Height}", width, height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar dimensiones de referencia");
            }
        }

        private static string GetJsonValue(System.Text.Json.JsonElement element)
        {
            return element.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => $"\"{element.GetString()}\"",
                System.Text.Json.JsonValueKind.Number => element.GetRawText(),
                System.Text.Json.JsonValueKind.True => "true",
                System.Text.Json.JsonValueKind.False => "false",
                System.Text.Json.JsonValueKind.Null => "null",
                _ => element.GetRawText()
            };
        }

        /// <summary>
        /// Escala las coordenadas de una región según el ratio entre la imagen actual y la imagen de referencia
        /// </summary>
        private (int X, int Y, int Width, int Height) GetScaledRegion(OpenScrape.Domain.ValueObjects.Region region)
        {
            if (_formImage.pbImage.Image == null)
                return (region.PosX, region.PosY, region.Width, region.Height);

            int currentWidth = _formImage.pbImage.Image.Width;
            int currentHeight = _formImage.pbImage.Image.Height;

            return _coordinateScaler.ScaleRegion(
                region.PosX, region.PosY, region.Width, region.Height,
                currentWidth, currentHeight);
        }

        // Método CalculateOverlayPosition movido a IOverlayPositioner
        // (extract-frmmain-testable-logic Fase 3).

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
                    // Esperar 2s para que el usuario active la ventana de poker
                    Task.Delay(2000).Wait();
                    IntPtr foregroundHandle = CaptureWindowsHelper.User32.GetForegroundWindow();
                    _handle = _useCase.GetWindow(foregroundHandle);

                    User32.GetWindowRect(_handle, ref windowRect);
                    _locWindowRect = windowRect;

                    // Inicialización de overlay: crear y mostrar primero para que AutoSize
                    // calcule el tamaño real, luego posicionar con offset proporcional
                    _frmOverlay = new FrmOverlay(_overlayConfig);
                    _frmOverlay.Show();

                    _frmOverlay.Location = _overlayPositioner.Calculate(windowRect.left, windowRect.right, windowRect.bottom, _frmOverlay.Size.Width);
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
                                _frmOverlay.Location = _overlayPositioner.Calculate(windowRect.left, windowRect.right, windowRect.bottom, _frmOverlay.Size.Width);
                            }
                        });
                    }
                }

                if (!_backgroundExecute)
                    backgroundWorker1.RunWorkerAsync();

                // refactor-frmmain-coordinators Fase 6: arranque opcional del coordinator.
                // Feature flag en false por defecto → este bloque queda inerte en producción
                // hasta que un cutover validado manualmente lo active.
                if (_featureFlags.UseGameLoopCoordinator && !_gameLoopCoordinator.IsRunning)
                {
                    _gameLoopCts = new CancellationTokenSource();
                    _uiSyncService.Attach(_gameLoopCoordinator, this, _frmOverlay!, tbResume);
                    _ = _gameLoopCoordinator.StartAsync(_gameLoopCts.Token);
                    _logger.LogInformation("[Fase 6] GameLoopCoordinator arrancado (feature flag ON)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en btnWindow_Click");
            }
        }

        /// <summary>
        /// Maneja el evento DoWork del BackgroundWorker con detección mejorada y logging detallado
        /// </summary>
        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                _backgroundExecute = true;
                var detectionStats = new { StartTime = DateTime.Now, TotalChecks = 0, Detections = 0, Errors = 0 };
                var lastLoggedColor = Color.Empty;
                var colorChangeCount = 0;

                User32.RECT windowRect = new User32.RECT();
                User32.GetWindowRect(_handle, ref windowRect);

                _detectionLoggerService.LogDetectionError("BackgroundWorker iniciado - Monitoreo de detección activo");

                while (true)
                {
                    try
                    {
                        detectionStats = detectionStats with { TotalChecks = detectionStats.TotalChecks + 1 };

                        // Validación de overlay
                        if (_frmOverlay == null || !_frmOverlay.Visible)
                        {
                            _detectionLoggerService.LogDetectionError("Overlay no visible - Cancelando BackgroundWorker");
                            e.Cancel = true;
                            return;
                        }

                        if (_handle == IntPtr.Zero)
                        {
                            _detectionLoggerService.LogDetectionError("Handle es IntPtr.Zero - no se puede capturar");
                            Task.Delay(500).Wait();
                            continue;
                        }

                        using var img = _useCase.Execute(_handle);

                        if (img == null || img.Width <= 1 || img.Height <= 1)
                        {
                            _detectionLoggerService.LogDetectionError($"Captura inválida: {img?.Width}x{img?.Height}");
                            Task.Delay(500).Wait();
                            continue;
                        }

                        // Validación de regiones
                        var regionAction = _regionLookupCache.GetRegion("User", "uAction");
                        var flop = _regionLookupCache.GetRegion("Table", "isFlop");

                        if (regionAction == null || flop == null)
                        {
                            if (detectionStats.TotalChecks % 100 == 0) // Log cada 100 intentos
                            {
                                _detectionLoggerService.LogDetectionError($"Regiones no encontradas - uAction: {regionAction != null}, isFlop: {flop != null}");
                            }
                            Task.Delay(100).Wait();
                            continue;
                        }

                        using var bitmap = new Bitmap(img);

                        // Detección mejorada con múltiples píxeles y tolerancia
                        var detectionResult = PerformEnhancedDetection(bitmap, regionAction, flop);

                        // Log cambios de color significativos
                        if (!detectionResult.ActionColor.Equals(lastLoggedColor))
                        {
                            colorChangeCount++;
                            if (colorChangeCount % 10 == 0 || Math.Abs(detectionResult.ActionColor.B - lastLoggedColor.B) > 5)
                            {
                                _detectionLoggerService.LogColorDetection(
                                    new Point(regionAction.PosX, regionAction.PosY),
                                    detectionResult.ActionColor,
                                    detectionResult.ShouldCapture,
                                    "uAction"
                                );
                                lastLoggedColor = detectionResult.ActionColor;
                            }
                        }

                        this.Invoke((MethodInvoker)delegate
                        {
                            try
                            {
                                // Detección de flop mejorada
                                if (detectionResult.ShouldCaptureFlop)
                                {
                                    _gameLoopStateMachine.TryTransition(GameState.FlopDetected, 3);
                                    _detectionLoggerService.LogTurnDetected(
                                        new Point(regionAction.PosX, regionAction.PosY),
                                        detectionResult.ActionColor,
                                        "FlopDetected"
                                    );
                                }

                                // Detección de turno del Hero mejorada
                                if (detectionResult.ShouldCapture)
                                {
                                    detectionStats = detectionStats with { Detections = detectionStats.Detections + 1 };

                                    _detectionLoggerService.LogTurnDetected(
                                        new Point(regionAction.PosX, regionAction.PosY),
                                        detectionResult.ActionColor,
                                        _gameLoopStateMachine.CurrentState.ToString()
                                    );

                                    // Guardar screenshot de debug si está habilitado
                                    if (detectionStats.Detections % 5 == 0) // Cada 5 detecciones
                                    {
                                        _detectionLoggerService.SaveDebugScreenshot(
                                            img,
                                            new Point(regionAction.PosX, regionAction.PosY),
                                            detectionResult.ActionColor,
                                            "turn_detected"
                                        );
                                    }

                                    btnCapture_Click(sender, e);
                                }

                                // Reset del flag de ejecución con lógica mejorada
                                if (!detectionResult.IsActionColorInRange)
                                    _executeCapture = false;
                            }
                            catch (Exception invokeEx)
                            {
                                _detectionLoggerService.LogDetectionError($"Error en Invoke delegate: {invokeEx.Message}", invokeEx);
                            }
                        });

                        // Log estadísticas periódicas
                        if (detectionStats.TotalChecks % 1000 == 0)
                        {
                            var sessionDuration = DateTime.Now - detectionStats.StartTime;
                            _detectionLoggerService.LogDetectionStatistics(
                                detectionStats.TotalChecks,
                                detectionStats.Detections,
                                detectionStats.Errors,
                                sessionDuration
                            );
                        }

                        btnWindow_Click(sender, e);

                        // Delay adaptativo basado en la actividad
                        var delay = detectionResult.ShouldCapture ? 200 : 100; // Más lento después de detección
                        Task.Delay(delay).Wait();
                    }
                    catch (Exception loopEx)
                    {
                        detectionStats = detectionStats with { Errors = detectionStats.Errors + 1 };
                        _detectionLoggerService.LogDetectionError($"Error en bucle de detección: {loopEx.Message}", loopEx);

                        // Delay más largo en caso de error para evitar spam
                        Task.Delay(500).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                _detectionLoggerService.LogDetectionError($"Error crítico en BackgroundWorker1_DoWork: {ex.Message}", ex);
                _logger.LogError(ex, "Error en BackgroundWorker1_DoWork");
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Realiza detección mejorada con múltiples píxeles y tolerancia de color
        /// </summary>
        private DetectionResult PerformEnhancedDetection(Bitmap bitmap, Domain.ValueObjects.Region regionAction, Domain.ValueObjects.Region flop)
        {
            try
            {
                // Escalar coordenadas de las regiones
                var scaledAction = GetScaledRegion(regionAction);
                var scaledFlop = GetScaledRegion(flop);

                int w = bitmap.Width;
                int h = bitmap.Height;

                // Acceso directo al buffer de píxeles (evita marshaling de GetPixel)
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, w, h),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    bitmap.PixelFormat);

                try
                {
                    int bpp = System.Drawing.Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                    int stride = bmpData.Stride;
                    nint scan0 = bmpData.Scan0;

                    // Leer color principal de acción y flop
                    var (actionR, actionG, actionB) = ReadPixelFromBuffer(scan0, stride, scaledAction.X, scaledAction.Y, bpp);
                    var (flopR, flopG, flopB) = ReadPixelFromBuffer(scan0, stride, scaledFlop.X, scaledFlop.Y, bpp);

                    Color primaryActionColor = Color.FromArgb(actionR, actionG, actionB);
                    Color flopColor = Color.FromArgb(flopR, flopG, flopB);

                    // Muestrear píxeles en patrón de cruz (±2 px) — suma manual sin LINQ
                    int sumR = actionR, sumG = actionG, sumB = actionB;
                    int count = 1;

                    ReadOnlySpan<(int dx, int dy)> offsets =
                    [
                        (-2, 0), (2, 0), (0, -2), (0, 2),
                        (-1, -1), (1, 1), (-1, 1), (1, -1)
                    ];

                    foreach (var (dx, dy) in offsets)
                    {
                        int x = scaledAction.X + dx;
                        int y = scaledAction.Y + dy;

                        if (x >= 0 && x < w && y >= 0 && y < h)
                        {
                            var (r, g, b) = ReadPixelFromBuffer(scan0, stride, x, y, bpp);
                            sumR += r;
                            sumG += g;
                            sumB += b;
                            count++;
                        }
                    }

                    double avgB = (double)sumB / count;
                    double avgR = (double)sumR / count;
                    double avgG = (double)sumG / count;

                    // Detección con rango de tolerancia en lugar de valor exacto
                    const int TARGET_B = 24;
                    const int TOLERANCE = 3; // Tolerancia de ±3 para el valor B

                    bool isActionColorInRange = Math.Abs(avgB - TARGET_B) <= TOLERANCE;
                    bool shouldCapture = isActionColorInRange && !_executeCapture;

                    // Detección de flop mejorada
                    const int FLOP_TARGET_B = 255;
                    const int FLOP_TOLERANCE = 10;
                    bool isFlopVisible = Math.Abs(flopB - FLOP_TARGET_B) <= FLOP_TOLERANCE;
                    bool shouldCaptureFlop = shouldCapture && isFlopVisible;

                    return new DetectionResult
                    {
                        ActionColor = primaryActionColor,
                        FlopColor = flopColor,
                        AverageActionB = avgB,
                        SampleCount = count,
                        IsActionColorInRange = isActionColorInRange,
                        ShouldCapture = shouldCapture,
                        ShouldCaptureFlop = shouldCaptureFlop,
                        IsFlopVisible = isFlopVisible
                    };
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }
            }
            catch (Exception ex)
            {
                _detectionLoggerService.LogDetectionError($"Error en detección mejorada: {ex.Message}", ex);

                // Fallback a detección simple (GetPixel como último recurso)
                var scaledAction = GetScaledRegion(regionAction);
                var scaledFlop = GetScaledRegion(flop);
                Color actionColor = bitmap.GetPixel(scaledAction.X, scaledAction.Y);
                Color flopColor = bitmap.GetPixel(scaledFlop.X, scaledFlop.Y);

                return new DetectionResult
                {
                    ActionColor = actionColor,
                    FlopColor = flopColor,
                    AverageActionB = actionColor.B,
                    SampleCount = 1,
                    IsActionColorInRange = actionColor.B == 24,
                    ShouldCapture = actionColor.B == 24 && !_executeCapture,
                    ShouldCaptureFlop = actionColor.B == 24 && !_executeCapture && flopColor.B == 255,
                    IsFlopVisible = flopColor.B == 255
                };
            }
        }

        /// <summary>
        /// Lee un píxel directamente del buffer de LockBits (soporta 24bpp y 32bpp)
        /// </summary>
        private static (byte R, byte G, byte B) ReadPixelFromBuffer(nint scan0, int stride, int x, int y, int bpp)
        {
            int offset = y * stride + x * bpp;
            byte b = System.Runtime.InteropServices.Marshal.ReadByte(scan0, offset);
            byte g = System.Runtime.InteropServices.Marshal.ReadByte(scan0, offset + 1);
            byte r = System.Runtime.InteropServices.Marshal.ReadByte(scan0, offset + 2);
            return (r, g, b);
        }

        /// <summary>
        /// Resultado de la detección mejorada
        /// </summary>
        private record DetectionResult
        {
            public Color ActionColor { get; init; }
            public Color FlopColor { get; init; }
            public double AverageActionB { get; init; }
            public int SampleCount { get; init; }
            public bool IsActionColorInRange { get; init; }
            public bool ShouldCapture { get; init; }
            public bool ShouldCaptureFlop { get; init; }
            public bool IsFlopVisible { get; init; }
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
                _logger.LogError(ex, "Error al establecer color");
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

            var firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                _selectedRegion.PosX, _selectedRegion.PosY,
                _selectedRegion.Width, _selectedRegion.Height,
                _selectedRegion.Umbral ?? 0,
                _selectedRegion.IsOnlyNumber ?? false);

            using var secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                _selectedRegion.PosX, _selectedRegion.PosY,
                _selectedRegion.Width, _selectedRegion.Height,
                _selectedRegion.InactiveUmbral ?? 0,
                _selectedRegion.IsOnlyNumber ?? false);

            var result = string.Empty;

            if (_selectedRegion.IsOnlyNumber == true)
            {
                if (string.IsNullOrEmpty(firstOcr.Text))
                    firstOcr.Text = "0";

                if (string.IsNullOrEmpty(secondOcr.Text))
                    secondOcr.Text = "0";

                var ocr1 = decimal.TryParse(firstOcr.Text, out var v1) ? v1 : 0m;
                var ocr2 = decimal.TryParse(secondOcr.Text, out var v2) ? v2 : 0m;

                if (ocr2 >= ocr1)
                    result = ocr2.ToString();
                else
                    result = ocr1.ToString();
            }

            tbTestTexto.Text = !string.IsNullOrEmpty(result) ? result : "Sin resultado";

            // Liberar imagen anterior y transferir la de firstOcr al PictureBox
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = firstOcr.Image;
            firstOcr.Image = null; // Evitar que Dispose libere la imagen transferida
            firstOcr.Dispose();
        }

        /// <summary>
        /// Maneja el evento de clic en el botón de prueba de carta
        /// </summary>
        private async void btnTestCarta_Click(object sender, EventArgs e)
        {
            // Validación de región seleccionada
            if (_selectedRegion == null || _formImage.pbImage.Image == null)
            {
                _logger.LogError("No se ha seleccionado una región o la imagen es nula.");
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

                // Carga de cartas desde cache singleton
                _cardsImages ??= await _cardCacheService.GetCardsAsync();

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
                _logger.LogError(ex, "Error al probar carta");
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
        /// Formatea las cartas del hero y del board para logs legibles.
        /// Ejemplo: "Hero: [As Qc]  Board: [Qh 3h 7s] + [5d]"
        /// </summary>
        private string FormatCardsForLog(BoardPosition street)
        {
            return _coordinator.FormatCardsForLog(_playerGameState, street);
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
                    case "tpHistorial":
                        ApplyHistorialTabStyle(tab);
                        break;
                    case "tpBankroll":
                        ApplyBankrollTabStyle(tab);
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

        #endregion

        /// <summary>
        /// Aplica estilo moderno a la pestaña de historial
        /// </summary>
        private void ApplyHistorialTabStyle(TabPage historialTab)
        {
            historialTab.SuspendLayout();

            // Labels título
            foreach (var lbl in new[] { lblSesionesTitle, lblManosTitle })
            {
                lbl.BackColor = AppThemeHelper.PrimaryDark;
                lbl.ForeColor = Color.White;
                lbl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                lbl.Padding = new Padding(6, 0, 0, 0);
            }

            // Estilizar ambas grillas
            foreach (var dgv in new[] { dgvSessions, dgvSessionHands })
            {
                dgv.EnableHeadersVisualStyles = false;
                dgv.BackgroundColor = AppThemeHelper.BackgroundMain;
                dgv.BorderStyle = BorderStyle.None;
                dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                dgv.GridColor = AppThemeHelper.BorderLight;
                dgv.Font = new Font("Segoe UI", 9F);
                dgv.ColumnHeadersDefaultCellStyle.BackColor = AppThemeHelper.PrimaryDark;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryDark;
                dgv.ColumnHeadersHeight = 35;
                dgv.DefaultCellStyle.BackColor = AppThemeHelper.BackgroundCard;
                dgv.DefaultCellStyle.ForeColor = AppThemeHelper.PrimaryDark;
                dgv.DefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryLight;
                dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                dgv.RowTemplate.Height = 28;
                dgv.AlternatingRowsDefaultCellStyle.BackColor = AppThemeHelper.BackgroundMain;
            }

            // Label de stats
            lblSessionStats.BackColor = AppThemeHelper.BackgroundMain;
            lblSessionStats.ForeColor = AppThemeHelper.PrimaryDark;
            lblSessionStats.Font = new Font("Segoe UI", 9F);

            historialTab.ResumeLayout(true);
        }

        private void ApplyBankrollTabStyle(TabPage bankrollTab)
        {
            bankrollTab.SuspendLayout();

            // Paneles con fondo blanco y borde sutil
            foreach (var panel in new[] { pnlBankrollInfo, pnlRiskInfo, pnlPerformance })
            {
                panel.BackColor = AppThemeHelper.BackgroundCard;
                panel.BorderStyle = BorderStyle.None;
                panel.Paint += (s, e) =>
                {
                    using var pen = new Pen(AppThemeHelper.BorderLight, 1f);
                    e.Graphics.DrawRectangle(pen, 0, 0, ((Panel)s!).Width - 1, ((Panel)s!).Height - 1);
                };
            }

            // DataGridView con mismo estilo que historial
            dgvBankrollHistory.EnableHeadersVisualStyles = false;
            dgvBankrollHistory.BackgroundColor = AppThemeHelper.BackgroundMain;
            dgvBankrollHistory.BorderStyle = BorderStyle.None;
            dgvBankrollHistory.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvBankrollHistory.GridColor = AppThemeHelper.BorderLight;
            dgvBankrollHistory.Font = new Font("Segoe UI", 9F);
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.BackColor = AppThemeHelper.PrimaryDark;
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            dgvBankrollHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryDark;
            dgvBankrollHistory.ColumnHeadersHeight = 32;
            dgvBankrollHistory.DefaultCellStyle.BackColor = AppThemeHelper.BackgroundCard;
            dgvBankrollHistory.DefaultCellStyle.ForeColor = AppThemeHelper.PrimaryDark;
            dgvBankrollHistory.DefaultCellStyle.SelectionBackColor = AppThemeHelper.PrimaryLight;
            dgvBankrollHistory.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvBankrollHistory.RowTemplate.Height = 26;
            dgvBankrollHistory.AlternatingRowsDefaultCellStyle.BackColor = AppThemeHelper.BackgroundMain;

            bankrollTab.ResumeLayout(true);
        }

        private void tbJuego_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Ajusta el tamaño de apuesta basado en stack dinámico
        /// </summary>
        private string AdjustBetSize(string action, decimal heroStack, decimal potSize, int numOpponents, bool isPaired, bool isCoordinated, bool isDry, bool isInPosition)
            => _coordinator.AdjustBetSize(action, heroStack, potSize, numOpponents, isPaired, isCoordinated, isDry, isInPosition);

        #region Pestaña Historial

        private bool _historialLoaded;
        private List<SessionStatsDto>? _loadedSessions;
        private List<HandRecord>? _loadedHands;

        /// <summary>
        /// Inicializa la pestaña Historial: columnas, estilos y eventos
        /// </summary>
        private void InitializeHistorialTab()
        {
            // Configurar columnas de sesiones
            dgvSessions.AutoGenerateColumns = false;
            dgvSessions.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "Mesa", DataPropertyName = "TableName", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "StartTime", HeaderText = "Inicio", DataPropertyName = "StartTime", Width = 130 },
                new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Duración", DataPropertyName = "Duration", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "TotalHands", HeaderText = "Manos", DataPropertyName = "TotalHands", Width = 60 }
                //new DataGridViewTextBoxColumn { Name = "TotalProfit", HeaderText = "Profit", DataPropertyName = "TotalProfit", Width = 80 },
                //new DataGridViewTextBoxColumn { Name = "BBPer100", HeaderText = "BB/100", DataPropertyName = "BBPer100", Width = 70 }
            );

            // Configurar columnas de manos
            dgvSessionHands.AutoGenerateColumns = false;
            dgvSessionHands.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "HandNumber", HeaderText = "Hand#", DataPropertyName = "HandNumber", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "Cards", HeaderText = "Cartas", DataPropertyName = "Cards", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Position", HeaderText = "Pos", DataPropertyName = "Position", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "LastStreet", HeaderText = "Street", DataPropertyName = "LastStreet", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "Resultado", DataPropertyName = "Result", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "ProfitLoss", HeaderText = "P/L", DataPropertyName = "ProfitLoss", Width = 70 }
            );

            // Eventos
            tpHistorial.Enter += async (s, e) =>
            {
                if (!_historialLoaded)
                    await LoadSessionsWithStatsAsync();
            };
            dgvSessions.SelectionChanged += DgvSessions_SelectionChanged;
            dgvSessionHands.CellDoubleClick += DgvSessionHands_CellDoubleClick;
            dgvSessions.CellFormatting += DgvSessions_CellFormatting;
            dgvSessionHands.CellFormatting += DgvSessionHands_CellFormatting;
            btnBacktest.Click += BtnBacktest_Click;
        }

        /// <summary>
        /// Carga las sesiones recientes con sus estadísticas
        /// </summary>
        private async Task LoadSessionsWithStatsAsync()
        {
            try
            {
                _loadedSessions = await _gameLoggerService.GetRecentSessionsWithStatsAsync(50);

                var displayData = _loadedSessions.Select(s => new
                {
                    s.TableName,
                    StartTime = s.StartTime.ToString("dd/MM HH:mm"),
                    Duration = FormatDuration(s.EndTime - s.StartTime),
                    s.TotalHands,
                    TotalProfit = s.TotalProfit.ToString("+0.00;-0.00"),
                    BBPer100 = s.BBPer100.ToString("+0.0;-0.0")
                }).ToList();

                dgvSessions.DataSource = displayData;
                _historialLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando sesiones");
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1)
                return "< 1m";
            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
                : $"{duration.Minutes}m";
        }

        private async void DgvSessions_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvSessions.SelectedRows.Count == 0 || _loadedSessions == null)
                return;

            int idx = dgvSessions.SelectedRows[0].Index;
            if (idx < 0 || idx >= _loadedSessions.Count)
                return;

            var session = _loadedSessions[idx];

            try
            {
                _loadedHands = await _gameLoggerService.GetHandsForSessionAsync(session.Id);

                var displayData = _loadedHands.Select(h => new
                {
                    h.HandNumber,
                    Cards = $"{h.HeroCard1} {h.HeroCard2}",
                    Position = FormatPosition(h.HeroPosition),
                    LastStreet = h.LastStreetPlayed.ToString(),
                    Result = h.Result.ToString(),
                    ProfitLoss = (h.HeroStackEnd - h.HeroStackStart).ToString("+0.00;-0.00")
                }).ToList();

                dgvSessionHands.DataSource = displayData;

                lblSessionStats.Text = $"Sesión: {session.TableName} | {session.TotalHands} manos | " +
                    $"{session.TotalProfit:+0.00;-0.00} | {session.BBPer100:+0.0;-0.0} BB/100";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando manos");
            }
        }

        private static string FormatPosition(TablePosition pos) => pos switch
        {
            TablePosition.Button => "BTN",
            TablePosition.CutOff => "CO",
            TablePosition.Middle => "MP",
            TablePosition.Early => "EP",
            TablePosition.SmallBlind => "SB",
            TablePosition.BigBlind => "BB",
            _ => pos.ToString()
        };

        private void DgvSessionHands_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _loadedHands == null || e.RowIndex >= _loadedHands.Count)
                return;

            var hand = _loadedHands[e.RowIndex];
            decimal bigBlind = _loadedSessions != null && dgvSessions.SelectedRows.Count > 0
                ? _loadedSessions[dgvSessions.SelectedRows[0].Index].BigBlind
                : 0.50m;

            using var frm = new FrmHandDetail(hand, bigBlind);
            frm.ShowDialog(this);
        }

        /// <summary>
        /// Aplica colores condicionales verde/rojo a las columnas Profit y BB/100
        /// </summary>
        private void DgvSessions_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;

            string colName = dgvSessions.Columns[e.ColumnIndex].Name;
            if (colName is "TotalProfit" or "BBPer100")
            {
                string val = e.Value.ToString() ?? "";
                if (val.StartsWith('+'))
                    e.CellStyle.ForeColor = AppThemeHelper.Success;
                else if (val.StartsWith('-'))
                    e.CellStyle.ForeColor = AppThemeHelper.Danger;
            }
        }

        /// <summary>
        /// Aplica colores condicionales a Result y P/L en la grilla de manos
        /// </summary>
        private void DgvSessionHands_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;

            string colName = dgvSessionHands.Columns[e.ColumnIndex].Name;
            string val = e.Value.ToString() ?? "";

            if (colName == "Result")
            {
                e.CellStyle.ForeColor = val switch
                {
                    "Won" => AppThemeHelper.Success,
                    "Lost" => AppThemeHelper.Danger,
                    _ => AppThemeHelper.PrimaryLight
                };
            }
            else if (colName == "ProfitLoss")
            {
                if (val.StartsWith('+'))
                    e.CellStyle.ForeColor = AppThemeHelper.Success;
                else if (val.StartsWith('-'))
                    e.CellStyle.ForeColor = AppThemeHelper.Danger;
            }
        }

        /// <summary>
        /// Ejecuta backtest A/B comparando decisiones históricas con el motor actual.
        /// </summary>
        private async void BtnBacktest_Click(object? sender, EventArgs e)
        {
            btnBacktest.Enabled = false;
            btnBacktest.Text = "Analizando...";

            try
            {
                // Cargar todas las manos disponibles
                var hands = await _gameLoggerService.GetRecentHandsAsync(500);

                if (hands == null || hands.Count == 0)
                {
                    MessageBox.Show("No hay manos en el historial para analizar.",
                        "Backtest", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Determinar BigBlind de la sesión
                decimal bigBlind = _loadedSessions?.FirstOrDefault()?.BigBlind ?? 0.50m;

                // Ejecutar backtest
                var backtester = new DecisionMaker.Services.StrategyBacktester(_postflopDecisionService);
                var result = backtester.RunBacktest(hands, bigBlind);

                // Mostrar resultado en popup
                var msg = result.ToString();
                MessageBox.Show(msg, $"Backtest A/B — {hands.Count} manos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en backtest: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBacktest.Enabled = true;
                btnBacktest.Text = "Backtest A/B";
            }
        }

        #endregion

        #region Pestaña Bankroll

        private bool _bankrollLoaded;

        private void InitializeBankrollTab()
        {
            dgvBankrollHistory.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Fecha", DataPropertyName = "Date", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Hands", HeaderText = "Manos", DataPropertyName = "Hands", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "Profit", HeaderText = "Profit", DataPropertyName = "Profit", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "BBPer100", HeaderText = "BB/100", DataPropertyName = "BBPer100", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "BankrollAfter", HeaderText = "Bankroll", DataPropertyName = "BankrollAfter", Width = 90 }
            );

            tpBankroll.Enter += (s, e) =>
            {
                if (!_bankrollLoaded)
                {
                    UpdateBankrollDashboard();
                    _bankrollLoaded = true;
                }
            };

            dgvBankrollHistory.CellFormatting += DgvBankrollHistory_CellFormatting;
        }

        private void DgvBankrollHistory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            var colName = dgvBankrollHistory.Columns[e.ColumnIndex].Name;
            if (colName == "Profit" && e.Value is string val)
            {
                if (val.StartsWith('+'))
                    e.CellStyle.ForeColor = AppThemeHelper.Success;
                else if (val.StartsWith('-'))
                    e.CellStyle.ForeColor = AppThemeHelper.Danger;
            }
        }

        #endregion

        #region Pestaña Métricas

        private System.Windows.Forms.Timer? _metricsRefreshTimer;
        private int _cycleCounter;
        private IMetricsCollector? _metrics;

        private void InitializeMetricsTab()
        {
            dgvMetrics.Columns.Clear();
            dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Fase",
                HeaderText = "Fase",
                FillWeight = 180f,
                ReadOnly = true,
            });
            foreach (var prefix in new[] { "Last", "Session" })
                foreach (var suffix in new[] { "P50", "P95", "Max", "Count" })
                {
                    dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = $"{prefix}{suffix}",
                        HeaderText = prefix == "Last" ? $"Últ. {suffix}" : $"Ses. {suffix}",
                        FillWeight = 60f,
                        ReadOnly = true,
                        DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
                    });
                }

            foreach (var cat in TelemetryCategories.DisplayOrder)
            {
                int rowIdx = dgvMetrics.Rows.Add(cat, "—", "—", "—", "0", "—", "—", "—", "0");
                dgvMetrics.Rows[rowIdx].Tag = cat;
            }

            _metricsRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _metricsRefreshTimer.Tick += (s, e) => RefreshMetricsGrid();

            tbControl.Selected += TabControl_Selected;
            btnResetMetrics.Click += BtnResetMetrics_Click;
        }

        private void TabControl_Selected(object? sender, TabControlEventArgs e)
        {
            if (e.TabPage == tabMetrics)
            {
                RefreshMetricsGrid();
                _metricsRefreshTimer?.Start();
            }
            else
            {
                _metricsRefreshTimer?.Stop();
            }
        }

        private void RefreshMetricsGrid()
        {
            if (_metrics == null) return;

            var snap = _metrics.SnapshotSession();

            lblCurrentHand.Text = $"Mano actual: {snap.CurrentHandId ?? "—"}";
            lblCycleCount.Text = $"Ciclos: {_cycleCounter:N0}";
            lblLastUpdate.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";

            foreach (DataGridViewRow row in dgvMetrics.Rows)
            {
                if (row.Tag is not string category) continue;

                FillCells(row, 1, snap.LastHand.TryGetValue(category, out var last) ? last : null);
                FillCells(row, 5, snap.Session.TryGetValue(category, out var session) ? session : null);
            }
        }

        private static void FillCells(DataGridViewRow row, int startCol, CategoryStats? stats)
        {
            if (stats is null)
            {
                row.Cells[startCol + 0].Value = "—";
                row.Cells[startCol + 1].Value = "—";
                row.Cells[startCol + 2].Value = "—";
                row.Cells[startCol + 3].Value = "0";
                return;
            }
            row.Cells[startCol + 0].Value = stats.P50Ms.ToString("N0");
            row.Cells[startCol + 1].Value = stats.P95Ms.ToString("N0");
            row.Cells[startCol + 2].Value = stats.MaxMs.ToString("N0");
            row.Cells[startCol + 3].Value = stats.Count.ToString("N0");
        }

        private void BtnResetMetrics_Click(object? sender, EventArgs e)
        {
            _metrics?.ResetSession();
            RefreshMetricsGrid();
            _logger.LogInformation("Telemetría: sesión reseteada por el usuario");
        }

        #endregion

        // ============================================================================
        // INVENTARIO DE ESTADO MUTABLE — refactor-frmmain-coordinators (Fase 1.4)
        // ----------------------------------------------------------------------------
        // Este bloque documenta, antes de empezar la migración, a qué servicio debe
        // mudarse cada campo mutable de FrmMain. Se ELIMINARÁ en la Fase 7.3 cuando
        // el cutover al GameLoopCoordinator esté validado.
        //
        // Control del loop → GameLoopCoordinator (scoped, propio):
        //   _executeCapture         (volatile bool)  → reemplazado por CancellationToken
        //   _backgroundExecute      (volatile bool)  → reemplazado por CancellationToken
        //   _speed                  (int)            → GameLoopOptions.CaptureIntervalMs
        //
        // Estado cross-street / cross-iteración → PostflopGameContext (ya scoped):
        //   _heroStackPreRebuy      (decimal)        → PostflopGameContext.TrackHeroStack()
        //   _newHand                (bool)           → PostflopGameContext.NewHandDetected
        //   _newTableHand           (long)           → PostflopGameContext.CurrentHandNumber
        //   _tableHand              (string)         → PostflopGameContext.CurrentHandNumber (str)
        //   _previousSBPlayerName   (string)         → PostflopGameContext.PreviousBlinds
        //   _previousBBPlayerName   (string)         → PostflopGameContext.PreviousBlinds
        //   _lastActivePlayerCount  (int)            → PostflopGameContext.LastActivePlayerCount
        //   _flopResult             (Poker...Result) → GameLoopResult (por iteración)
        //   _turnResult             (Poker...Result) → GameLoopResult
        //   _riverResult            (Poker...Result) → GameLoopResult
        //   _turnBoardTexture       (enum)           → GameLoopResult.BoardTexture
        //   _riverBoardTexture      (enum)           → GameLoopResult.BoardTexture
        //   _scrapeFlopResult       (TableScrape...) → GameLoopResult o contexto
        //   _responseAction         (ResponseAction) → GameLoopResult.DecisionResult
        //
        // Lectura de mesa (mano en curso) → permanece vía IScreenReaderService /
        // ITableLayoutService / IGameCoordinator (ya servicios scoped):
        //   _playerGameState        (PlayerGameState) → ITableLayoutService
        //   _handle                 (IntPtr)          → se mantiene en FrmMain (ventana)
        //   _tableName              (string)          → ITableLayoutService
        //   _session                (string)          → GameLoggerService (ya)
        //   _folderPath             (string)          → se mantiene en FrmMain (Config tab)
        //   _pathResume             (string)          → UiSyncService
        //   _pictureUmbralBet       (int)             → IScreenReaderService config
        //
        // Estado puro de UI → permanece en FrmMain:
        //   _lastChecked            (RadioButton?)
        //   _img                    (Image?)
        //   _isClosing              (bool)
        //   _historialLoaded        (bool)
        //   _bankrollLoaded         (bool)
        //
        // Inyección DI → se mantienen hasta Fase 7.5 (consolidación en facade):
        //   _pokerCalculator, _betSizingService, _postflopDecisionService,
        //   _boardTextureAnalyzer, _opponentTracker → absorbidos por IPokerDecisionFacade
        //   _coordinator, _screenReader, _tableLayout → consumidos por
        //     GameLoopCoordinator y eliminados de FrmMain tras cutover
        //   Resto (*UseCase, *Service de infraestructura) → permanecen o se reasignan
        //     a coordinator según uso real, evaluado en Fase 6.
        // ============================================================================
    }
}
