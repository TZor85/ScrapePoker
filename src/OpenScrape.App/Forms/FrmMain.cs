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
using OpenScrape.App.Forms.Controls;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Bankroll;
using OpenScrape.App.Services.Calibration;
using OpenScrape.App.Services.Exploitability;
using OpenScrape.App.Services.Logging;
using OpenScrape.App.Services.Tracking;
using OpenScrape.App.Telemetry;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Algorithms;
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
        // Se instancian en el constructor salvo en modo background (ExecuteInBackground).
        FormImage _formImage = null!;
        FrmOverlay _frmOverlay = null!;
        #endregion

        #region [Regions]
        List<Regions> _regions = new List<Regions>();
        List<ImageRegion> _images = new List<ImageRegion>();
        #endregion

        #region [Private Fields]
        private List<Card> _cards = new();
        private RadioButton? _lastChecked;
        private PlayerGameState _playerGameState = new();
        private TableScrapeFlopResult _scrapeFlopResult = new();
        private ResponseAction _responseAction = new();
        private string _folderPath = string.Empty;
        private string _tableHand = string.Empty;
        private List<RegionTableMap>? _regionsTableMap;
        private readonly RegionLookupCache _regionLookupCache;
        private readonly CardCacheService _cardCacheService;
        private readonly string _pathResume;
        private Dictionary<TablePosition, Dictionary<TablePosition, decimal>> _preflopHeroPosition = new();
        private string _session = string.Empty;
        private IntPtr _handle;
        private User32.RECT _locWindowRect = new();
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
        private int _cycleCounter;
        private IMetricsCollector? _metrics;
        private volatile bool _backgroundExecute;
        private IReadOnlyList<Table>? _tables;
        private List<Table>? _dataTables;
        #endregion

        #region [Services and UseCases]
        private readonly GetWindowsScreenUseCase _useCase;
        private readonly ActionScenarioUseCases _actionScenarioUseCases;
        private readonly RegionTableMapUseCases _regionTableMapUseCases;
        private readonly IDecisionEngine _decisionEngine;
        private readonly ImageCropperService _imageCropperService;
        private readonly IDocumentStore _dataBase;
        // _sessionDB eliminado: se usan sesiones locales con using para evitar connection leak
        private readonly ISetFlopForceBoardUseCase _setFlopForceBoardUseCase;
        private readonly IGetHashImageUseCase _getHashImageUseCase;
        private readonly IGetCropImageUseCase _getCropImageUseCase;
        private readonly IGetCardsFlopUseCase _getCardsFlopUseCase;
        private readonly IGetCardsTurnUseCase _getCardsTurnUseCase;
        private readonly IGetCardsRiverUseCase _getCardsRiverUseCase;
        private readonly IOutsCalculatorUseCase _outsCalculatorUseCase;
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

        // refactor-frmmain Fase 1: DetectionLoopService extraído del BackgroundWorker
        private readonly IDetectionLoopService _detectionLoopService;
        private CancellationTokenSource? _detectionCts;
        // refactor-frmmain Fase 2: CaptureService extraído de GetImageWhilePlaying
        private readonly ICaptureService _captureService;
        // refactor-frmmain Fase 3: TableReaderService extraído de SetBetPlayer/SetHeroStack/etc.
        private readonly ITableReaderService _tableReaderService;
        // refactor-frmmain Fase 5: UiUpdateService extraído de UpdateUIWithResults y relacionados
        private readonly IUiUpdateService _uiUpdateService;
        // refactor-frmmain Fase 7: UserControls de TabPageContributions
        private readonly HistorialTabControl _historialTab;
        private readonly BankrollTabControl _bankrollTab;
        private readonly MetricsTabControl _metricsTab;
        // refactor-frmmain Fase 8: RegionEditorControl extraído de FrmMain
        private readonly RegionEditorControl _regionEditor;
        #endregion

        /// <summary>
        /// Constructor del formulario principal
        /// </summary>
        public FrmMain(IDocumentStore dataBase,
                        ActionScenarioUseCases actionScenarioUseCases,
                        CardUseCases cardUseCases,
                        RegionTableMapUseCases regionTableMapUseCases,
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
                        IDecisionEngine decisionEngine,
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
                        IMetricsCollector metrics,
                        IDetectionLoopService detectionLoopService,
                        ICaptureService captureService,
                        ITableReaderService tableReaderService,
                         IUiUpdateService uiUpdateService,
                         HistorialTabControl historialTab,
                         BankrollTabControl bankrollTab,
                         MetricsTabControl metricsTab,
                         RegionEditorControl regionEditor)
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
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _screenReader = screenReader ?? throw new ArgumentNullException(nameof(screenReader));
            _tableLayout = tableLayout ?? throw new ArgumentNullException(nameof(tableLayout));
            _detectionLoopService = detectionLoopService ?? throw new ArgumentNullException(nameof(detectionLoopService));
            _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
            _tableReaderService = tableReaderService ?? throw new ArgumentNullException(nameof(tableReaderService));
            _uiUpdateService = uiUpdateService ?? throw new ArgumentNullException(nameof(uiUpdateService));
            _historialTab = historialTab ?? throw new ArgumentNullException(nameof(historialTab));
            _bankrollTab = bankrollTab ?? throw new ArgumentNullException(nameof(bankrollTab));
            _metricsTab = metricsTab ?? throw new ArgumentNullException(nameof(metricsTab));
            _regionEditor = regionEditor ?? throw new ArgumentNullException(nameof(regionEditor));

            // Suscribirse a eventos del DetectionLoopService (Fase 1)
            SubscribeDetectionEvents();

            // Resto de inicialización existente...
            _session = GenerateRandomNumbers();
            _lastChecked = new RadioButton();

            _pathResume = Path.Combine(DEFAULT_SAMPLES_PATH,
                $"resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt");

            FormClosing += FrmMain_FormClosing;

            // Fase 7: Inicializar UserControls en lugar de llamadas directas
            _historialTab.Initialize(dgvSessions, dgvSessionHands, btnBacktest, lblSessionStats);
            tpHistorial.Enter += async (s, e) => await _historialTab.OnTabEnterAsync();
            _bankrollTab.Initialize(dgvBankrollHistory, tpBankroll,
                lblBankrollCurrent, lblBankrollPeak, lblBankrollMaxDD,
                lblWinRate, lblStdDev, lblTotalSessions, lblTotalHands,
                lblAvgSessionProfit, lblRiskOfRuin, lblRecommendation);
            _metricsTab.Initialize(dgvMetrics, btnResetMetrics, lblCurrentHand,
                lblCycleCount, lblLastUpdate, tbControl, tabMetrics, () => _cycleCounter);

            // Fase 8: Suscribir evento de guardado de regiones para recargar datos
            _regionEditor.RegionsSaved += async (s, args) =>
            {
                await LoadRegionTableMapAsync();
                _regionEditor.LoadTreeViewRegions(_regionsTableMap);
            };
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

            // refactor-frmmain Fase 1: parada del DetectionLoopService
            if (_detectionLoopService.IsRunning)
            {
                try
                {
                    _detectionCts?.Cancel();
                    await _detectionLoopService.StopAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deteniendo DetectionLoopService al cerrar");
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
                _uiUpdateService.SetOverlay(_frmOverlay);
                cbSpeed.SelectedIndex = 0;

                await using var sessionDB = _dataBase.LightweightSession();
                await LoadRegionTableMapAsync(sessionDB);
                await LoadTablesAsync(sessionDB);

                var allCards = await sessionDB.Query<Card>().ToListAsync();
                _cards.AddRange(allCards);
                _uiUpdateService.SetCardList(_cards);

                // Fase 5: Attach controles para que UiUpdateService pueda actualizar la UI
                // Se hace aquí (FrmMain_Load) y no en btnWindow_Click porque en test mode
                // no se ejecuta btnWindow_Click.
                _uiUpdateService.Attach(new UiUpdateControls(
                    tbResume, pbHeroCard0, pbHeroCard1, pbBoard1, pbBoard2, pbBoard3, pbBoard4, pbBoard5,
                    lbNamePlayerOne, lbNamePlayerTwo, lbNamePlayerThree, lbNamePlayerFour, lbNamePlayerFive,
                    lbBetPlayerOne, lbBetPlayerTwo, lbBetPlayerThree, lbBetPlayerFour, lbBetPlayerFive,
                    lbBetHero, lbPot,
                    pbbuttonPlayerOne, pbButtonPlayerTwo, pbButtonPlayerThree, pbButtonPlayerFour, pbButtonPlayerFive, pbButtonHero,
                    lbAction, lbPositionAction, lbTableHand, lbTableName
                ));

                _formImage.Location = new Point(Width, Location.Y);
                _formImage.Show();

                // Fase 8: Inicializar RegionEditorControl con controles de la pestaña de configuración
                _regionEditor.Initialize(
                    twRegionsConfig, _formImage.pbImage,
                    tbX, tbY, tbWidth, tbHeight, tbColor, tbRegionName,
                    tbRegionUmbral, tbRegionInactUmbral,
                    btnUpdateMap, btnTestColor, btnTestTexto, btnTestCarta,
                    rgRegion,
                    cbRegionColor, cbRegionHash, cbRegionBoard, cbRegionNumber,
                    btnUp, btnDown, btnLeft, btnRight,
                    btnUpLeft, btnUpRight, btnDownLeft, btnDownRight,
                    btnPlusWidth, btnMinusWidth, btnPlusHeight, btnMinusHeight,
                    cbSpeed,
                    pbColorDebug, tbTestColor, tbTestTexto, pbTestCarta,
                    pictureBox1, pbRegionColor,
                    _regionsTableMap);

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
                // LoadTreeViewRegions movido a RegionEditorControl (Fase 8)
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
                Contracts.DecisionResult? preflopDecision = null;

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
                    _tableLayout.SetEmptyPlayer(_formImage.Screenshot, _playerGameState);
                    _tableLayout.SetSitOutPlayer(_formImage.Screenshot, _playerGameState);
                    _tableLayout.SetActivePlayer(_formImage.Screenshot, _playerGameState);
                    _tableLayout.InitializePlayers(_formImage.Screenshot, _playerGameState);
                }
                else
                {
                    _tableLayout.SetEmptyPlayer(_formImage.Screenshot, _playerGameState);
                    _tableLayout.SetActivePlayer(_formImage.Screenshot, _playerGameState);
                    _tableLayout.RefreshPlayerStates(_formImage.Screenshot, _playerGameState);

                    int currentActiveCount = _playerGameState.Players.Count(p => !p.Empty && !p.SitOut && p.ValuePosition != 0);
                    bool playerCountChanged = currentActiveCount != _lastActivePlayerCount;
                    _lastActivePlayerCount = currentActiveCount;

                    _logger.LogDebug("Jugadores activos: {Active}, Cambió: {Changed}, Posición actual: {Position}", currentActiveCount, playerCountChanged, _playerGameState.Position);

                    bool shouldRecalculate = _playerGameState.Position == TablePosition.None || playerCountChanged;

                    if (shouldRecalculate)
                    {
                        _tableLayout.SetDealerPlayer(_formImage.Screenshot, _playerGameState);
                        if (_tableLayout.DealerValuePosition >= 0 && _playerGameState.Position != TablePosition.None)
                            _tableLayout.SetVillainPosition(_playerGameState, _playerGameState.Position, _tableLayout.DealerValuePosition);
                    }
                }

                SetBetPlayer();
                SetHeroStack();
                _tableLayout.RetryEmptyAliases(_formImage.Screenshot, _playerGameState);
                _tableLayout.ValidatePlayerStates(_playerGameState);

                // Procesar la información de la mesa
                preflopDecision = await ProcessTableInfoAsync();

                // Actualizar la interfaz con los resultados
                _uiUpdateService.UpdateResults(_playerGameState, preflopDecision, _responseAction, _tableHand, _tableName);

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
        private async Task<Contracts.DecisionResult?> ProcessTableInfoAsync()
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
                        return null;
                    }
                }

                _gameLoopStateMachine.TryTransition(GameState.PreflopAction);
                return await ProcessPreflopAsync();
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
                        return null;
                    }
                }

                await ProcessPostFlopAsync();
                return null;
            }
        }

        /// <summary>
        /// Procesa la fase de preflop
        /// </summary>
        private async Task<Contracts.DecisionResult> ProcessPreflopAsync()
        {
            if (_playerGameState.Position == TablePosition.None)
            {
                _logger.LogError("Posición del jugador no detectada, saltando procesamiento preflop");
                return new Contracts.DecisionResult { Action = string.Empty };
            }

            if (_preflopHeroPosition == null || !_preflopHeroPosition.ContainsKey(_playerGameState.Position))
            {
                _logger.LogError("PreflopHeroPosition no tiene datos para posición {Position}, reconstruyendo...", _playerGameState.Position);
                _preflopHeroPosition = GetPreflopHeroPosition();

                if (!_preflopHeroPosition.ContainsKey(_playerGameState.Position))
                {
                    _logger.LogError("Sigue sin tener datos para posición {Position}, saltando preflop", _playerGameState.Position);
                    return new Contracts.DecisionResult { Action = string.Empty };
                }
            }

            var decision = await _decisionEngine.DecideAsync(
                PlayerStateMapper.MapPreflop(
                    _playerGameState,
                    _responseAction.IsSecondAction,
                    villain: null,
                    preflopBets: _preflopHeroPosition.TryGetValue(_playerGameState.Position, out var heroBets)
                        ? heroBets
                        : null));

            _responseAction.Action = decision.Action;
            if (Enum.TryParse<HandSituation>(decision.Situation, out var nextSituation))
                _responseAction.HandSituation = nextSituation;

            if (decision.Action.Contains("Call"))
                _playerGameState.HandSituation = HandSituation.Call;

            _frmOverlay.UpdateStreetPhase("Pre-Flop");
            SetPreflopAggressors();
            return decision;
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
        private async Task ProcessPostFlopAsync()
        {
            _logger.LogInformation("ProcessPostFlopAsync: Estado actual = {State}", _gameLoopStateMachine.CurrentState);

            // Detectar villanos que foldearon mid-hand (actualiza Active/numOpponents)
            _tableLayout.DetectFoldedPlayers(_formImage.Screenshot, _playerGameState, _gameLoopStateMachine.CurrentState);

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
                    await ProcessFlopAsync();
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
                await ProcessFlopAsync();
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
        /// Delega en ITableReaderService (Fase 3).
        /// </summary>
        private bool IsBoardCardVisible(string cardRegionName)
        {
            if (_formImage.pbImage.Image is Bitmap bmp)
                return _tableReaderService.IsBoardCardVisible(bmp, cardRegionName);
            return false;
        }

        /// <summary>
        /// Cuenta cuántas cartas del board son realmente visibles (Card1 a Card5).
        /// Las cartas son secuenciales: si Card3 no es visible, Card4/5 tampoco.
        /// Delega en ITableReaderService (Fase 3).
        /// </summary>
        private int CountVisibleBoardCards()
        {
            if (_formImage.pbImage.Image is Bitmap bmp)
                return _tableReaderService.CountVisibleBoardCards(bmp);
            return 0;
        }

        #region [Legacy Turn/River Handlers - REMOVED]
        // 10 handlers legacy eliminados (HandleOpenRaiseTurnAction, HandleCallTurnAction, etc.)
        // Reemplazados por PostflopDecisionService.DetermineAction()
        #endregion


        /// <summary>
        /// Determina la acción del river delegando a GameCoordinator.
        /// </summary>
        private async Task DetermineRiverAction()
        {
            var result = await _coordinator.DetermineRiverAction(_playerGameState);
            _responseAction.Action = result.Action;
            UpdateOverlayWithDecision(result.Decision);
            _logger.LogError("{LogText}", result.LogText);
        }

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
        private async Task ProcessFlopAsync()
        {
            _logger.LogInformation("ProcessFlopAsync: Iniciando procesamiento de flop - Estado actual: {State}", _gameLoopStateMachine.CurrentState);

            var regionsTableMap = _regionsTableMap;
            if (regionsTableMap is null)
            {
                _logger.LogWarning("ProcessFlopAsync: No hay mapa de regiones cargado, se omite la lectura del flop.");
                return;
            }

            // Capturar cartas del flop con retry
            List<BoardData> dataBoard = null!;
            for (int attempt = 0; attempt <= GameLoopStateMachine.MaxOcrRetries; attempt++)
            {
                using var bitmap = new Bitmap(_formImage.Screenshot);
                var flopResponse = await _getCardsFlopUseCase.ExecuteAsync(new GetCardsFlopUseCaseRequest
                {
                    Image = bitmap,
                    RegionsTableMap = regionsTableMap,
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

            // Verificar que hay suficientes cartas del flop tras reintentos
            if (dataBoard.Count(d => d.Position == BoardPosition.Flop) < 3)
            {
                _logger.LogError("No se detectaron suficientes cartas del flop tras reintentos.");
                _responseAction.Action = "Error: No se pudieron detectar cartas del flop";
                _frmOverlay.ClearAll();
                return;
            }

            // Determinar si estamos en posición
            _tableLayout.SetIsInPosition(_playerGameState);

            // La decisión viene del motor; el cliente ya no calcula equity localmente.
            var flopDecision = await DetermineFlopActionUnified();

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
            var flop = flopDecision.Decision;
            _gameLoggerService.LogStreetDecision(new StreetDecision(
                BoardPosition.Flop, flop.RawEquity ?? flop.Equity, flop.PotOdds, flop.EVWithFoldEquity ?? 0,
                flop.Action, _responseAction.Action ?? "Unknown", _playerGameState.PotSize,
                flopMaxBet, _playerGameState.HandSituation, _playerGameState.IsInPosition,
                Reason: flop.Reason, TotalOuts: flop.TotalOuts, SPR: flopSpr));
            _gameLoggerService.UpdateSituation(_playerGameState.HandSituation);
        }

        /// <summary>
        /// Determina la acción del flop delegando a GameCoordinator.
        /// </summary>
        private async Task<GameDecisionResult> DetermineFlopActionUnified()
        {
            var result = await _coordinator.DetermineFlopAction(_playerGameState);
            _responseAction.Action = result.Action;
            UpdateOverlayWithDecision(result.Decision);
            _logger.LogError("{LogText}", result.LogText);
            return result;
        }

        /// <summary>
        /// Determina si hero fue el agresor preflop basado en la HandSituation.
        /// </summary>
        // IsPreflopAggressor, HasRangeAdvantageOnBoard, CalculateCbetAdjustment
        // extraídos a PreflopAnalyzer en DecisionMaker

        /// <summary>
        /// Determina la acción del turn delegando a GameCoordinator.
        /// </summary>
        private async Task DetermineTurnAction()
        {
            var result = await _coordinator.DetermineTurnAction(_playerGameState);
            _responseAction.Action = result.Action;
            UpdateOverlayWithDecision(result.Decision);
            _logger.LogError("{LogText}", result.LogText);
        }

        /// <summary>
        /// <summary>
        /// Procesa la fase de turn
        /// </summary>
        private async Task ProcessTurnAsync()
        {
            _logger.LogInformation("ProcessTurnAsync: Iniciando procesamiento de turn - Estado actual: {State}", _gameLoopStateMachine.CurrentState);

            var regionsTableMap = _regionsTableMap;
            if (regionsTableMap is null)
            {
                _logger.LogWarning("ProcessTurnAsync: No hay mapa de regiones cargado, se omite la lectura del turn.");
                return;
            }

            // Capturar carta del turn con retry
            List<BoardData> dataBoard = null!;
            for (int attempt = 0; attempt <= GameLoopStateMachine.MaxOcrRetries; attempt++)
            {
                using var bitmap = new Bitmap(_formImage.Screenshot);
                var turnResponse = await _getCardsTurnUseCase.ExecuteAsync(new GetCardsTurnUseCaseRequest
                {
                    Image = bitmap,
                    RegionsTableMap = regionsTableMap,
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

            // Determinar si estamos en posición
            _tableLayout.SetIsInPosition(_playerGameState);

            // Determinar acción en el turn (la equity y la textura vienen del motor)
            await DetermineTurnAction();
        }

        /// <summary>
        /// Procesa la fase de river
        /// </summary>
        private async Task ProcessRiverAsync()
        {
            _logger.LogInformation("ProcessRiverAsync: Iniciando procesamiento de river - Estado actual: {State}", _gameLoopStateMachine.CurrentState);

            var regionsTableMap = _regionsTableMap;
            if (regionsTableMap is null)
            {
                _logger.LogWarning("ProcessRiverAsync: No hay mapa de regiones cargado, se omite la lectura del river.");
                return;
            }

            // Capturar carta del river con retry
            List<BoardData> dataBoard = null!;
            for (int attempt = 0; attempt <= GameLoopStateMachine.MaxOcrRetries; attempt++)
            {
                using var bitmap = new Bitmap(_formImage.Screenshot);
                var riverResponse = await _getCardsRiverUseCase.ExecuteAsync(new GetCardsRiverUseCaseRequest
                {
                    Image = bitmap,
                    RegionsTableMap = regionsTableMap,
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

            // Determinar si estamos en posición
            _tableLayout.SetIsInPosition(_playerGameState);

            // Determinar acción en el river (la equity y la textura vienen del motor)
            await DetermineRiverAction();
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
        /// Actualiza el overlay con la decisión que devolvió el motor.
        /// </summary>
        private void UpdateOverlayWithDecision(Contracts.DecisionResult decision)
        {
            var street = _gameLoopStateMachine.IsFlop ? "FLOP"
                : _gameLoopStateMachine.IsTurn ? "TURN"
                : _gameLoopStateMachine.IsRiver ? "RIVER"
                : "PRE-FLOP";

            _frmOverlay.UpdateWithDecision(decision, street);
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
        /// Establece las apuestas de los jugadores.
        /// Delega en ITableReaderService (Fase 3).
        /// </summary>
        private void SetBetPlayer()
        {
            if (_formImage.pbImage.Image is Bitmap bmp)
                _tableReaderService.SetBetPlayer(bmp, _playerGameState);
        }

        /// <summary>
        /// Establece el stack del héroe.
        /// Delega OCR en ITableReaderService, mantiene detección de auto-rebuy y UI update en FrmMain (Fase 3).
        /// </summary>
        private void SetHeroStack()
        {
            if (_formImage.pbImage.Image is not Bitmap bmp)
                return;

            var stackValue = _tableReaderService.SetHeroStack(bmp, _playerGameState);

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
        /// Establece el valor del bote.
        /// Delega en ITableReaderService (Fase 3).
        /// </summary>
        private void SetPotValue()
        {
            if (_formImage.pbImage.Image is Bitmap bmp)
                _tableReaderService.SetPotValue(bmp, _playerGameState);
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
                    _tableHand = _screenReader.ReadHandNumber(_formImage.Screenshot,
                        scaled.X, scaled.Y, scaled.Width, scaled.Height,
                        regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber);
                    _newHand = true;
                    _metrics?.StartHand(_tableHand);
                    _cycleCounter++;
                }
                else
                {
                    var currentHand = _screenReader.ReadHandNumber(_formImage.Screenshot,
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
                _tableName = _screenReader.ReadText(_formImage.Screenshot,
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
                    string rutaArchivoTexto = Path.Combine(directoryInfo.FullName, nombreArchivoTexto);

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
        /// Obtiene las cartas del jugador de forma asíncrona.
        /// Delega en ITableReaderService (Fase 3).
        /// </summary>
        private async Task ObtainCardsPlayerAsync()
        {
            if (_formImage.pbImage.Image is Bitmap bmp)
                await _tableReaderService.ObtainCardsPlayerAsync(bmp, _playerGameState);
        }

        /// <summary>
        /// Obtiene una imagen mientras se está jugando.
        /// Delega la captura en ICaptureService y solo maneja la presentación en UI.
        /// </summary>
        private async Task GetImageWhilePlaying()
        {
            try
            {
                // Crear directorio de sesión si no existe
                string baseFolder = Path.Combine(
                    "C:", "Code", "Poker", "ScrapePoker", "output", "samples", "games",
                    $"{DateTime.Now:yyyyMMdd}_Game");

                _folderPath = Path.Combine(baseFolder, _session);

                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }

                var capturedBitmap = _captureService.CaptureWindow(_handle);
                if (capturedBitmap == null)
                    return;

                // Ajuste de tamaño de formulario
                _formImage.Width = capturedBitmap.Width + _formImage.Width / 11;
                _formImage.Height = capturedBitmap.Height + _formImage.Height / 4;

                _formImage.pbImage.Width = capturedBitmap.Width;
                _formImage.pbImage.Height = capturedBitmap.Height;

                // Clonar imagen para evitar problemas de acceso
                _formImage.pbImage.Image = new Bitmap(capturedBitmap);
                _formImage.pbImage.Refresh();

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener imagen");
            }
        }

        /// <summary>
        /// Guarda las dimensiones de referencia en appsettings.json.
        /// Delega en ICaptureService.
        /// </summary>
        private void SaveReferenceDimensionsToConfig(int width, int height)
        {
            _captureService.SaveReferenceDimensions(width, height);
        }

        /// <summary>
        /// Escala las coordenadas de una región según el ratio entre la imagen actual y la imagen de referencia.
        /// Delega en ICaptureService.
        /// </summary>
        private (int X, int Y, int Width, int Height) GetScaledRegion(OpenScrape.Domain.ValueObjects.Region region)
        {
            var scaled = _captureService.GetScaledRegion(region);
            return (scaled.PosX, scaled.PosY, scaled.Width, scaled.Height);
        }

        // Método CalculateOverlayPosition movido a IOverlayPositioner
        // (extract-frmmain-testable-logic Fase 3).

        #endregion

        #region [UI Events]

        /// <summary>
        /// Suscribe FrmMain a los eventos del DetectionLoopService.
        /// Fase 1: usa this.Invoke() provisional hasta que UiUpdateService (Fase 5) absorba el marshaling.
        /// </summary>
        private void SubscribeDetectionEvents()
        {
            _detectionLoopService.HeroTurnDetected += (_, args) =>
            {
                if (IsDisposed) return;
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    try
                    {
                        _detectionLoggerService.LogTurnDetected(
                            new Point(0, 0),
                            args.ActionColor,
                            _gameLoopStateMachine.CurrentState.ToString());

                        // Guardar screenshot de debug cada 5 detecciones
                        if (_detectionLoopService.Detections % 5 == 0)
                        {
                            _detectionLoggerService.SaveDebugScreenshot(
                                _formImage.Screenshot,
                                new Point(0, 0),
                                args.ActionColor,
                                "turn_detected");
                        }

                        btnCapture_Click(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        _detectionLoggerService.LogDetectionError($"Error en HeroTurnDetected handler: {ex.Message}", ex);
                    }
                }));
            };

            _detectionLoopService.FlopDetected += (_, args) =>
            {
                if (IsDisposed) return;
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    try
                    {
                        _gameLoopStateMachine.TryTransition(GameState.FlopDetected, 3);
                        _detectionLoggerService.LogTurnDetected(
                            new Point(0, 0),
                            Color.Empty,
                            "FlopDetected");
                    }
                    catch (Exception ex)
                    {
                        _detectionLoggerService.LogDetectionError($"Error en FlopDetected handler: {ex.Message}", ex);
                    }
                }));
            };

            _detectionLoopService.IterationCompleted += (_, _) =>
            {
                if (IsDisposed) return;
                // Tracking de ventana: reemplaza btnWindow_Click(sender, e) en el loop original
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    try
                    {
                        if (_handle != IntPtr.Zero)
                        {
                            User32.RECT windowRect = new User32.RECT();
                            User32.GetWindowRect(_handle, ref windowRect);

                            bool windowMoved = windowRect.left != _locWindowRect.left ||
                                              windowRect.right != _locWindowRect.right ||
                                              windowRect.top != _locWindowRect.top ||
                                              windowRect.bottom != _locWindowRect.bottom;

                            if (windowMoved)
                            {
                                _locWindowRect = windowRect;
                                if (_frmOverlay != null)
                                {
                                    _frmOverlay.Location = _overlayPositioner.Calculate(
                                        windowRect.left, windowRect.right, windowRect.bottom,
                                        _frmOverlay.Size.Width);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error en IterationCompleted (tracking de ventana)");
                    }
                }));
            };

            _detectionLoopService.DetectionStopped += (_, _) =>
            {
                _backgroundExecute = false;
                _logger.LogInformation("DetectionLoopService detenido");
            };
        }

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
                    // Fase 4: Propagar el handle al CaptureService para HandProcessor
                    _captureService.WindowHandle = _handle;

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
                {
                    _detectionCts = new CancellationTokenSource();
                    _ = _detectionLoopService.StartAsync(
                        _handle,
                        () => _frmOverlay?.Visible ?? false,
                        _detectionCts.Token);
                    _backgroundExecute = true;
                }

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
        /// Guarda un screenshot raw de la ventana de poker en output/samples/grab/.
        /// Útil para recolectar capturas reales durante el juego y usarlas luego en modo test.
        /// </summary>
        private void btnGrabScreenshot_Click(object sender, EventArgs e)
        {
            try
            {
                if (_handle == IntPtr.Zero)
                {
                    _logger.LogWarning("[GRAB] No hay ventana seleccionada. Usá 'Window' primero.");
                    return;
                }

                using var bitmap = _captureService.CaptureWindow(_handle);
                if (bitmap == null)
                {
                    _logger.LogError("[GRAB] Falló la captura de la ventana");
                    return;
                }

                string grabDir = Path.Combine("C:", "Code", "Poker", "ScrapePoker", "output", "samples", "grab");
                Directory.CreateDirectory(grabDir);

                string fileName = $"game_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png";
                string filePath = Path.Combine(grabDir, fileName);

                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                _logger.LogInformation("[GRAB] Screenshot guardado: {Path}", filePath);

                // Feedback visual: mostrar el nombre del archivo sin pisar _tableHand
                lbTableHand.Text = $"GRAB: {fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GRAB] Error al guardar screenshot");
            }
        }

        // ============================================================================
        // CÓDIGO ELIMINADO — refactor-frmmain Fase 1: DetectionLoopService
        // ----------------------------------------------------------------------------
        // BackgroundWorker1_DoWork → movido a DetectionLoopService.DetectionLoop
        // PerformEnhancedDetection  → movido a DetectionLoopService.PerformEnhancedDetection
        // ReadPixelFromBuffer       → movido a DetectionLoopService.ReadPixelFromBuffer
        // DetectionResult           → movido a DetectionLoopService (record público)
        // ============================================================================

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
            var actionButtons = new[] { btnCapture, btnGrabScreenshot, btnWindow };
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

        // Pestañas Bankroll/Métricas delegadas a UserControls (Fase 7)

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
        // Inyección DI → se mantienen hasta Fase 7.5:
        //   _pokerCalculator, _betSizingService, _postflopDecisionService,
        //   _boardTextureAnalyzer, _opponentTracker → absorbidos por GameCoordinator
        //   _coordinator, _screenReader, _tableLayout → consumidos por
        //     GameLoopCoordinator y eliminados de FrmMain tras cutover
        //   Resto (*UseCase, *Service de infraestructura) → permanecen o se reasignan
        //     a coordinator según uso real, evaluado en Fase 6.
        // ============================================================================
    }
}
