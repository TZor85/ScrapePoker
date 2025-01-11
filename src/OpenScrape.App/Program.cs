using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenScrape.Infrastructure;

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

            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Agregar configuración de base de datos
                    services.AddDataBase(context.Configuration, true);
                    //services.AddUseCases();
                    //services.AddScoped<IFileDialogService, WindowsFileDialogService>();

                    //services.AddScoped<OcrService>();

                    //// Registrar tu formulario principal
                    services.AddTransient<FrmMain>();
                });

            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "Production";

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var host = builder.Build();

            // Obtener el formulario principal desde el contenedor de servicios
            var form = host.Services.GetRequiredService<FrmMain>();

            Application.Run(form);
        }
    }
}