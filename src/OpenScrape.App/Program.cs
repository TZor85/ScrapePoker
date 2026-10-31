using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Marten;
using OpenScrape.App.Aplication;
using OpenScrape.App.Configuration;
using OpenScrape.App.Forms.Controls;
using OpenScrape.App.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Calibration;
using OpenScrape.App.Services.Exploitability;
using OpenScrape.App.Services.Http;
using OpenScrape.App.Services.License;
using OpenScrape.App.Services.Logging;
using OpenScrape.App.Services.Tracking;
using OpenScrape.Domain;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Exceptions;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.App.Aplication.RegionsTableMap;
using OpenScrape.App.Aplication.RegionsTableMap.Update;
using OpenScrape.Infrastructure;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App
{
    internal static class Program
    {
        public static IConfiguration? Configuration { get; private set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

            // El ContentRoot por defecto es el working directory. Con `dotnet run --project`
            // el CWD queda en la raíz del repo y no hay appsettings*.json ahí, así que la
            // connection string no se resolvía. El directorio del ejecutable es donde el
            // build deja los appsettings.
            var builder = Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .UseEnvironment(environment)
                .ConfigureLogging((context, lb) =>
                {
                    lb.AddTextBoxLogger();
                    lb.AddMartenLogger();
                })
                .ConfigureServices(Program.ConfigureClientServices);

            var host = builder.Build();

            Configuration = host.Services.GetRequiredService<IConfiguration>();

            // La mano en curso se estampa también en el panel de logs: el provider nace
            // antes que el contenedor, así que el estado se le pasa cuando ya existe.
            host.Services.GetRequiredService<TextBoxLoggerProvider>()
                .SetHandContext(host.Services.GetRequiredService<HandContext>());

            // Validación fail-fast del StrategyProfile: abortar antes de mostrar FrmMain
            // si faltan claves, hay claves inválidas, o tiers incoherentes.
            try
            {
                var profileOptions = host.Services.GetRequiredService<IOptions<StrategyProfile>>();
                StrategyProfileValidator.Validate(profileOptions.Value);
            }
            catch (StrategyProfileValidationException ex)
            {
                MessageBox.Show(ex.Message, "Error de configuración", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            // Inicializar CoordinateScaler desde configuración
            var captureSettings = Configuration.GetSection("CaptureSettings");
            if (captureSettings["IsReferenceSet"] == "true" &&
                int.TryParse(captureSettings["ReferenceImageWidth"], out var refWidth) &&
                int.TryParse(captureSettings["ReferenceImageHeight"], out var refHeight) &&
                refWidth > 0 && refHeight > 0)
            {
                var scaler = host.Services.GetRequiredService<ICoordinateScaler>();
                scaler.Initialize(refWidth, refHeight);
            }

            // Precomputar plantillas de cartas antes de arrancar la UI (evita latencia en la primera mano)
            host.Services.GetRequiredService<CardTemplateIndex>().InitializeAsync().GetAwaiter().GetResult();

            // El motor es remoto: si la API no escucha, decirlo al arrancar con la causa
            // y el comando que la instala. Sin esto el sintoma aparece recien jugando.
            var healthProbe = host.Services.GetRequiredService<IEngineHealthProbe>();
            if (!healthProbe.IsHealthyAsync().GetAwaiter().GetResult())
            {
                var apiBaseUrl = Configuration["Api:BaseUrl"] ?? "http://localhost:5015";

                MessageBox.Show(
                    $"La API del motor no responde en {apiBaseUrl}." + Environment.NewLine +
                    Environment.NewLine +
                    "Instalala como servicio de Windows con scripts\\install-api-service.ps1 " +
                    "desde una consola elevada.",
                    "Motor no disponible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            // Obtener el formulario principal desde un scope async para resolver dependencias scoped.
            // CreateAsyncScope es necesario porque algunos servicios (p. ej. GameLoopCoordinator) sólo
            // implementan IAsyncDisposable; usar un scope síncrono los rompería al cerrar la app.
            var scope = host.Services.CreateAsyncScope();
            try
            {
                // Compuerta de licencia: el motor es remoto, así que no se arranca
                // sin un token vigente.
                var licenseStore = host.Services.GetRequiredService<LicenseTokenStore>();

                if (LicenseGate.NeedsActivation(licenseStore, DateTimeOffset.UtcNow))
                {
                    using var licenseForm = new FormLicense(
                        host.Services.GetRequiredService<LicenseClient>(),
                        licenseStore);

                    if (licenseForm.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show(
                            "Se necesita una licencia vigente para iniciar el motor.",
                            "OpenScrape",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }
                }

                var form = scope.ServiceProvider.GetRequiredService<FrmMain>();
                Application.Run(form);
            }
            finally
            {
                scope.DisposeAsync().AsTask().GetAwaiter().GetResult();

                // El proveedor escribe por lotes: volcar la cola antes de salir
                // para no perder los últimos eventos de la sesión.
                try
                {
                    host.Services.GetRequiredService<MartenLoggerProvider>()
                        .FlushAsync()
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MartenLogger] No se pudo vaciar la cola al cerrar: {ex}");
                }
            }
        }

        /// <summary>
        /// Registro completo de servicios del cliente. Vive fuera de <see cref="Main"/>
        /// para que un test pueda construir el contenedor: si un registro del cliente
        /// desaparece, la app muere al arrancar y este método es lo único que permite
        /// detectarlo antes de publicar.
        /// </summary>
        internal static void ConfigureClientServices(HostBuilderContext context, IServiceCollection services)
        {
            // Agregar configuración de base de datos
            services.AddDataBase(context.Configuration, context.HostingEnvironment.IsDevelopment());

            // Use cases del editor de regiones (viven en el cliente, no en la API).
            services.AddScoped<UpdateRegionTableMap>();
            services.AddScoped<RegionTableMapUseCases>();
            //services.AddScoped<IFileDialogService, WindowsFileDialogService>();

            //services.AddScoped<OcrService>();

            // Configuración del overlay
            services.Configure<OverlayConfig>(context.Configuration.GetSection("OverlayConfig"));

            // Strategy profile (antes de servicios que lo usan)
            services.Configure<StrategyProfile>(context.Configuration.GetSection("StrategyProfile"));
            services.AddSingleton<StrategyProfileService>();
            services.AddSingleton<
                OpenScrape.Domain.Interfaces.IRandomProvider,
                OpenScrape.Domain.Services.SystemRandomProvider>();

            // Opciones del game loop y feature flags (refactor-frmmain-coordinators)
            services.Configure<GameLoopOptions>(context.Configuration.GetSection(GameLoopOptions.SectionName));
            services.Configure<FeatureFlags>(context.Configuration.GetSection(FeatureFlags.SectionName));

            // Estado del cliente: tracking, exploitability, calibración y bankroll
            services.AddSingleton<IOpponentProfileStore, MartenOpponentProfileStore>();
            services.AddSingleton<IDecisionTraceStore, MartenDecisionTraceStore>();
            services.AddSingleton<OpponentTracker>();
            services.AddSingleton<IOpponentTracker>(sp => sp.GetRequiredService<OpponentTracker>());
            services.AddSingleton<ExploitabilityCalculator>();
            services.AddSingleton<IExploitabilityCalculator>(sp => sp.GetRequiredService<ExploitabilityCalculator>());
            services.AddSingleton<AutoCalibrationService>();
            services.AddSingleton<IAutoCalibrationService>(sp => sp.GetRequiredService<AutoCalibrationService>());

            // Cliente del motor: siempre por HTTP. El timeout por decisión lo
            // impone HttpDecisionEngine.DecisionTimeout.
            services.AddLicense(context.Configuration);
            services.AddScoped<IDecisionEngine, HttpDecisionEngine>();
            services.AddSingleton<IEngineHealthProbe, EngineHealthProbe>();
            services.AddSingleton<AppModeState>();
            services.AddSingleton<HandContext>();
            services.AddSingleton<IGetHashImageUseCase, GetHashImageUseCase>();
            services.AddSingleton<IGetCropImageUseCase, GetCropImageUseCase>();
            services.AddSingleton<IOutsCalculatorUseCase, OutsCalculatorUseCase>();
            services.AddSingleton<GetWindowsScreenUseCase>();

            // Screen reader (OCR + preprocesamiento + normalización)
            services.AddSingleton<ScreenReaderService>();
            services.AddSingleton<IScreenReaderService>(sp => sp.GetRequiredService<ScreenReaderService>());

            // Game logger y state machine
            services.AddScoped<GameLoggerService>();
            services.AddScoped<IGameLoggerService>(sp => sp.GetRequiredService<GameLoggerService>());
            services.AddSingleton<GameLoopStateMachine>();
            services.AddSingleton<RegionLookupCache>();
            services.AddSingleton<CardCacheService>();

            // Detection loop service (Fase 1: extraído de FrmMain.BackgroundWorker1_DoWork)
            services.AddSingleton<ITurnGate, TurnGate>();
            services.AddSingleton<IDetectionLoopService, DetectionLoopService>();

            // Capture service (Fase 2: extraído de FrmMain.GetImageWhilePlaying)
            services.AddSingleton<IGetWindowsScreenUseCase>(sp => sp.GetRequiredService<GetWindowsScreenUseCase>());
            services.AddScoped<ICaptureService, CaptureService>();

            // Table reader service (Fase 3: extraído de FrmMain.SetBetPlayer/SetHeroStack/etc.)
            // Interfaces para testeabilidad: forwarding desde las implementaciones concretas existentes
            services.AddSingleton<IRegionLookupCache>(sp => sp.GetRequiredService<RegionLookupCache>());
            services.AddSingleton<IImageCropperService>(sp => sp.GetRequiredService<ImageCropperService>());
            services.AddSingleton<ICardCacheService>(sp => sp.GetRequiredService<CardCacheService>());
            // Reconocimiento de cartas centralizado (plantillas precomputadas + dHash shortlist)
            services.AddSingleton<CardTemplateIndex>();
            services.AddSingleton<ICardRecognizer, CardRecognitionEngine>();
            services.AddSingleton<IOcrService>(sp => sp.GetRequiredService<OcrService>());
            services.AddScoped<ITableReaderService, TableReaderService>();

            // Table layout (detección jugadores, dealer, posiciones)
            services.AddScoped<TableLayoutService>();
            services.AddScoped<ITableLayoutService>(sp => sp.GetRequiredService<TableLayoutService>());

            // Postflop context holder (scoped: una instancia por sesión de juego)
            services.AddScoped<PostflopContextHolder>();
            services.AddScoped<IPostflopContextHolder>(sp => sp.GetRequiredService<PostflopContextHolder>());

            // Game coordinator
            services.AddScoped<GameCoordinator>();
            services.AddScoped<IGameCoordinator>(sp => sp.GetRequiredService<GameCoordinator>());

            // CoordinateScaler y telemetría: los usan las capturas, los
            // use cases de cartas y los servicios de logging.
            services.AddSingleton<CoordinateScaler>();
            services.AddSingleton<ICoordinateScaler>(sp => sp.GetRequiredService<CoordinateScaler>());
            services.AddSingleton<IMetricsCollector, MetricsCollector>();
            services.AddSingleton<DetectionLoggerService>();
            services.AddSingleton<OcrService>();
            services.AddSingleton<ColorDetectionService>();
            services.AddSingleton<ImageCropperService>();
            services.AddSingleton<ISetFlopForceBoardUseCase, SetFlopForceBoardUseCase>();

            // Servicios de UI y orquestación del game loop.
            services.AddScoped<IGameLoopCoordinator, GameLoopCoordinator>();
            services.AddScoped<IUiSyncService, UiSyncService>();
            services.AddScoped<IUiUpdateService, UiUpdateService>();
            services.AddSingleton<IActionFormatter, ActionFormatter>();
            services.AddSingleton<IOverlayPositioner, OverlayPositioner>();

            // UseCases de cartas (necesitan CardCacheService + ICoordinateScaler)
            services.AddSingleton<IGetCardsFlopUseCase, GetCardsFlopUseCase>();
            services.AddSingleton<IGetCardsTurnUseCase, GetCardsTurnUseCase>();
            services.AddSingleton<IGetCardsRiverUseCase, GetCardsRiverUseCase>();

            // Fase 7: UserControls de TabPageContributions
            services.AddTransient<MetricsTabControl>();

            // Fase 8: RegionEditorControl extraído de FrmMain
            services.AddTransient<RegionEditorControl>();

            //// Registrar tu formulario principal
            services.AddTransient<FrmMain>();
        }
    }
}
