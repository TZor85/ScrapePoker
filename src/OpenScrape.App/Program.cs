using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenScrape.App.Services;
using OpenScrape.DecisionMaker.Algorithms;
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

                    // Register equity calculation components
                    services.AddSingleton<MonteCarloSimulator>();
                    services.AddSingleton<HandEvaluator>();
                    services.AddSingleton<OutsCalculator>();
                    services.AddSingleton<PreflopEquityCalculator>();
                    services.AddSingleton<EquityCalculatorService>();
                    // Strategy profile (antes de servicios que lo usan)
                    services.Configure<StrategyProfile>(context.Configuration.GetSection("StrategyProfile"));
                    services.AddSingleton<StrategyProfileService>();

                    services.AddSingleton<BetSizingService>();
                    services.AddSingleton<BoardTextureAnalyzer>();
                    services.AddSingleton<PostflopDecisionService>();
                    services.AddSingleton<OpponentTracker>();

                    // Register unified calculator
                    services.AddSingleton<IPokerCalculator, UnifiedPokerCalculator>();

                    // Game logger y state machine
                    services.AddScoped<GameLoggerService>();
                    services.AddSingleton<GameLoopStateMachine>();

                    //// Registrar tu formulario principal
                    services.AddTransient<FrmMain>();
                });

            var host = builder.Build();

            Configuration = host.Services.GetRequiredService<IConfiguration>();

            // Obtener el formulario principal desde un scope para resolver dependencias scoped
            using var scope = host.Services.CreateScope();
            var form = scope.ServiceProvider.GetRequiredService<FrmMain>();

            Application.Run(form);
        }
    }
}