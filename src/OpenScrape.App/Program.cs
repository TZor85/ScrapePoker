using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Marten;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.Features;
using OpenScrape.Infrastructure;
using OpenScrape.App.Aplication.UseCases;

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

            var builder = Host.CreateDefaultBuilder()
                .UseEnvironment(environment)
                .ConfigureServices((context, services) =>
                {
                    // Agregar configuraci�n de base de datos
                    services.AddDataBase(context.Configuration, true);
                    services.AddUseCases();
                    //services.AddScoped<IFileDialogService, WindowsFileDialogService>();

                    //services.AddScoped<OcrService>();

                    // Configuración del overlay
                    services.Configure<OverlayConfig>(context.Configuration.GetSection("OverlayConfig"));

                    // Strategy profile (antes de servicios que lo usan)
                    services.Configure<StrategyProfile>(context.Configuration.GetSection("StrategyProfile"));
                    services.AddSingleton<StrategyProfileService>();

                    // Algoritmos: registro por clase concreta + forwarding por interfaz (misma instancia)
                    services.AddSingleton<MonteCarloSimulator>();
                    services.AddSingleton<IMonteCarloSimulator>(sp => sp.GetRequiredService<MonteCarloSimulator>());
                    services.AddSingleton<BitHandEvaluator>();
                    services.AddSingleton<IHandEvaluator>(sp => sp.GetRequiredService<BitHandEvaluator>());
                    services.AddSingleton<OutsCalculator>();
                    services.AddSingleton<IOutsCalculator>(sp => sp.GetRequiredService<OutsCalculator>());
                    services.AddSingleton<BoardTextureAnalyzer>();
                    services.AddSingleton<IBoardTextureAnalyzer>(sp => sp.GetRequiredService<BoardTextureAnalyzer>());
                    services.AddSingleton<PreflopEquityCalculator>();
                    services.AddSingleton<EquityCalculatorService>();

                    // Servicios DecisionMaker: clase concreta + forwarding por interfaz
                    services.AddSingleton<BetSizingService>();
                    services.AddSingleton<IBetSizingService>(sp => sp.GetRequiredService<BetSizingService>());
                    services.AddSingleton<RangePolarizer>();
                    services.AddSingleton<IRangePolarizer>(sp => sp.GetRequiredService<RangePolarizer>());
                    services.AddSingleton<PostflopDecisionService>();
                    services.AddSingleton<IPostflopDecisionService>(sp => sp.GetRequiredService<PostflopDecisionService>());
                    services.AddSingleton<OpponentTracker>();
                    services.AddSingleton<IOpponentTracker>(sp => sp.GetRequiredService<OpponentTracker>());
                    services.AddSingleton<StrategyAnalyzerService>();
                    services.AddSingleton<IStrategyAnalyzerService>(sp => sp.GetRequiredService<StrategyAnalyzerService>());
                    services.AddSingleton<ExploitabilityCalculator>();
                    services.AddSingleton<IExploitabilityCalculator>(sp => sp.GetRequiredService<ExploitabilityCalculator>());
                    services.AddSingleton<AutoCalibrationService>();
                    services.AddSingleton<IAutoCalibrationService>(sp => sp.GetRequiredService<AutoCalibrationService>());
                    services.AddSingleton<DangerPenaltyCalculator>();
                    services.AddSingleton<IDangerPenaltyCalculator>(sp => sp.GetRequiredService<DangerPenaltyCalculator>());
                    services.AddSingleton<ImpliedOddsCalculator>();
                    services.AddSingleton<IImpliedOddsCalculator>(sp => sp.GetRequiredService<ImpliedOddsCalculator>());
                    services.AddSingleton<PreflopAnalyzer>();
                    services.AddSingleton<IPreflopAnalyzer>(sp => sp.GetRequiredService<PreflopAnalyzer>());
                    services.AddSingleton<EquityCalculatorService>();
                    services.AddSingleton<IEquityCalculatorService>(sp => sp.GetRequiredService<EquityCalculatorService>());
                    services.AddSingleton<StrategyBacktester>();
                    services.AddSingleton<IStrategyBacktester>(sp => sp.GetRequiredService<StrategyBacktester>());
                    services.AddSingleton(sp =>
                    {
                        var store = sp.GetRequiredService<IDocumentStore>();
                        var profile = sp.GetRequiredService<IOptions<StrategyProfile>>().Value;
                        return new BankrollTrackerService(store, profile);
                    });
                    services.AddSingleton<IBankrollTrackerService>(sp => sp.GetRequiredService<BankrollTrackerService>());

                    // Register unified calculator
                    services.AddSingleton<IPokerCalculator, UnifiedPokerCalculator>();

                    // Game logger y state machine
                    services.AddScoped<GameLoggerService>();
                    services.AddSingleton<GameLoopStateMachine>();
                    services.AddSingleton<RegionLookupCache>();
                    services.AddSingleton<CardCacheService>();

                    //// Registrar tu formulario principal
                    services.AddTransient<FrmMain>();
                });

            var host = builder.Build();

            Configuration = host.Services.GetRequiredService<IConfiguration>();

            // Inicializar CoordinateScaler desde configuración
            var captureSettings = Configuration.GetSection("CaptureSettings");
            if (captureSettings["IsReferenceSet"] == "true" &&
                int.TryParse(captureSettings["ReferenceImageWidth"], out var refWidth) &&
                int.TryParse(captureSettings["ReferenceImageHeight"], out var refHeight) &&
                refWidth > 0 && refHeight > 0)
            {
                CoordinateScaler.Initialize(refWidth, refHeight);
            }

            // Obtener el formulario principal desde un scope para resolver dependencias scoped
            using var scope = host.Services.CreateScope();
            var form = scope.ServiceProvider.GetRequiredService<FrmMain>();

            Application.Run(form);
        }
    }
}