using JasperFx.Core;
using Marten;
using Microsoft.Extensions.Options;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Helpers.FlopHelper;
using OpenScrape.App.Helpers.FlopHelper.RaiseOverLimper;
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
using Image = System.Drawing.Image;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace OpenScrape.App
{
    /// <summary>
    /// Formulario principal de la aplicación OpenScrape para análisis de mesas de póker
    /// </summary>
    public partial class FrmMain : Form, IDisposable
    {
        #region [Constants]
        private static readonly string DEFAULT_RESOURCES_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources");
        #endregion

        #region [Enums]
        internal enum TurnBoardTexture { Dry, Coordinated, Paired }
        internal enum RiverBoardTexture { Dry, Coordinated, Paired }
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
        private readonly List<int> _colorDealer = new() { 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255 };
        private readonly List<int> _colorEmpty = new() { 14, 15, 53, 59, 74 }; //, 41, 42, 43, 44, 45, 46, 47, 48, 49, 57, 66, 67, 68, 69 };
        private readonly List<int> _colorPlaying = new() { 17 };
        private Dictionary<TablePosition, Dictionary<TablePosition, decimal>> _preflopHeroPosition = new();
        private int _pictureUmbralBet = 130;
        private string _session = string.Empty;
        private IntPtr _handle;
        private User32.RECT _locWindowRect = new();
        private bool _executeCapture;
        // Street flags derivados del state machine
        private bool IsPreflop => _gameLoopStateMachine.IsPreflop;
        private bool IsFlop => _gameLoopStateMachine.IsFlop;
        private bool IsTurn => _gameLoopStateMachine.IsTurn;
        private bool IsRiver => _gameLoopStateMachine.IsRiver;
        private string _tableName = string.Empty;
        private long _newTableHand;
        private bool _newHand;
        private string _dealerPosition = "";
        private int _dealerValuePosition = -1;
        private string _previousDealerPlayerName = "";
        private string _previousSBPlayerName = "";
        private string _previousBBPlayerName = "";
        private BetSizeCategory GetOpponentBetSize(decimal maxBet, decimal potSize)
        {
            if (maxBet == 0)
                return BetSizeCategory.NoBet;
            if (maxBet <= potSize * 0.15m)
                return BetSizeCategory.Underbet;
            if (maxBet <= potSize * 0.3m)
                return BetSizeCategory.Small;
            if (maxBet <= potSize * 0.7m)
                return BetSizeCategory.Medium;
            return BetSizeCategory.Large;
        }

        /// <summary>
        /// Verifica si un valor de canal B coincide con algún expected value dentro de tolerancia.
        /// </summary>
        private static bool IsColorMatch(int actualB, IEnumerable<int> expectedValues, int tolerance = 5)
        {
            return expectedValues.Any(expected => Math.Abs(actualB - expected) <= tolerance);
        }

        /// <summary>
        /// Detecta villanos que foldearon mid-hand (color de playing desaparece).
        /// </summary>
        private void DetectFoldedPlayers()
        {
            // Detectar folds en cualquier estado de hand activa (preflop incluido)
            if (_gameLoopStateMachine.CurrentState == GameState.WaitingForHand ||
                _gameLoopStateMachine.CurrentState == GameState.HandComplete)
                return;

            var playingRegions = _regionsTableMap?.FirstOrDefault(x => x.Id == "Playing");
            if (playingRegions?.Regions == null || _formImage.pbImage.Image == null) return;

            using var bitmap = new Bitmap(_formImage.pbImage.Image);

            foreach (var player in _playerGameState.Players.Where(p => p.Active && !p.HasFolded && p.Name != "P0"))
            {
                var regionName = $"p{player.ValuePosition}playing";
                var region = playingRegions.Regions.FirstOrDefault(r => r.Name == regionName);
                if (region == null) continue;

                var scaled = GetScaledRegion(region);
                var color = bitmap.GetPixel(scaled.X, scaled.Y);

                if (!IsColorMatch(color.B, _colorPlaying))
                {
                    player.HasFolded = true;
                    player.Active = false;
                    LogDebug($"[FOLD] {player.Name} ({player.Alias ?? "?"}) foldeó mid-hand");
                }
            }
        }

        /// <summary>
        /// Re-evalúa Empty para jugadores no-hero en cada iteración del game loop.
        /// Detecta jugadores que se van mid-session.
        /// </summary>
        private void RefreshPlayerStates()
        {
            var emptyRegions = _regionsTableMap?.FirstOrDefault(x => x.Id == "Empty");
            if (emptyRegions?.Regions == null || _formImage.pbImage.Image == null) return;

            using var bitmap = new Bitmap(_formImage.pbImage.Image);

            foreach (var region in emptyRegions.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "empty");
                if (playerNumber == null || playerNumber == 0) continue;

                var player = _playerGameState.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
                if (player == null) continue;

                var scaled = GetScaledRegion(region);
                var color = bitmap.GetPixel(scaled.X, scaled.Y);

                bool wasEmpty = player.Empty;
                bool isNowEmpty = IsColorMatch(color.B, _colorEmpty);

                if (!wasEmpty && isNowEmpty)
                {
                    player.Empty = true;
                    player.Active = false;
                    player.SitOut = false;
                    LogDebug($"[LEFT] {player.Name} ({player.Alias ?? "?"}) dejó la mesa mid-session");
                }
            }
        }

        /// <summary>
        /// Validación cruzada: inferir Empty cuando múltiples señales coinciden.
        /// </summary>
        private void ValidatePlayerStates()
        {
            foreach (var player in _playerGameState.Players.Where(p => p.Name != "P0"))
            {
                // Sin datos → probablemente empty
                if (!player.Active && !player.Empty && !player.SitOut &&
                    string.IsNullOrEmpty(player.Alias) &&
                    player.Stack == 0 && player.Bet == 0)
                {
                    player.Empty = true;
                }

                // Active pero sin stack/bet → warning
                if (player.Active && player.Stack == 0 && player.Bet == 0 && !player.HasFolded)
                {
                    LogDebug($"[WARNING] {player.Name} activo pero stack=0, bet=0 — posible detección incorrecta");
                }
            }
        }

        /// <summary>
        /// Obtiene el identificador del villano activo: alias real > cache seat > seat name.
        /// </summary>
        private string GetActiveVillainId()
        {
            var villain = _playerGameState.Players
                .Where(p => p.Active && !string.IsNullOrEmpty(p.Name))
                .OrderByDescending(p => p.Bet)
                .FirstOrDefault();
            if (villain == null) return "Unknown";

            // Preferir alias real (nombre OCR) sobre seat name (P0, P1)
            if (!string.IsNullOrEmpty(villain.Alias))
                return villain.Alias;

            // Fallback: buscar alias cacheado por seat
            return _opponentTracker.ResolveName(villain.Name!) ?? villain.Name!;
        }

        /// <summary>
        /// Lee nombre de jugador con 2 lecturas + consenso + limpieza.
        /// </summary>
        private string ReadPlayerNameOCR(int x, int y, int w, int h, double umbral, double inactiveUmbral)
        {
            if (_formImage.pbImage.Image == null) return string.Empty;

            // Lectura 1: umbral estándar (0.80)
            string read1;
            using (var ocrResult1 = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image, x, y, w, h, umbral, false))
            {
                read1 = CleanOcrPlayerName(ocrResult1.Text);
            }

            // Lectura 2: umbral bajo (inactive) para capturar más colores
            string read2;
            using (var ocrResult2 = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image, x, y, w, h, inactiveUmbral, false))
            {
                read2 = CleanOcrPlayerName(ocrResult2.Text);
            }

            // Consenso: ambas iguales → seguro
            if (!string.IsNullOrEmpty(read1) && read1 == read2)
                return read1;

            // Si solo una tiene resultado → usarla
            if (string.IsNullOrEmpty(read1)) return read2;
            if (string.IsNullOrEmpty(read2)) return read1;

            // Ambas diferentes → la más larga (más probable correcta)
            return read1.Length >= read2.Length ? read1 : read2;
        }

        /// <summary>
        /// Limpia resultado OCR de nombre: trim, eliminar no alfanuméricos, min 2 chars.
        /// </summary>
        private static string CleanOcrPlayerName(string? rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return string.Empty;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(rawName.Trim(), @"[^a-zA-Z0-9_\- ]", "");
            cleaned = cleaned.Trim(' ', '_', '-');

            return cleaned.Length >= 2 ? cleaned : string.Empty;
        }

        /// <summary>
        /// Re-lee nombres de jugadores activos con alias vacío.
        /// </summary>
        private void RetryEmptyAliases()
        {
            var nameRegions = _regionsTableMap?.FirstOrDefault(x => x.Id == "Names");
            if (nameRegions?.Regions == null || _formImage.pbImage.Image == null) return;

            foreach (var player in _playerGameState.Players.Where(p => p.Active && string.IsNullOrEmpty(p.Alias)))
            {
                var regionName = $"p{player.ValuePosition}Name";
                var region = nameRegions.Regions.FirstOrDefault(r => r.Name == regionName);
                if (region == null) continue;

                var scaled = GetScaledRegion(region);
                double nameUmbral = Math.Min(region.Umbral ?? 0.80, 0.80);
                var cleanName = ReadPlayerNameOCR(scaled.X, scaled.Y, scaled.Width, scaled.Height,
                    nameUmbral, region.InactiveUmbral ?? 0.30);

                if (!string.IsNullOrEmpty(cleanName))
                {
                    player.Alias = cleanName;
                    if (!string.IsNullOrEmpty(player.Name))
                        _opponentTracker.RegisterSeatAlias(player.Name, cleanName);
                }
            }
        }

        /// <summary>
        /// Obtiene el tipo del villano activo para decisiones (Unknown si < 20 manos).
        /// </summary>
        private OpponentType GetVillainType()
        {
            var villainId = GetActiveVillainId();
            if (villainId == "Unknown") return OpponentType.Unknown;
            var profile = _opponentTracker.GetProfile(villainId);
            return profile.IsReliable ? profile.Type : OpponentType.Unknown;
        }

        /// <summary>
        /// Registra acciones de los villanos para tracking de oponente.
        /// </summary>
        private void TrackVillainPostflopAction(decimal maxBet, bool isPreflopAggressor)
        {
            var villainId = GetActiveVillainId();
            if (villainId == "Unknown") return;

            if (maxBet > 0)
            {
                _opponentTracker.RecordPostflopAction(villainId, PostflopAction.Bet);
                if (isPreflopAggressor)
                    _opponentTracker.RecordCBetOpportunity(villainId, didCBet: true);
            }
            else if (isPreflopAggressor)
            {
                _opponentTracker.RecordCBetOpportunity(villainId, didCBet: false);
            }
        }

        /// <summary>
        /// Obtiene el stack del villano principal (oponente activo con mayor stack).
        /// </summary>
        private decimal GetVillainStack()
        {
            var activeVillains = _playerGameState.Players
                .Where(p => p.Active && !string.IsNullOrEmpty(p.Name));
            return activeVillains.Any() ? activeVillains.Max(p => p.Stack) : 0;
        }

        private (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(decimal maxBet, bool isHeroInPosition, HandSituation currentSituation)
        {
            bool villainWasPreflopAggressor = _playerGameState.Players
                .Any(p => p.Active && p.WasPreflopAggressor);
            return PreflopAnalyzer.DetectDonkBet(maxBet, villainWasPreflopAggressor, currentSituation);
        }
        private bool _backgroundExecute;
        private IReadOnlyList<Table>? _tables;
        private List<Table>? _dataTables;
        private PokerCalculationResult _flopResult;
        private PokerCalculationResult _turnResult;
        private TurnBoardTexture _turnBoardTexture;
        private PokerCalculationResult _riverResult;
        private RiverBoardTexture _riverBoardTexture;
        #endregion

        #region [Services and UseCases]
        private readonly GetWindowsScreenUseCase _useCase = new();
        private readonly ActionScenarioUseCases _actionScenarioUseCases;
        private readonly RegionTableMapUseCases _regionTableMapUseCases;
        private readonly ISetPreflopActionUseCase _setPreflopActionUseCase;
        private readonly ImageCropperService _imageCropperService = new();
        private List<CardDTO>? _cardsImages;
        private readonly IDocumentStore _dataBase;
        // _sessionDB eliminado: se usan sesiones locales con using para evitar connection leak
        private readonly ISetFlopForceBoardUseCase _setFlopForceBoardUseCase = new SetFlopForceBoardUseCase();
        private static readonly IGetHashImageUseCase _getHashImageUseCase = new GetHashImageUseCase();
        private static readonly IGetCropImageUseCase _getCropImageUseCase = new GetCropImageUseCase();
        private readonly IGetCardsFlopUseCase _getCardsFlopUseCase;
        private readonly IGetCardsTurnUseCase _getCardsTurnUseCase;
        private readonly IGetCardsRiverUseCase _getCardsRiverUseCase;
        private readonly IOutsCalculatorUseCase _outsCalculatorUseCase = new OutsCalculatorUseCase();
        private readonly IPokerCalculator _pokerCalculator;
        private readonly BetSizingService _betSizingService;
        private readonly ColorDetectionService _colorDetectionService = new();
        private readonly OcrService _ocrService = new();
        private readonly CardUseCases _cardUseCases;
        private readonly GameLoggerService _gameLoggerService;
        private readonly DetectionLoggerService _detectionLoggerService;
        private readonly GameLoopStateMachine _gameLoopStateMachine;
        private readonly StrategyProfileService _strategyProfileService;
        private readonly PostflopDecisionService _postflopDecisionService;
        private readonly BoardTextureAnalyzer _boardTextureAnalyzer;
        private readonly OpponentTracker _opponentTracker;
        private readonly OverlayConfig _overlayConfig;
        private readonly PostflopGameContext _postflopContext = new();
        #endregion

        /// <summary>
        /// Constructor del formulario principal
        /// </summary>
        public FrmMain(IDocumentStore dataBase,
                        ActionScenarioUseCases actionScenarioUseCases,
                        CardUseCases cardUseCases,
                        RegionTableMapUseCases regionTableMapUseCases,
                        IPokerCalculator pokerCalculator,
                        BetSizingService betSizingService,
                        GameLoggerService gameLoggerService,
                        GameLoopStateMachine gameLoopStateMachine,
                        StrategyProfileService strategyProfileService,
                        PostflopDecisionService postflopDecisionService,
                        BoardTextureAnalyzer boardTextureAnalyzer,
                        OpponentTracker opponentTracker,
                        IOptions<OverlayConfig> overlayConfigOptions)
        {
            InitializeComponent();

            // NUEVO: Aplicar estilos visuales ANTES de la inicialización
            //InitializeVisualStyles();

            _dataBase = dataBase ?? throw new ArgumentNullException(nameof(dataBase));
            _actionScenarioUseCases = actionScenarioUseCases ?? throw new ArgumentNullException(nameof(actionScenarioUseCases));
            _regionTableMapUseCases = regionTableMapUseCases ?? throw new ArgumentNullException(nameof(regionTableMapUseCases));
            _cardUseCases = cardUseCases ?? throw new ArgumentNullException(nameof(cardUseCases));
            _pokerCalculator = pokerCalculator ?? throw new ArgumentNullException(nameof(pokerCalculator));
            _betSizingService = betSizingService ?? throw new ArgumentNullException(nameof(betSizingService));
            _gameLoggerService = gameLoggerService ?? throw new ArgumentNullException(nameof(gameLoggerService));
            _detectionLoggerService = new DetectionLoggerService();
            _gameLoopStateMachine = gameLoopStateMachine ?? throw new ArgumentNullException(nameof(gameLoopStateMachine));
            _strategyProfileService = strategyProfileService ?? throw new ArgumentNullException(nameof(strategyProfileService));
            _postflopDecisionService = postflopDecisionService ?? throw new ArgumentNullException(nameof(postflopDecisionService));
            _boardTextureAnalyzer = boardTextureAnalyzer ?? throw new ArgumentNullException(nameof(boardTextureAnalyzer));
            _opponentTracker = opponentTracker ?? throw new ArgumentNullException(nameof(opponentTracker));
            _overlayConfig = overlayConfigOptions?.Value ?? new OverlayConfig();

            // Resto de inicialización existente...
            _session = GenerateRandomNumbers();
            _lastChecked = new RadioButton();

            _setPreflopActionUseCase = new SetPreflopActionUseCase(_actionScenarioUseCases);
            _getCardsFlopUseCase = new GetCardsFlopUseCase(_dataBase);
            _getCardsTurnUseCase = new GetCardsTurnUseCase(_dataBase);
            _getCardsRiverUseCase = new GetCardsRiverUseCase(_dataBase);

            _pathResume = Path.Combine(DEFAULT_RESOURCES_PATH,
                $"resume_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt");

            FormClosing += FrmMain_FormClosing;

            InitializeHistorialTab();
        }

        /// <summary>
        /// Al cerrar la aplicación, persiste la última mano y sesión activas para no perder datos.
        /// </summary>
        private bool _isClosing;

        private async void FrmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_isClosing)
                return; // Ya estamos cerrando programáticamente

            if (_gameLoggerService.HasActiveHand || _gameLoggerService.HasActiveSession)
            {
                e.Cancel = true; // Cancelar el cierre para esperar al save
                _isClosing = true;

                try
                {
                    if (_gameLoggerService.HasActiveHand)
                        _gameLoggerService.EndHand(_playerGameState?.HeroStack ?? 0);

                    if (_gameLoggerService.HasActiveSession)
                        await _gameLoggerService.SaveSessionAsync();
                }
                catch (Exception ex)
                {
                    LogError($"Error al guardar datos al cerrar: {ex.Message}");
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
                    var prevHeroStack = _playerGameState?.HeroStack ?? 0;

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
                        LogInformation($"Guardando estado postflop antes de reset: {savedPostflopState}, BoardCards: {savedBoardCards?.Count ?? 0}");
                    }

                    _frmOverlay?.ClearAll();
                    _playerGameState = new PlayerGameState();
                    _responseAction = new ResponseAction();
                    _preflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
                    _dealerValuePosition = -1;
                    _dealerPosition = string.Empty;
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
                    if (savedPostflopState.HasValue)
                    {
                        LogInformation($"Restaurando estado postflop: {savedPostflopState}, BoardCards: {savedBoardCards?.Count ?? 0}");
                        _gameLoopStateMachine.ForceState(savedPostflopState.Value);
                        // Restaurar las cartas del board para que ProcessTurnAsync/ProcessRiverAsync
                        // puedan construir sobre las cartas previas
                        if (savedBoardCards != null && savedBoardCards.Count > 0)
                            _playerGameState.BoardCards = savedBoardCards;
                    }
                    else
                    {
                        // Solo resetear contexto postflop si NO se está restaurando un estado postflop
                        _postflopContext.Reset();
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

                LogInformation($"ProcessNewHand: Players.Count={_playerGameState.Players.Count()}, cbTest={cbTest.Checked}, isTestPostflop={isTestPostflop}");

                // Inicializar jugadores si:
                // 1. No hay jugadores, O
                // 2. Es modo Test y no es postflop, O
                // 3. El dealer no se ha detectado correctamente
                bool needsInitialization = _playerGameState.Players.Count() == 0 ||
                                          (cbTest.Checked && !isTestPostflop) ||
                                          _playerGameState.Position == TablePosition.None;

                if (needsInitialization)
                {
                    SetEmptyPlayer();
                    SetSitOutPlayer();
                    SetActivePlayer();
                    await InitializePlayersAsync();
                }
                else
                {
                    SetActivePlayer();
                    RefreshPlayerStates();

                    if (_playerGameState.Position == TablePosition.None)
                    {
                        SetDealerPlayer();
                        if (_dealerValuePosition >= 0 && _playerGameState.Position != TablePosition.None)
                            SetVillainPosition(_playerGameState.Position, _dealerValuePosition);
                    }
                }

                SetBetPlayer();
                SetHeroStack();
                RetryEmptyAliases();
                ValidatePlayerStates();

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
        /// Abre la ventana de debug de detección de turnos
        /// </summary>
        private void BtnDetectionDebug_Click(object sender, EventArgs e)
        {
            try
            {
                // Obtener la región uAction
                var regionAction = _regionsTableMap?.FirstOrDefault(f => f.Id == "User")?.Regions?.FirstOrDefault(x => x.Name == "uAction");

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
        /// Inicializa los datos de los jugadores
        /// </summary>
        private async Task InitializePlayersAsync()
        {
            try
            {
                // Las cartas y jugadores ya se obtienen antes
                SetDealerPlayer();

                // Log resultado de detección de dealer
                LogInformation($"Dealer result: P{_dealerValuePosition}, Position: {_playerGameState.Position}, IsDealer: {_playerGameState.IsDealer}, Players: {_playerGameState.Players.Count}");

                if (_dealerValuePosition >= 0)
                    SetVillainPosition(_playerGameState.Position, _dealerValuePosition);
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
                        LogError("HoleCards no detectadas después de reintentos, saltando procesamiento preflop");
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
                        LogError("HoleCards no detectadas para postflop, saltando procesamiento");
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
                LogError("Posición del jugador no detectada, saltando procesamiento preflop");
                return;
            }

            if (_preflopHeroPosition == null || !_preflopHeroPosition.ContainsKey(_playerGameState.Position))
            {
                LogError($"PreflopHeroPosition no tiene datos para posición {_playerGameState.Position}, reconstruyendo...");
                _preflopHeroPosition = GetPreflopHeroPosition();

                if (!_preflopHeroPosition.ContainsKey(_playerGameState.Position))
                {
                    LogError($"Sigue sin tener datos para posición {_playerGameState.Position}, saltando preflop");
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
                LogError($"[PREFLOP] Aggressors set: {string.Join(", ", playersWhoRaised.Select(p => $"{p.Name}({p.Position}):{p.Bet}"))}");
            }
        }

        /// <summary>
        /// Procesa las fases posteriores al flop (flop, turn, river)
        /// </summary>
        private async Task ProcessPostFlopAsync(PokerCalculationResult potOddsResult)
        {
            LogInformation($"ProcessPostFlopAsync: Estado actual = {_gameLoopStateMachine.CurrentState}");

            // Detectar villanos que foldearon mid-hand (actualiza Active/numOpponents)
            DetectFoldedPlayers();

            // Detectar transición a nueva calle verificando si hay carta visible en el board
            if (_gameLoopStateMachine.CurrentState == GameState.FlopAction)
            {
                if (IsBoardCardVisible("Card4"))
                    _gameLoopStateMachine.TryTransition(GameState.TurnDetected);
                else
                {
                    // Misma calle, reprocessar flop con info actualizada (pot y bets pueden haber cambiado)
                    SetPotValue();
                    // Actualizar bet size del villano antes de reprocessar (puede haber raise)
                    var reprocessMaxBet = _playerGameState.Players.Max(m => m.Bet);
                    _postflopContext.VillainBetSizeFlop = GetOpponentBetSize(reprocessMaxBet, _playerGameState.PotSize);
                    _postflopContext.VillainBetFlop = reprocessMaxBet > 0;
                    await ProcessFlopAsync(potOddsResult);
                }
            }

            if (_gameLoopStateMachine.CurrentState == GameState.TurnAction)
            {
                if (IsBoardCardVisible("Card5"))
                    _gameLoopStateMachine.TryTransition(GameState.RiverDetected);
                else
                {
                    // Misma calle, reprocessar turn con info actualizada (pot y bets pueden haber cambiado)
                    SetPotValue();
                    // Actualizar bet size del villano antes de reprocessar (puede haber raise)
                    var reprocessMaxBet = _playerGameState.Players.Max(m => m.Bet);
                    _postflopContext.VillainBetSizeTurn = GetOpponentBetSize(reprocessMaxBet, _playerGameState.PotSize);
                    _postflopContext.VillainBetTurn = reprocessMaxBet > 0;
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

            var boardRegion = _regionsTableMap?.FirstOrDefault(f => f.Id == "Board");
            var cardRegion = boardRegion?.Regions?.FirstOrDefault(r => r.Name == cardRegionName && r.IsHash == true);
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
            LogDebug($"IsBoardCardVisible({cardRegionName}): bestMatch={bestMatch:F1}%, visible={bestMatch > 80.0}");
            return bestMatch > 80.0;
        }

        #region [Legacy Turn/River Handlers - REMOVED]
        // 10 handlers legacy eliminados (HandleOpenRaiseTurnAction, HandleCallTurnAction, etc.)
        // Reemplazados por PostflopDecisionService.DetermineAction()
        #endregion


        private void DetermineRiverAction()
        {
            var equity = _riverResult.EquityPercentage;
            var inPosition = _playerGameState.IsInPosition;
            var maxBet = _playerGameState.Players.Max(m => m.Bet);
            var potSize = _playerGameState.PotSize;
            var betSize = GetOpponentBetSize(maxBet, potSize);
            var texture = _riverBoardTexture.ToString();
            bool villainAggro = maxBet > 0;

            var (isDonkBet, donkSituation) = DetectDonkBet(maxBet, inPosition, _playerGameState.HandSituation);
            var effectiveSituation = isDonkBet ? donkSituation : _playerGameState.HandSituation;

            // Analizar carta peligrosa: comparar turn (4 cartas) con river (5ª carta)
            var riverChange = AnalyzeBoardChange(_playerGameState.BoardCards, 4);
            // Combinar con peligro arrastrado del turn (flush/straight que sigue en board)
            var boardChange = PostflopGameContext.CombineBoardChanges(_postflopContext.LastBoardChange, riverChange);
            bool heroBlocks = boardChange.CompletedFlushSuit >= 0 &&
                (_playerGameState.HoleCard1Suit == boardChange.CompletedFlushSuit ||
                 _playerGameState.HoleCard2Suit == boardChange.CompletedFlushSuit);
            bool heroHasNutBlocker = heroBlocks &&
                ((_playerGameState.HoleCard1Suit == boardChange.CompletedFlushSuit && _playerGameState.HoleCard1Rank == 14) ||
                 (_playerGameState.HoleCard2Suit == boardChange.CompletedFlushSuit && _playerGameState.HoleCard2Rank == 14));

            bool isFacingBet = betSize != BetSizeCategory.NoBet;
            var dangerPenalty = _postflopDecisionService.CalculateDangerPenalty(equity, boardChange, heroBlocks, isFacingBet, BoardPosition.River, heroHasNutBlocker);

            var numOpponents = Math.Max(1, _playerGameState.Players.Count(p => p.Active) - 1);
            bool riverIsAggressor = PreflopAnalyzer.IsPreflopAggressor(effectiveSituation);
            var decision = _postflopDecisionService.DetermineAction(
                equity, BoardPosition.River, effectiveSituation, texture, inPosition,
                betSize,
                potOdds: _riverResult.PotOddsPercentage,
                totalOuts: _riverResult.TotalOuts,
                previousStreetBet: _postflopContext.PreviousStreetWasBet,
                villainShowedAggression: villainAggro,
                boardChange: boardChange,
                heroBlocksDangerSuit: heroBlocks,
                heroStack: _playerGameState.HeroStack,
                potSize: _playerGameState.PotSize,
                hasFlushDraw: _riverResult.DrawTypes.Contains("Flush Draw"),
                numOpponents: Math.Max(1, numOpponents),
                heroIsAggressor: riverIsAggressor,
                heroHandRank: _riverResult.HeroHandRank,
                hasComboDraw: _riverResult.HasComboDraw,
                villainAggressorCheckedPreviousStreet: !_postflopContext.VillainBetTurn && !riverIsAggressor,
                villainBarreling: _postflopContext.VillainBetTurn && maxBet > 0,
                pairClassification: _riverResult.PairType,
                foldEquity: _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(), _riverResult.FoldEquity),
                villainBetSizeTurn: _postflopContext.VillainBetSizeTurn,
                villainCheckedMiddleStreet: _postflopContext.VillainCheckedMiddleStreet,
                villainType: GetVillainType(),
                villainFoldToBetPct: _opponentTracker.GetFoldToBetPct(GetActiveVillainId()),
                heroKickerStrength: _riverResult.HeroKickerStrength);

            // Tracking postflop del villano en river
            if (maxBet > 0)
                _opponentTracker.RecordPostflopAction(GetActiveVillainId(), PostflopAction.Bet);

            double effectiveEquity = equity - dangerPenalty;
            double spr = potSize > 0 ? (double)(_playerGameState.HeroStack / potSize) : 0;
            var dangerFlags = new List<string>();
            if (boardChange.FlushCompleted) dangerFlags.Add("Flush completado");
            if (boardChange.StraightCompleted) dangerFlags.Add("Straight completado");
            if (boardChange.FlushDrawAppeared) dangerFlags.Add("Flush draw");
            if (boardChange.BoardPaired) dangerFlags.Add("Board paired");
            if (boardChange.OvercardAppeared) dangerFlags.Add("Overcard");
            if (_postflopContext.LastBoardChange.DangerLevel > 0) dangerFlags.Add("Arrastrado del turn");
            var dangerInfo = dangerFlags.Count > 0 ? string.Join(", ", dangerFlags) : "Ninguno";
            var draws = _riverResult.DrawTypes.Count > 0
                ? string.Join(", ", _riverResult.DrawTypes)
                : "Ninguno";
            LogError($"═══ [RIVER] ══════════════════════════════════════");
            LogError($"  {FormatCardsForLog(BoardPosition.River)}");
            LogError($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {_playerGameState.HeroStack:F0}  |  SPR: {spr:F1}");
            LogError($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {Math.Max(1, numOpponents)}");
            LogError($"  Equity: {equity:F1}%  |  Danger penalty: {dangerPenalty:F1}  |  Equity efectiva: {effectiveEquity:F1}%  |  Pot odds: {_riverResult.PotOddsPercentage:F1}%");
            LogError($"  Mano hero: {_riverResult.HeroHandRank}{(_riverResult.PairType != PairClassification.None ? $" ({_riverResult.PairType})" : "")}  |  Agresor preflop: {(riverIsAggressor ? "Sí" : "No")}  |  Hero blocks: {(heroBlocks ? "Sí" : "No")}");
            LogError($"  Board: {texture}  |  Peligro: {dangerInfo}  |  Outs: {_riverResult.TotalOuts}  |  Draws: {draws}");
            LogError($"  ▶ DECISIÓN: {decision.Action}  —  {decision.Reason}{(decision.IsCheckRaise ? "  [CHECK-RAISE]" : "")}{(decision.IsBluff ? "  [BLUFF]" : "")}{(decision.IsBarrel ? "  [BARREL]" : "")}");

            _responseAction.Action = decision.Action;
            _postflopContext.PreviousStreetWasBet = decision.Action.Contains("Bet") || decision.Action.Contains("Raise");

            // Persistir decisión y board en game logger
            double riverSpr = _playerGameState.PotSize > 0
                ? (double)(_playerGameState.HeroStack / _playerGameState.PotSize) : 0;
            _gameLoggerService.LogStreetDecision(new StreetDecision(
                BoardPosition.River, equity, _riverResult.PotOddsPercentage, _riverResult.ExpectedValue,
                _riverResult.RecommendedAction, decision.Action, _playerGameState.PotSize,
                maxBet, effectiveSituation, inPosition,
                Reason: decision.Reason, BoardTexture: texture, TotalOuts: _riverResult.TotalOuts, SPR: riverSpr));
            var riverCardName = _playerGameState.BoardCards
                .FirstOrDefault(b => b.Position == BoardPosition.River)?.Name;
            _gameLoggerService.UpdateBoard([], riverCard: riverCardName);
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
        {
            var communityCards = boardCards.Where(b => b.Position != BoardPosition.Hand).ToList();
            if (communityCards.Count <= previousCardCount)
                return BoardChangeResult.Safe;

            var previousRanks = communityCards.Take(previousCardCount).Select(c => c.Force).ToList();
            var previousSuits = communityCards.Take(previousCardCount).Select(c => c.Suit).ToList();
            var newCard = communityCards[previousCardCount];

            return _boardTextureAnalyzer.AnalyzeBoardChange(previousRanks, previousSuits, newCard.Force, newCard.Suit);
        }

        private TurnBoardTexture AnalyzeTurnBoardTexture(List<BoardData> boardCards)
        {
            // Analizar TODAS las cartas comunitarias (flop + turn), no solo la carta del turn
            var turnCards = boardCards.Where(b => b.Position != BoardPosition.Hand).ToList();
            if (turnCards.Count < 4) return TurnBoardTexture.Dry;

            var suits = turnCards.Select(b => b.Suit).ToList();
            var ranks = turnCards.Select(b => b.Force).OrderBy(r => r).ToList();

            // Check for pairs
            if (ranks.GroupBy(r => r).Any(g => g.Count() >= 2))
                return TurnBoardTexture.Paired;

            // Check for flush draws or straight draws
            bool hasFlushDraw = suits.GroupBy(s => s).Any(g => g.Count() >= 3);
            bool hasStraightDraw = ranks.Count >= 3 && ranks.Zip(ranks.Skip(1), (a, b) => b - a).Any(diff => diff <= 4);

            if (hasFlushDraw || hasStraightDraw)
                return TurnBoardTexture.Coordinated;

            return TurnBoardTexture.Dry;
        }

        /// <summary>
        /// Analiza la textura del board del river
        /// </summary>
        private RiverBoardTexture AnalyzeRiverBoardTexture(List<BoardData> boardCards)
        {
            var communityCards = boardCards.Where(b => b.Position != BoardPosition.Hand).ToList();
            if (communityCards.Count < 5) return RiverBoardTexture.Dry;

            var suits = communityCards.Select(b => b.Suit).ToList();
            var ranks = communityCards.Select(b => b.Force).OrderBy(r => r).ToList();

            // Check for pairs (three of a kind or full house)
            if (ranks.GroupBy(r => r).Any(g => g.Count() >= 3) || ranks.GroupBy(r => r).Count(g => g.Count() >= 2) >= 2)
                return RiverBoardTexture.Paired;

            // Check for flush or straight possibilities
            bool hasFlush = suits.GroupBy(s => s).Any(g => g.Count() >= 5);
            bool hasStraight = ranks.Count >= 5 && ranks.Zip(ranks.Skip(1), (a, b) => b - a).Any(diff => diff <= 4);

            if (hasFlush || hasStraight)
                return RiverBoardTexture.Coordinated;

            return RiverBoardTexture.Dry;
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
            bool indicator5 = !string.IsNullOrEmpty(currentDealerPlayerName) && currentDealerPlayerName != _previousDealerPlayerName;

            // Indicador 6: SB cambió
            string currentSBPlayerName = _playerGameState?.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "";
            bool indicator6 = !string.IsNullOrEmpty(currentSBPlayerName) && currentSBPlayerName != _previousSBPlayerName;

            // Indicador 7: BB cambió
            string currentBBPlayerName = _playerGameState?.Players.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "";
            bool indicator7 = !string.IsNullOrEmpty(currentBBPlayerName) && currentBBPlayerName != _previousBBPlayerName;

            // Log indicadores para debugging
            LogDebug($"DetectNewHand - HandChanged: {indicator1}, HoleCards: {indicator2}, PotLow: {indicator3}, BoardEmpty: {indicator4}, DealerChanged: {indicator5}, SBChanged: {indicator6}, BBChanged: {indicator7}");

            int secondaryCount = (indicator2 ? 1 : 0) + (indicator3 ? 1 : 0) + (indicator4 ? 1 : 0) + (indicator5 ? 1 : 0) + (indicator6 ? 1 : 0) + (indicator7 ? 1 : 0);

            // Ruta principal: hand number cambió + al menos 1 indicador secundario
            // Ruta fallback: hand number no disponible (OCR falló) + al menos 3 indicadores secundarios
            bool isNewHand = indicator1
                ? secondaryCount >= 1
                : secondaryCount >= 3;

            // Actualizar nombres previos si se detectó nueva mano
            if (isNewHand)
            {
                _previousDealerPlayerName = currentDealerPlayerName;
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
            LogInformation($"ProcessFlopAsync: Iniciando procesamiento de flop - Estado actual: {_gameLoopStateMachine.CurrentState}");

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
                LogInformation($"ProcessFlopAsync: Intento {attempt + 1} - Cartas detectadas: {dataBoard.Count}, Flop cards: {dataBoard.Count(d => d.Position == BoardPosition.Flop)}");

                if (dataBoard.Count(d => d.Position == BoardPosition.Flop) >= 3)
                    break;

                if (attempt < GameLoopStateMachine.MaxOcrRetries)
                {
                    LogError($"OCR flop: intento {attempt + 1} falló, reintentando...");
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
                LogError("No se detectaron suficientes cartas del flop tras reintentos.");
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
                handSituation: _playerGameState.HandSituation.ToString());

            _flopResult = result;
            UpdateOverlayWithPotOdds(result);

            // Determinar si estamos en posición
            SetIsInPosition();

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
        /// Determina la acción del flop usando PostflopDecisionService + C-bet awareness + Range Advantage.
        /// Ajusta equity efectiva según: agresor preflop, textura favorable al rango, posición, multiway.
        /// </summary>
        private void DetermineFlopActionUnified()
        {
            var rawEquity = _flopResult.EquityPercentage;
            var inPosition = _playerGameState.IsInPosition;
            var maxBet = _playerGameState.Players.Max(m => m.Bet);
            var potSize = _playerGameState.PotSize;
            var betSize = GetOpponentBetSize(maxBet, potSize);

            // Analizar textura del board con BoardTextureAnalyzer
            var flopCards = _playerGameState.BoardCards
                .Where(b => b.Position == BoardPosition.Flop)
                .ToList();
            var flopRanks = flopCards.Select(c => c.Force).ToList();
            var flopSuits = flopCards.Select(c => c.Suit).ToList();
            var boardTexture = _boardTextureAnalyzer.Analyze(flopRanks, flopSuits);
            var texture = boardTexture.SimplifiedTexture;

            bool villainAggro = maxBet > 0;
            var numOpponents = Math.Max(1, _playerGameState.Players.Count(p => p.Active) - 1);
            var boardChange = _boardTextureAnalyzer.AnalyzeInitialBoard(flopRanks, flopSuits);
            _postflopContext.InitialBoardDanger = boardChange;

            // Detectar donk bet en flop (villano apuesta sin ser agresor preflop)
            var (isDonkBet, donkSituation) = DetectDonkBet(maxBet, inPosition, _playerGameState.HandSituation);
            var effectiveSituation = isDonkBet ? donkSituation : _playerGameState.HandSituation;

            // C-bet awareness: ajustar equity según rol preflop y ventaja de rango
            bool isPreflopAggressor = PreflopAnalyzer.IsPreflopAggressor(_playerGameState.HandSituation);
            bool hasRangeAdvantage = PreflopAnalyzer.HasRangeAdvantageOnBoard(
                flopRanks, boardTexture, isPreflopAggressor, _playerGameState.HandSituation);
            double cbetAdjustment = PreflopAnalyzer.CalculateCbetAdjustment(
                isPreflopAggressor, hasRangeAdvantage, boardTexture, inPosition, numOpponents,
                _strategyProfileService.Profile);
            double effectiveEquity = Math.Min(99, rawEquity + cbetAdjustment);

            var decision = _postflopDecisionService.DetermineAction(
                effectiveEquity, BoardPosition.Flop, effectiveSituation, texture, inPosition,
                betSize,
                potOdds: _flopResult.PotOddsPercentage,
                totalOuts: _flopResult.TotalOuts,
                previousStreetBet: false,
                villainShowedAggression: villainAggro,
                boardChange: boardChange,
                heroBlocksDangerSuit: false,
                heroStack: _playerGameState.HeroStack,
                potSize: potSize,
                hasFlushDraw: _flopResult.DrawTypes.Contains("Flush Draw"),
                numOpponents: numOpponents,
                heroIsAggressor: isPreflopAggressor,
                heroHandRank: _flopResult.HeroHandRank,
                hasComboDraw: _flopResult.HasComboDraw,
                pairClassification: _flopResult.PairType,
                foldEquity: _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(), _flopResult.FoldEquity),
                villainType: GetVillainType(),
                villainFoldToBetPct: _opponentTracker.GetFoldToBetPct(GetActiveVillainId()),
                heroKickerStrength: _flopResult.HeroKickerStrength);

            // Tracking postflop del villano en flop
            TrackVillainPostflopAction(maxBet, isPreflopAggressor);

            double spr = potSize > 0 ? (double)(_playerGameState.HeroStack / potSize) : 0;
            var draws = _flopResult.DrawTypes.Count > 0
                ? string.Join(", ", _flopResult.DrawTypes)
                : "Ninguno";
            LogError($"═══ [FLOP] ═══════════════════════════════════════");
            LogError($"  {FormatCardsForLog(BoardPosition.Flop)}");
            LogError($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {_playerGameState.HeroStack:F0}  |  SPR: {spr:F1}");
            LogError($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {numOpponents}");
            LogError($"  Equity: {rawEquity:F1}%  |  C-bet adj: {cbetAdjustment:+0.0;-0.0}  |  Equity efectiva: {effectiveEquity:F1}%  |  Pot odds: {_flopResult.PotOddsPercentage:F1}%");
            LogError($"  Mano hero: {_flopResult.HeroHandRank}  |  Agresor preflop: {(isPreflopAggressor ? "Sí" : "No")}  |  Range advantage: {(hasRangeAdvantage ? "Sí" : "No")}");
            LogError($"  Board: {texture}  |  Outs: {_flopResult.TotalOuts}  |  Draws: {draws}");
            LogError($"  ▶ DECISIÓN: {decision.Action}  —  {decision.Reason}{(decision.IsCheckRaise ? "  [CHECK-RAISE]" : "")}{(decision.IsBluff ? "  [BLUFF]" : "")}");

            _responseAction.Action = decision.Action;
            _postflopContext.PreviousStreetWasBet = decision.Action.Contains("Bet") || decision.Action.Contains("Raise");
            _postflopContext.HeroBetFlop = _postflopContext.PreviousStreetWasBet;
            _postflopContext.VillainBetFlop = maxBet > 0;
            _postflopContext.VillainBetSizeFlop = betSize;
            _postflopContext.HeroFloatedFlop = decision.IsFloating;

            // Detectar si villano agresor preflop checkeó en flop (para probe bet en turn)
            _postflopContext.VillainAggressorCheckedFlop = !isPreflopAggressor && betSize == BetSizeCategory.NoBet;
        }

        /// <summary>
        /// Determina si hero fue el agresor preflop basado en la HandSituation.
        /// </summary>
        // IsPreflopAggressor, HasRangeAdvantageOnBoard, CalculateCbetAdjustment
        // extraídos a PreflopAnalyzer en DecisionMaker

        private void DetermineTurnAction()
        {
            var equity = _turnResult.EquityPercentage;
            var inPosition = _playerGameState.IsInPosition;
            var maxBet = _playerGameState.Players.Max(m => m.Bet);
            var potSize = _playerGameState.PotSize;
            var betSize = GetOpponentBetSize(maxBet, potSize);
            var texture = _turnBoardTexture.ToString();
            bool villainAggro = maxBet > 0;

            var (isDonkBet, donkSituation) = DetectDonkBet(maxBet, inPosition, _playerGameState.HandSituation);
            var effectiveSituation = isDonkBet ? donkSituation : _playerGameState.HandSituation;

            // Analizar carta peligrosa: comparar flop (3 cartas) con turn (4ª carta)
            // Combinar con peligro base del flop (flush draw, paired, connected)
            var turnChange = AnalyzeBoardChange(_playerGameState.BoardCards, 3);
            var boardChange = PostflopGameContext.CombineBoardChanges(
                _postflopContext.InitialBoardDanger, turnChange);
            bool heroBlocks = boardChange.CompletedFlushSuit >= 0 &&
                (_playerGameState.HoleCard1Suit == boardChange.CompletedFlushSuit ||
                 _playerGameState.HoleCard2Suit == boardChange.CompletedFlushSuit);
            bool heroHasNutBlocker = heroBlocks &&
                ((_playerGameState.HoleCard1Suit == boardChange.CompletedFlushSuit && _playerGameState.HoleCard1Rank == 14) ||
                 (_playerGameState.HoleCard2Suit == boardChange.CompletedFlushSuit && _playerGameState.HoleCard2Rank == 14));

            bool isFacingBet = betSize != BetSizeCategory.NoBet;
            var dangerPenalty = _postflopDecisionService.CalculateDangerPenalty(equity, boardChange, heroBlocks, isFacingBet, BoardPosition.Turn, heroHasNutBlocker);

            _postflopContext.LastBoardChange = boardChange;

            var numOpponents = Math.Max(1, _playerGameState.Players.Count(p => p.Active) - 1);
            bool turnIsAggressor = PreflopAnalyzer.IsPreflopAggressor(effectiveSituation);
            var decision = _postflopDecisionService.DetermineAction(
                equity, BoardPosition.Turn, effectiveSituation, texture, inPosition,
                betSize,
                potOdds: _turnResult.PotOddsPercentage,
                totalOuts: _turnResult.TotalOuts,
                previousStreetBet: _postflopContext.PreviousStreetWasBet,
                villainShowedAggression: villainAggro,
                boardChange: boardChange,
                heroBlocksDangerSuit: heroBlocks,
                heroStack: _playerGameState.HeroStack,
                potSize: _playerGameState.PotSize,
                hasFlushDraw: _turnResult.DrawTypes.Contains("Flush Draw"),
                numOpponents: Math.Max(1, numOpponents),
                heroIsAggressor: turnIsAggressor,
                heroHandRank: _turnResult.HeroHandRank,
                hasComboDraw: _turnResult.HasComboDraw,
                villainAggressorCheckedPreviousStreet: _postflopContext.VillainAggressorCheckedFlop,
                villainBarreling: _postflopContext.VillainBetFlop && maxBet > 0,
                pairClassification: _turnResult.PairType,
                foldEquity: _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(), _turnResult.FoldEquity),
                villainBetSizeFlop: _postflopContext.VillainBetSizeFlop,
                villainType: GetVillainType(),
                heroFloatedFlop: _postflopContext.HeroFloatedFlop,
                villainFoldToBetPct: _opponentTracker.GetFoldToBetPct(GetActiveVillainId()),
                heroKickerStrength: _turnResult.HeroKickerStrength);

            // Tracking postflop del villano en turn
            TrackVillainPostflopAction(maxBet, turnIsAggressor);

            double effectiveEquity = equity - dangerPenalty;
            double spr = potSize > 0 ? (double)(_playerGameState.HeroStack / potSize) : 0;
            var dangerFlags = new List<string>();
            if (boardChange.FlushCompleted) dangerFlags.Add("Flush completado");
            if (boardChange.StraightCompleted) dangerFlags.Add("Straight completado");
            if (boardChange.FlushDrawAppeared) dangerFlags.Add("Flush draw");
            if (boardChange.BoardPaired) dangerFlags.Add("Board paired");
            if (boardChange.OvercardAppeared) dangerFlags.Add("Overcard");
            var dangerInfo = dangerFlags.Count > 0 ? string.Join(", ", dangerFlags) : "Ninguno";
            var draws = _turnResult.DrawTypes.Count > 0
                ? string.Join(", ", _turnResult.DrawTypes)
                : "Ninguno";
            LogError($"═══ [TURN] ═══════════════════════════════════════");
            LogError($"  {FormatCardsForLog(BoardPosition.Turn)}");
            LogError($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {_playerGameState.HeroStack:F0}  |  SPR: {spr:F1}");
            LogError($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {Math.Max(1, numOpponents)}");
            LogError($"  Equity: {equity:F1}%  |  Danger penalty: {dangerPenalty:F1}  |  Equity efectiva: {effectiveEquity:F1}%  |  Pot odds: {_turnResult.PotOddsPercentage:F1}%");
            LogError($"  Mano hero: {_turnResult.HeroHandRank}{(_turnResult.PairType != PairClassification.None ? $" ({_turnResult.PairType})" : "")}  |  Agresor preflop: {(turnIsAggressor ? "Sí" : "No")}  |  Hero blocks: {(heroBlocks ? "Sí" : "No")}");
            LogError($"  Board: {texture}  |  Peligro: {dangerInfo}  |  Outs: {_turnResult.TotalOuts}  |  Draws: {draws}");
            LogError($"  ▶ DECISIÓN: {decision.Action}  —  {decision.Reason}{(decision.IsCheckRaise ? "  [CHECK-RAISE]" : "")}{(decision.IsBluff ? "  [BLUFF]" : "")}");

            _responseAction.Action = decision.Action;
            _postflopContext.PreviousStreetWasBet = decision.Action.Contains("Bet") || decision.Action.Contains("Raise");
            _postflopContext.HeroBetTurn = _postflopContext.PreviousStreetWasBet;
            _postflopContext.VillainBetTurn = maxBet > 0;
            _postflopContext.VillainBetSizeTurn = betSize;

            // Persistir decisión y board en game logger
            double turnSpr = _playerGameState.PotSize > 0
                ? (double)(_playerGameState.HeroStack / _playerGameState.PotSize) : 0;
            _gameLoggerService.LogStreetDecision(new StreetDecision(
                BoardPosition.Turn, equity, _turnResult.PotOddsPercentage, _turnResult.ExpectedValue,
                _turnResult.RecommendedAction, decision.Action, _playerGameState.PotSize,
                maxBet, effectiveSituation, inPosition,
                Reason: decision.Reason, BoardTexture: texture, TotalOuts: _turnResult.TotalOuts, SPR: turnSpr));
            var turnCardName = _playerGameState.BoardCards
                .FirstOrDefault(b => b.Position == BoardPosition.Turn)?.Name;
            _gameLoggerService.UpdateBoard([], turnCard: turnCardName);
            _gameLoggerService.UpdateSituation(effectiveSituation);
        }

        /// <summary>
        /// Valida las asignaciones de posiciones para asegurar consistencia
        /// </summary>
        /// <param name="players">Lista de jugadores activos</param>
        private void ValidatePositionAssignments(List<Player> players)
        {
            // Validar exactamente un dealer
            var dealers = players.Where(p => p.Dealer).ToList();
            if (dealers.Count != 1)
            {
                LogInformation($"Advertencia: Se encontraron {dealers.Count} dealers. Debe haber exactamente 1.");
            }

            // Validar posiciones únicas (excepto None)
            var assignedPositions = players.Where(p => p.Position != TablePosition.None)
                                           .GroupBy(p => p.Position)
                                           .Where(g => g.Count() > 1)
                                           .Select(g => g.Key)
                                           .ToList();
            if (assignedPositions.Any())
            {
                LogInformation($"Advertencia: Posiciones duplicadas: {string.Join(", ", assignedPositions)}");
            }

            // Validar blinds si hay suficientes jugadores
            if (players.Count >= 2)
            {
                var hasSmallBlind = players.Any(p => p.Position == TablePosition.SmallBlind);
                var hasBigBlind = players.Any(p => p.Position == TablePosition.BigBlind);
                if (!hasSmallBlind || !hasBigBlind)
                {
                    LogDebug("Advertencia: Faltan asignar SmallBlind o BigBlind.");
                }
            }

            // Validar Button si hay suficientes jugadores
            if (players.Count >= 3)
            {
                var hasButton = players.Any(p => p.Position == TablePosition.Button);
                if (!hasButton)
                {
                    LogDebug("Advertencia: Falta asignar Button.");
                }
            }
        }

        /// <summary>
        /// Procesa la fase de turn
        /// </summary>
        private async Task ProcessTurnAsync()
        {
            LogInformation($"ProcessTurnAsync: Iniciando procesamiento de turn - Estado actual: {_gameLoopStateMachine.CurrentState}");

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
                LogInformation($"ProcessTurnAsync: Intento {attempt + 1} - Cartas totales: {dataBoard.Count}");

                if (dataBoard.Count >= 4)
                    break;

                if (attempt < GameLoopStateMachine.MaxOcrRetries)
                {
                    LogError($"OCR turn: intento {attempt + 1} falló ({dataBoard.Count} cartas), reintentando...");
                    await Task.Delay(200);
                }
            }

            if (dataBoard.Count < 4)
            {
                LogError("No se detectó la carta del turn tras reintentos.");
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
                handSituation: _playerGameState.HandSituation.ToString());

            _turnResult = result;

            UpdateOverlayWithPotOdds(result);

            // Determinar si estamos en posición
            SetIsInPosition();

            // Determinar acción en el turn
            DetermineTurnAction();
        }

        /// <summary>
        /// Procesa la fase de river
        /// </summary>
        private async Task ProcessRiverAsync()
        {
            LogInformation($"ProcessRiverAsync: Iniciando procesamiento de river - Estado actual: {_gameLoopStateMachine.CurrentState}");

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
                LogInformation($"ProcessRiverAsync: Intento {attempt + 1} - Cartas totales: {dataBoard.Count}");

                if (dataBoard.Count >= 5)
                    break;

                if (attempt < GameLoopStateMachine.MaxOcrRetries)
                {
                    LogError($"OCR river: intento {attempt + 1} falló ({dataBoard.Count} cartas), reintentando...");
                    await Task.Delay(200);
                }
            }

            if (dataBoard.Count < 5)
            {
                LogError("No se detectó la carta del river tras reintentos.");
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
                handSituation: _playerGameState.HandSituation.ToString());

            _riverResult = result;

            UpdateOverlayWithPotOdds(result);

            // Analizar textura del board del river
            _riverBoardTexture = AnalyzeRiverBoardTexture(dataBoard);

            // Determinar si estamos en posición
            SetIsInPosition();

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
                _opponentTracker.RecordHandPlayed(player.Name!);
                // Si villain puso dinero preflop → VPIP
                if (player.Bet > 0)
                    _opponentTracker.RecordVPIP(player.Name!);
                // Si villain hizo raise (bet significativa) → PFR
                if (player.Bet > 0 && _playerGameState.PotSize > 0 &&
                    player.Bet > _playerGameState.PotSize * 0.3m)
                    _opponentTracker.RecordPFR(player.Name!);
            }

            // _postflopContext.Reset() se hace condicionalmente en btnCapture_Click
            // para no perder el contexto cuando se restaura un estado postflop guardado
            LogError($"Nueva mano detectada: Hand {_tableHand}, Pot: {prevPot}, HoleCards: {prevHoleCards}");

            // Asegurar que hay sesión activa e iniciar nueva mano
            if (!_gameLoggerService.HasActiveSession)
                await _gameLoggerService.StartSessionAsync(_session, _tableName);

            if (long.TryParse(_tableHand, out var handNum))
            {
                var activePlayers = _playerGameState?.Players?.Count(p => !p.Empty) ?? 0;
                await _gameLoggerService.StartNewHandAsync(
                    handNum,
                    prevHoleCard1,
                    prevHoleCard2,
                    prevPosition,
                    prevHeroStack,
                    activePlayers);
            }

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
                _frmOverlay.UpdateAction(_responseAction?.Action ?? string.Empty);

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
                    var numOpp = Math.Max(1, _playerGameState.Players.Count(p => p.Active) - 1);
                    var preflopResult = _pokerCalculator.Calculate(
                        heroCards, new List<CardDataOuts>(),
                        _playerGameState.PotSize,
                        _playerGameState.Players.Max(m => m.Bet),
                        numOpponents: numOpp,
                        heroStack: _playerGameState.HeroStack,
                        villainStack: GetVillainStack(),
                        handSituation: _playerGameState.HandSituation.ToString());
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
            sb.AppendLine($"▶ DECISIÓN: {heroAction}");

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
            var regionTableMap = _regionsTableMap?.FirstOrDefault(f => f.Id == "Bets");

            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "bet");
                if (playerNumber == null) continue;

                var scaled = GetScaledRegion(region);
                var betValue = SetBetValue(scaled.X, scaled.Y, scaled.Width, scaled.Height,
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
        /// Establece el stack del héroe
        /// </summary>
        private void SetHeroStack()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(f => f.Id == "User");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            var region = regionTableMap.Regions.FirstOrDefault(r => r.Name == "uStack");
            if (region == null)
                return;

            // Limpiar cache OCR para evitar colisiones dHash entre valores similares (ej: 97 vs 92)
            _ocrService.ClearCache();

            // Intentar leer el stack con retry
            decimal stackValue = 0;
            int maxRetries = 2;

            var scaled = GetScaledRegion(region);

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var rawValue = SetStackValue(scaled.X, scaled.Y, scaled.Width, scaled.Height,
                    region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);

                stackValue = NormalizeStackValue(rawValue);

                if (stackValue > 0)
                    break;

                if (attempt < maxRetries)
                    Thread.Sleep(100);
            }

            // Si no se obtuvo valor válido, mantener el anterior
            if (stackValue <= 0 && _playerGameState.HeroStack > 0)
            {
                LogError($"[STACK] OCR falló tras reintentos, manteniendo valor anterior: {_playerGameState.HeroStack}");
                lbUserStack.Text = _playerGameState.HeroStack.ToString();
                return;
            }

            _playerGameState.HeroStack = stackValue;
            lbUserStack.Text = stackValue.ToString();
        }

        /// <summary>
        /// Normaliza el valor raw del OCR a un decimal válido de stack.
        /// Maneja artefactos comunes del OCR:
        /// - Prefijo "8" espurio con separador decimal (ej: "812,50" → "12,50")
        /// - Separador decimal perdido (ej: 9950 → 99.50) cuando el valor supera el umbral razonable
        /// Los stacks en BB raramente superan 300, así que valores > 500 sin decimal indican separador perdido.
        /// </summary>
        private decimal NormalizeStackValue(decimal rawValue)
        {
            if (rawValue <= 0)
                return 0;

            var rawStr = rawValue.ToString();
            bool hasDecimalSeparator = rawStr.Contains(',') || rawStr.Contains('.');

            // Solo corregir artefacto "8" cuando ya tiene separador decimal
            if (hasDecimalSeparator)
            {
                var separator = rawStr.Contains(',') ? ',' : '.';
                var parts = rawStr.Split(separator);

                // Artefacto OCR: "8" espurio al inicio (ej: "812,50" → "12,50")
                if (parts[0].Length > 2 && parts[0][0] == '8')
                {
                    var corrected = parts[0][1..] + separator + parts[1];
                    if (decimal.TryParse(corrected, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.CurrentCulture, out var correctedValue))
                    {
                        LogDebug($"[STACK] OCR artefacto '8' corregido: {rawStr} → {corrected}");
                        return correctedValue;
                    }
                }
            }

            // Separador decimal perdido: el cliente siempre muestra 2 decimales,
            // si el OCR pierde el punto/coma el valor se infla ~100x (ej: 99.50 → 9950)
            if (!hasDecimalSeparator && rawValue >= 500 && rawStr.Length >= 4)
            {
                var corrected = rawStr[..^2] + "," + rawStr[^2..];
                if (decimal.TryParse(corrected, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out var correctedValue))
                {
                    LogDebug($"[STACK] OCR separador decimal perdido corregido: {rawStr} → {corrected}");
                    return correctedValue;
                }
            }

            // Sin separador decimal y valor razonable → valor entero, devolver tal cual
            return rawValue;
        }

        /// <summary>
        /// Limpia el texto OCR dejando solo caracteres numéricos válidos (dígitos, coma, punto)
        /// </summary>
        private static string CleanOcrNumericText(string? ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return "0";

            // Eliminar espacios, letras y caracteres no numéricos excepto separadores decimales
            var cleaned = new string(ocrText.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());

            return string.IsNullOrEmpty(cleaned) ? "0" : cleaned;
        }

        /// <summary>
        /// Establece los jugadores vacíos
        /// </summary>
        private void SetEmptyPlayer()
        {
            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Empty");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            // P0 (héroe) siempre está activo — no tiene región Empty/Playing en Regiones.json
            if (!_playerGameState.Players.Any(p => p.ValuePosition == 0))
            {
                var heroPlayer = CreatePlayerData(0);
                heroPlayer.Active = true;
                _playerGameState.Players.Add(heroPlayer);
            }

            using var bitmap = new Bitmap(_formImage.pbImage.Image);

            foreach (var region in regionTableMap.Regions)
            {
                var playerNumber = GetPlayerNumber(region.Name, "empty");
                if (playerNumber == null)
                    continue;

                var scaled = GetScaledRegion(region);
                var color = bitmap.GetPixel(scaled.X, scaled.Y);
                var colorMatch = IsColorMatch(color.B, _colorEmpty);

                _playerGameState.Players.Add(CreatePlayerData(playerNumber.Value));

                // Verificamos si el jugador está vacío (Empty ≠ SitOut, son estados independientes)
                if (region.Name.Contains("empty") && colorMatch)
                {
                    var player = _playerGameState.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");
                    if (player != null)
                    {
                        player.Empty = true;
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

                var scaled = GetScaledRegion(region);
                var color = bitmap.GetPixel(scaled.X, scaled.Y);

                //_playerGameState.Players.Add(CreatePlayerData(playerNumber.Value));

                // Verificamos si el jugador está vacío
                var player = _playerGameState.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");
                if (region.Name.Contains("playing") && IsColorMatch(color.B, _colorPlaying))
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
                    var scaled = GetScaledRegion(region);
                    double nameUmbral = Math.Min(region.Umbral ?? 0.80, 0.80);
                    var cleanName = ReadPlayerNameOCR(scaled.X, scaled.Y, scaled.Width, scaled.Height,
                        nameUmbral, region.InactiveUmbral ?? 0.30);
                    player.Alias = cleanName;

                    if (!string.IsNullOrEmpty(cleanName) && !string.IsNullOrEmpty(player.Name))
                        _opponentTracker.RegisterSeatAlias(player.Name, cleanName);
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
                var scaled = GetScaledRegion(regionPot);
                var pot = SetTextOCR(scaled.X, scaled.Y, scaled.Width, scaled.Height,
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

            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Table");
            if (regionTableMap == null)
                return;

            var regionTableHand = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "tablehand");
            if (regionTableHand != null)
            {
                var scaled = GetScaledRegion(regionTableHand);
                if (string.IsNullOrEmpty(_tableHand))
                {
                    _tableHand = SetHandNumberOCR(scaled.X, scaled.Y, scaled.Width, scaled.Height,
                        regionTableHand.Umbral, regionTableHand.InactiveUmbral, regionTableHand.IsOnlyNumber);
                    _newHand = true;
                }
                else
                {
                    var currentHand = SetHandNumberOCR(scaled.X, scaled.Y, scaled.Width, scaled.Height,
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
                        LogDebug($"[HAND#] Parsing fallido: prev='{_tableHand}', current='{currentHand}' — comparando como texto");
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
                            LogInformation($"[HAND#] Cambio sospechoso de OCR en postflop: prev='{_tableHand}', current='{currentHand}' — requiriendo más indicadores");
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
                        LogDebug("[HAND#] OCR falló completamente, evaluando indicadores secundarios");
                        _newHand = DetectNewHand(false, currentHand);
                        if (_newHand)
                        {
                            _newTableHand++;
                            _tableHand = _newTableHand.ToString();
                        }
                    }
                }
            }

            var regionTableName = regionTableMap.Regions?.FirstOrDefault(f => f.Name == "tablename");
            if (regionTableName != null && string.IsNullOrEmpty(_tableName))
            {
                var scaledName = GetScaledRegion(regionTableName);
                _tableName = SetTextOCR(scaledName.X, scaledName.Y, scaledName.Width, scaledName.Height,
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
            // Si no hay jugadores, no podemos determinar el dealer
            if (_playerGameState.Players.Count == 0)
                return;

            var regionTableMap = _regionsTableMap?.FirstOrDefault(x => x.Id == "Dealer");
            if (regionTableMap == null || regionTableMap.Regions == null || _formImage.pbImage.Image == null)
                return;

            // Clear all previous dealer flags to ensure only one dealer per hand
            _playerGameState.Players.ForEach(p => p.Dealer = false);

            using var bitmap = new Bitmap(_formImage.pbImage.Image);

            var emptyPositions = _playerGameState.Players
                .Where(w => w.Empty || w.SitOut)
                .Select(s => s.ValuePosition)
                .ToList();

            // Log de todas las regiones para diagnóstico
            var allColorsLog = new System.Text.StringBuilder();
            int? detectedDealerPosition = null;

            foreach (var region in regionTableMap.Regions.Where(x => x.IsColor.GetValueOrDefault()))
            {
                var scaled = GetScaledRegion(region);
                var centerColor = bitmap.GetPixel(scaled.X, scaled.Y);
                allColorsLog.Append($"{region.Name}=RGB({centerColor.R},{centerColor.G},{centerColor.B}) ");

                // Detección robusta: color dorado/amarillo del dealer button
                // #ffd800 = R:255, G:216, B:0 — verificar los 3 canales
                bool isDealerColor = IsDealerButtonColor(bitmap, scaled.X, scaled.Y, searchRadius: 3);

                if (isDealerColor)
                {
                    var playerNumber = GetPlayerNumber(region.Name, "dealer");
                    if (playerNumber != null && detectedDealerPosition == null)
                    {
                        detectedDealerPosition = playerNumber.Value;
                    }
                }
            }

            LogInformation($"Dealer scan: {allColorsLog}| Detectado: {(detectedDealerPosition.HasValue ? $"P{detectedDealerPosition}" : "NINGUNO")} | Imagen: {bitmap.Width}x{bitmap.Height}");

            if (detectedDealerPosition.HasValue)
            {
                SetDealerForPlayer(detectedDealerPosition.Value, emptyPositions);
            }
        }

        /// <summary>
        /// Verifica si el pixel y su entorno corresponden al color del dealer button (dorado/amarillo)
        /// </summary>
        private bool IsDealerButtonColor(Bitmap bitmap, int centerX, int centerY, int searchRadius)
        {
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int px = centerX + dx;
                    int py = centerY + dy;

                    if (px < 0 || py < 0 || px >= bitmap.Width || py >= bitmap.Height)
                        continue;

                    var c = bitmap.GetPixel(px, py);

                    // Dealer button dorado: R alto (>=200), G medio-alto (>=140), B bajo (<=80)
                    // Esto excluye blancos (B alto), grises, fondos oscuros, etc.
                    if (c.R >= 200 && c.G >= 140 && c.B <= 80)
                        return true;
                }
            }

            return false;
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

            // Para P0 (caso especial: héroe es el dealer)
            if (playerNumber == 0)
            {
                _playerGameState.IsDealer = true;
                _playerGameState.Position = TablePosition.Button;
                _dealerValuePosition = 0;

                // Establecer la posición del jugador P0 (héroe)
                var heroP0 = _playerGameState.Players.FirstOrDefault(p => p.ValuePosition == 0);
                if (heroP0 != null)
                {
                    heroP0.Position = TablePosition.Button;
                    heroP0.Dealer = true;
                }

                // Asignar posiciones a villanos
                SetVillainPosition(TablePosition.Button, 0);
                return;
            }

            // Marcar dealer en el jugador si existe en la lista
            var player = _playerGameState.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");

            if (player != null)
            {
                // Skip dealer assignment if the seat is truly empty (no active, no playing)
                // Un jugador puede estar marcado Empty por detección de color pero Active por Playing
                if ((player.Empty || player.SitOut) && !player.Active)
                    return;

                player.Dealer = true;
            }

            LogDebug($"Dealer assigned to player P{playerNumber}");

            // Determinar posición P0 basado en la posición del dealer y asientos vacíos
            var p0Pos = DetermineP0Position(playerNumber, emptyPositions);
            LogDebug($"DetermineP0Position resultado: {p0Pos}, dealer: {playerNumber}, emptyPositions: [{string.Join(",", emptyPositions)}]");
            _playerGameState.Position = p0Pos;

            // Establecer la posición del jugador P0 (héroe)
            var heroPlayer = _playerGameState.Players.FirstOrDefault(p => p.ValuePosition == 0);
            if (heroPlayer != null)
            {
                heroPlayer.Position = p0Pos;
                LogDebug($"Héroe P0 position establecida: {p0Pos}");
            }

            _previousDealerPlayerName = _dealerPosition;
            _dealerPosition = player?.Name ?? $"P{playerNumber}";
            _dealerValuePosition = playerNumber;

            // Asignar posiciones a villanos usando la posición del dealer
            SetVillainPosition(p0Pos, playerNumber);
        }

        /// <summary>
        /// Determina la posición de P0 basado en la posición del dealer y asientos vacíos
        /// </summary>
        /// <param name="dealerPosition">Posición del dealer</param>
        /// <param name="emptyPositions">Lista de posiciones vacías</param>
        /// <returns>Posición de la mesa para P0</returns>
        private TablePosition DetermineP0Position(int dealerPosition, List<int> emptyPositions)
        {
            var position = PositionCalculator.DetermineP0Position(dealerPosition, _playerGameState.Players);

            var activeSeats = _playerGameState.Players
                .Where(p => !p.Empty && !p.SitOut)
                .Select(p => p.ValuePosition)
                .OrderBy(s => s)
                .ToList();

            LogDebug($"Posición del héroe calculada: {position} (dealer: {dealerPosition}, activos: {string.Join(",", activeSeats)})");
            return position;
        }

        /// <summary>
        /// Intenta múltiples umbrales OCR para mejorar la detección
        /// </summary>
        private string TryMultipleOCRThresholds(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            // Lista de umbrales a probar
            var thresholds = new List<double?> { umbral, inactiveUmbral, 0.1, 0.2, 0.3, 0.4, 0.5 };

            foreach (var threshold in thresholds.Distinct())
            {
                var text = SetTextOCR(posX, posY, width, height, threshold, threshold, isOnlyNumber);
                if (!string.IsNullOrEmpty(text) && text.Contains("SIT"))
                {
                    return text;
                }
            }

            // Si ninguno contiene "SIT", devolver el mejor resultado
            return SetTextOCR(posX, posY, width, height, umbral, inactiveUmbral, isOnlyNumber);
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
                var scaled = GetScaledRegion(region);
                var textoo = TryMultipleOCRThresholds(scaled.X, scaled.Y, scaled.Width, scaled.Height,
                    region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);

                // Extracción de condición compleja a variable
                bool isSittingOut = !player.Empty && !player.Active &&
                    SetTextOCR(scaled.X, scaled.Y, scaled.Width, scaled.Height,
                               region.Umbral, region.InactiveUmbral, region.IsOnlyNumber)
                    .Contains("SIT");

                if (isSittingOut)
                {
                    player.SitOut = true;
                    // NO marcar Empty — jugador sitout sigue sentado, puede volver
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
        /// Establece las posiciones de los villanos basado en la posición de P0 y del dealer
        /// </summary>
        /// <param name="p0Position">Posición de P0</param>
        /// <param name="dealerPosition">Posición del dealer</param>
        private void SetVillainPosition(TablePosition p0Position, int dealerPosition)
        {
            var allPlayers = _playerGameState.Players.ToList();
            if (allPlayers == null || allPlayers.Count == 0)
                return;

            var activePlayers = allPlayers
                .Where(p => p != null && !p.Empty && !p.SitOut)
                .OrderBy(p => p.ValuePosition)
                .ToList();

            if (!activePlayers.Any())
                return;

            LogDebug($"SetVillainPosition - Jugadores activos: {string.Join(", ", activePlayers.Select(p => $"{p.Name}(VP:{p.ValuePosition},Empty:{p.Empty},SitOut:{p.SitOut})"))}, Posición héroe: {p0Position}, Dealer: {dealerPosition}");

            // Limpiar posiciones previas de jugadores activos (excepto héroe P0)
            foreach (var p in activePlayers.Where(p => p.ValuePosition != 0))
            {
                p.Position = TablePosition.None;
            }

            // Usar PositionCalculator para asignar posiciones a villanos (basado en posición del héroe)
            var villainPositions = PositionCalculator.AssignVillainPositions(p0Position, allPlayers);

            foreach (var kvp in villainPositions)
            {
                var player = activePlayers.FirstOrDefault(p => p.ValuePosition == kvp.Key);
                if (player != null)
                {
                    player.Position = kvp.Value;
                }
            }

            var positionLog = string.Join(", ", activePlayers.Select(p => $"{p.Name}:{p.Position}"));
            LogDebug($"Posiciones asignadas: {positionLog}");

            ValidatePositionAssignments(activePlayers);
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

                    // Elegibles solamente jugadores activos (excluir héroe P0)
                    bool shouldAssignPosition = !player.Empty && !player.SitOut &&
                                               player.Position == TablePosition.None &&
                                               player.ValuePosition != 0; // Excluir héroe
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

            var hashRegions = regionTableMap.Regions.Where(w => w.IsHash == true).ToList();

            // Carga de cartas una sola vez
            if (_cardsImages == null)
            {
                var cards = await session.Query<Card>().ToListAsync();
                _cardsImages = cards.Select(item => item.ToDto()).ToList();
            }

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
        /// Establece el valor de la apuesta para una región
        /// </summary>
        /// <returns>Valor decimal de la apuesta</returns>
        private decimal SetBetValue(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            // Validación de parámetros
            if (_formImage.pbImage.Image == null)
                return 0;

            using var firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                posX, posY, width, height,
                umbral ?? 0,
                isOnlyNumber ?? false);

            using var secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                posX, posY, width, height,
                inactiveUmbral ?? 0,
                isOnlyNumber ?? false);

            var result = string.Empty;

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
        /// Preprocesa una región de imagen para mejorar el OCR del stack
        /// </summary>
        private Bitmap PreprocessImageForOCR(Image sourceImage, int x, int y, int width, int height)
        {
            // Extraer la región
            var regionRect = new Rectangle(x, y, width, height);
            var regionBitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(regionBitmap))
            {
                g.DrawImage(sourceImage, new Rectangle(0, 0, width, height), regionRect, GraphicsUnit.Pixel);
            }

            // Convertir a escala de grises
            var grayBitmap = new Bitmap(width, height);
            using (var gGray = Graphics.FromImage(grayBitmap))
            {
                var colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                    new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                    new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
                var attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                gGray.DrawImage(regionBitmap, new Rectangle(0, 0, width, height), 0, 0, width, height, GraphicsUnit.Pixel, attributes);
            }
            regionBitmap.Dispose();

            // Binarización con umbral adaptativo simple
            var binaryBitmap = new Bitmap(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var pixel = grayBitmap.GetPixel(i, j);
                    var gray = (pixel.R + pixel.G + pixel.B) / 3;
                    var binaryColor = gray > 128 ? Color.White : Color.Black;
                    binaryBitmap.SetPixel(i, j, binaryColor);
                }
            }
            grayBitmap.Dispose();

            return binaryBitmap;
        }

        /// <summary>
        /// Establece el valor del stack usando OCR con doble lectura y preprocesamiento
        /// </summary>
        private decimal SetStackValue(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            // Validación de parámetros
            if (_formImage.pbImage.Image == null)
                return 0;

            OcrResult firstOcr;
            OcrResult secondOcr;

            // Lectura 1: con umbral principal y preprocesamiento
            using (var preprocessed = PreprocessImageForOCR(_formImage.pbImage.Image, posX, posY, width, height))
            {
                firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                    preprocessed, 0, 0, width, height,
                    umbral ?? 0, isOnlyNumber ?? false);
            }

            // Lectura 2: con umbral inactivo y preprocesamiento
            using (var preprocessed = PreprocessImageForOCR(_formImage.pbImage.Image, posX, posY, width, height))
            {
                secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                    preprocessed, 0, 0, width, height,
                    inactiveUmbral ?? 0, isOnlyNumber ?? false);
            }

            // Lectura 3: directa sin preprocesamiento (como fallback)
            using var thirdOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image, posX, posY, width, height,
                umbral ?? 0, isOnlyNumber ?? false);

            var result = string.Empty;

            if (isOnlyNumber.HasValue == true)
            {
                var cleanFirst = CleanOcrNumericText(firstOcr.Text);
                var cleanSecond = CleanOcrNumericText(secondOcr.Text);
                var cleanThird = CleanOcrNumericText(thirdOcr.Text);

                decimal.TryParse(cleanFirst, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out var ocr1);
                decimal.TryParse(cleanSecond, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out var ocr2);
                decimal.TryParse(cleanThird, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture, out var ocr3);

                // Elegir por consenso: si 2+ lecturas coinciden, usar ese valor.
                // Si no hay consenso, preferir la lectura directa (sin preprocesamiento)
                // ya que la binarización puede distorsionar dígitos.
                decimal best;
                if (ocr1 == ocr2 && ocr1 == ocr3)
                    best = ocr1;
                else if (ocr1 == ocr2)
                    best = ocr1;
                else if (ocr1 == ocr3)
                    best = ocr1;
                else if (ocr2 == ocr3)
                    best = ocr2;
                else
                    best = ocr3; // Sin consenso → preferir lectura directa (sin preprocesamiento)

                result = best.ToString();

                LogDebug($"[STACK] OCR lecturas: '{firstOcr.Text}'→{ocr1}, '{secondOcr.Text}'→{ocr2}, '{thirdOcr.Text}'→{ocr3}, best={best}");
            }

            firstOcr.Dispose();
            secondOcr.Dispose();

            if (decimal.TryParse(result, out var stack))
                return stack;

            return 0;
        }

        /// <summary>
        /// Lee el número de mano con 3 lecturas OCR + preprocesamiento + consenso.
        /// Misma estrategia robusta que SetStackValue para evitar lecturas erróneas.
        /// </summary>
        private string SetHandNumberOCR(int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
        {
            if (_formImage.pbImage.Image == null)
                return string.Empty;

            OcrResult firstOcr;
            OcrResult secondOcr;

            // Lectura 1: con umbral principal y preprocesamiento
            using (var preprocessed = PreprocessImageForOCR(_formImage.pbImage.Image, posX, posY, width, height))
            {
                firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                    preprocessed, 0, 0, width, height,
                    umbral ?? 0, isOnlyNumber ?? false);
            }

            // Lectura 2: con umbral inactivo y preprocesamiento
            using (var preprocessed = PreprocessImageForOCR(_formImage.pbImage.Image, posX, posY, width, height))
            {
                secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                    preprocessed, 0, 0, width, height,
                    inactiveUmbral ?? 0, isOnlyNumber ?? false);
            }

            // Lectura 3: directa sin preprocesamiento (fallback)
            using var thirdOcr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image, posX, posY, width, height,
                umbral ?? 0, isOnlyNumber ?? false);

            // Limpiar textos: solo dígitos para hand number
            var clean1 = CleanOcrHandNumber(firstOcr.Text);
            var clean2 = CleanOcrHandNumber(secondOcr.Text);
            var clean3 = CleanOcrHandNumber(thirdOcr.Text);

            // Consenso: si 2+ lecturas coinciden, usar ese valor
            string best;
            if (clean1 == clean2 && clean1 == clean3)
                best = clean1;
            else if (clean1 == clean2)
                best = clean1;
            else if (clean1 == clean3)
                best = clean1;
            else if (clean2 == clean3)
                best = clean2;
            else
                best = clean3; // Sin consenso → preferir lectura directa

            LogDebug($"[HAND#] OCR lecturas: '{firstOcr.Text}'→{clean1}, '{secondOcr.Text}'→{clean2}, '{thirdOcr.Text}'→{clean3}, best={best}");

            firstOcr.Dispose();
            secondOcr.Dispose();

            return best;
        }

        /// <summary>
        /// Limpia el texto OCR del número de mano dejando solo dígitos.
        /// </summary>
        private static string CleanOcrHandNumber(string? ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return string.Empty;

            var cleaned = new string(ocrText.Where(char.IsDigit).ToArray());
            return cleaned;
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

            using var ocr = _ocrService.ExtractTextFromRegionAndDebug(
                _formImage.pbImage.Image,
                posX, posY, width, height,
                umbral ?? 0,
                isOnlyNumber ?? false);

            // Si no se obtiene texto, intentar con umbral inactivo
            if (string.IsNullOrEmpty(ocr.Text))
            {
                using var ocrRetry = _ocrService.ExtractTextFromRegionAndDebug(
                    _formImage.pbImage.Image,
                    posX, posY, width, height,
                    inactiveUmbral ?? 0,
                    isOnlyNumber ?? false);

                return ocrRetry.Text ?? string.Empty;
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
                if (_handle == IntPtr.Zero)
                {
                    LogError("GetImageWhilePlaying: _handle es IntPtr.Zero, no se puede capturar");
                    return;
                }

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

                using var capturedBitmap = _useCase.ExecuteImage(path);

                if (capturedBitmap.Width <= 1 || capturedBitmap.Height <= 1)
                {
                    LogError($"GetImageWhilePlaying: Captura inválida ({capturedBitmap.Width}x{capturedBitmap.Height})");
                    return;
                }

                // Inicializar CoordinateScaler en primera captura y guardar en configuración
                if (!CoordinateScaler.IsInitialized)
                {
                    CoordinateScaler.Initialize(capturedBitmap.Width, capturedBitmap.Height);
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
                LogError($"Error al obtener imagen: {ex.Message}", ex);
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
                    LogError("SaveReferenceDimensionsToConfig: appsettings.json no encontrado");
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

                LogInformation($"Guardadas dimensiones de referencia: {width}x{height}");
            }
            catch (Exception ex)
            {
                LogError($"Error al guardar dimensiones de referencia: {ex.Message}");
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

            return CoordinateScaler.ScaleRegion(
                region.PosX, region.PosY, region.Width, region.Height,
                currentWidth, currentHeight);
        }

        /// <summary>
        /// Calcula la posición del overlay relativa a la ventana de poker.
        /// Centra horizontalmente con un offset proporcional al 15% del ancho de la ventana
        /// (desplaza a la izquierda para no tapar el centro de la mesa).
        /// Posiciona verticalmente a 75px del borde inferior.
        /// </summary>
        private Point CalculateOverlayPosition(User32.RECT windowRect, int overlayWidth)
        {
            int windowWidth = windowRect.right - windowRect.left;
            int horizontalOffset = (int)(windowWidth * _overlayConfig.HorizontalOffsetPercent);
            int centerX = windowRect.left + (windowWidth / 2);
            int x = centerX - (overlayWidth / 2) - horizontalOffset;
            int y = windowRect.bottom - _overlayConfig.VerticalOffset;
            return new Point(x, y);
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

                    _frmOverlay.Location = CalculateOverlayPosition(windowRect, _frmOverlay.Size.Width);
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
                                _frmOverlay.Location = CalculateOverlayPosition(windowRect, _frmOverlay.Size.Width);
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
                        var regionAction = _regionsTableMap?.FirstOrDefault(f => f.Id == "User")?.Regions?.FirstOrDefault(x => x.Name == "uAction");
                        var flop = _regionsTableMap?.FirstOrDefault(f => f.Id == "Table")?.Regions?.FirstOrDefault(x => x.Name == "isFlop");

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
                                    _gameLoopStateMachine.TryTransition(GameState.FlopDetected);
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
                LogError($"Error en BackgroundWorker1_DoWork: {ex.Message}", ex);
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

                // Obtener color principal
                Color primaryActionColor = bitmap.GetPixel(scaledAction.X, scaledAction.Y);
                Color flopColor = bitmap.GetPixel(scaledFlop.X, scaledFlop.Y);

                // Muestrear píxeles adicionales alrededor del punto principal para mayor robustez
                var sampleColors = new List<Color> { primaryActionColor };

                // Muestrear en un patrón de cruz pequeño (±2 píxeles)
                var offsets = new[] { (-2, 0), (2, 0), (0, -2), (0, 2), (-1, -1), (1, 1), (-1, 1), (1, -1) };

                foreach (var (dx, dy) in offsets)
                {
                    var x = scaledAction.X + dx;
                    var y = scaledAction.Y + dy;

                    if (x >= 0 && x < bitmap.Width && y >= 0 && y < bitmap.Height)
                    {
                        sampleColors.Add(bitmap.GetPixel(x, y));
                    }
                }

                // Análisis de colores con tolerancia
                var avgB = sampleColors.Average(c => c.B);
                var avgR = sampleColors.Average(c => c.R);
                var avgG = sampleColors.Average(c => c.G);

                // Detección con rango de tolerancia en lugar de valor exacto
                const int TARGET_B = 24;
                const int TOLERANCE = 3; // Tolerancia de ±3 para el valor B

                bool isActionColorInRange = Math.Abs(avgB - TARGET_B) <= TOLERANCE;
                bool shouldCapture = isActionColorInRange && !_executeCapture;

                // Detección de flop mejorada
                const int FLOP_TARGET_B = 255;
                const int FLOP_TOLERANCE = 10;
                bool isFlopVisible = Math.Abs(flopColor.B - FLOP_TARGET_B) <= FLOP_TOLERANCE;
                bool shouldCaptureFlop = shouldCapture && isFlopVisible;

                return new DetectionResult
                {
                    ActionColor = primaryActionColor,
                    FlopColor = flopColor,
                    AverageActionB = avgB,
                    SampleCount = sampleColors.Count,
                    IsActionColorInRange = isActionColorInRange,
                    ShouldCapture = shouldCapture,
                    ShouldCaptureFlop = shouldCaptureFlop,
                    IsFlopVisible = isFlopVisible
                };
            }
            catch (Exception ex)
            {
                _detectionLoggerService.LogDetectionError($"Error en detección mejorada: {ex.Message}", ex);

                // Fallback a detección simple
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

                var ocr1 = decimal.Parse(firstOcr.Text);
                var ocr2 = decimal.Parse(secondOcr.Text);

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
            var logLine = exception != null
                ? $"[{DateTime.Now:HH:mm:ss}] {message} | Exception: {exception.Message}"
                : $"[{DateTime.Now:HH:mm:ss}] {message}";

            Console.WriteLine(logLine);

            if (tbResume != null && !tbResume.IsDisposed)
            {
                if (tbResume.InvokeRequired)
                    tbResume.Invoke(() => AppendLog(logLine));
                else
                    AppendLog(logLine);
            }
        }

        private void AppendLog(string line)
        {
            tbResume.AppendText(line + Environment.NewLine);
            tbResume.SelectionStart = tbResume.TextLength;
            tbResume.ScrollToCaret();
        }

        /// <summary>
        /// Formatea las cartas del hero y del board para logs legibles.
        /// Ejemplo: "Hero: [As Qc]  Board: [Qh 3h 7s] + [5d]"
        /// </summary>
        private string FormatCardsForLog(BoardPosition street)
        {
            var hero = $"[{_playerGameState.HoleCard1Face} {_playerGameState.HoleCard2Face}]";
            var boardCards = _playerGameState.BoardCards
                .Where(b => b.Position != BoardPosition.Hand)
                .OrderBy(b => b.Location)
                .ToList();

            var flopCards = boardCards.Where(b => b.Position == BoardPosition.Flop)
                .Select(b => b.Name ?? "??").ToList();
            var flop = flopCards.Count > 0 ? $"[{string.Join(" ", flopCards)}]" : "";

            if (street == BoardPosition.Flop)
                return $"Hero: {hero}  Board: {flop}";

            var turnCard = boardCards.FirstOrDefault(b => b.Position == BoardPosition.Turn);
            var turn = turnCard != null ? $"[{turnCard.Name ?? "??"}]" : "";

            if (street == BoardPosition.Turn)
                return $"Hero: {hero}  Board: {flop} + {turn}";

            var riverCard = boardCards.FirstOrDefault(b => b.Position == BoardPosition.River);
            var river = riverCard != null ? $"[{riverCard.Name ?? "??"}]" : "";

            return $"Hero: {hero}  Board: {flop} + {turn} + {river}";
        }

        /// <summary>
        /// Logs an informational message to the output or a log file.
        /// </summary>
        /// <param name="message"></param>
        private void LogInformation(string message)
        {
            var logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(logLine);

            if (tbResume != null && !tbResume.IsDisposed)
            {
                if (tbResume.InvokeRequired)
                    tbResume.Invoke(() => AppendLog(logLine));
                else
                    AppendLog(logLine);
            }
        }

        /// <summary>
        /// Solo escribe a Console (debug). No aparece en la pestaña de logs del usuario.
        /// </summary>
        private static void LogDebug(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] {message}");
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

        private void tbJuego_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Ajusta el tamaño de apuesta basado en stack dinámico
        /// </summary>
        private string AdjustBetSize(string action, decimal heroStack, decimal potSize, int numOpponents, bool isPaired, bool isCoordinated, bool isDry, bool isInPosition)
        {
            if (!action.StartsWith("Bet "))
                return action;

            var parts = action.Split(' ');
            double baseSize;

            if (parts[1] == "Pot")
                baseSize = 1.0;
            else if (parts[1].Contains('/'))
            {
                var frac = parts[1].Split('/');
                baseSize = double.Parse(frac[0]) / double.Parse(frac[1]);
            }
            else
                return action; // not a standard bet

            var adjustedBet = _betSizingService.CalculateDynamicBetSize(baseSize, heroStack, potSize, numOpponents, isPaired, isCoordinated, isDry, isInPosition);
            var reason = action.Contains('(') ? action.Substring(action.IndexOf('(')) : "";
            return adjustedBet + reason;
        }

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
                Console.WriteLine($"Error cargando sesiones: {ex.Message}");
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
                Console.WriteLine($"Error cargando manos: {ex.Message}");
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

        #endregion
    }


}
