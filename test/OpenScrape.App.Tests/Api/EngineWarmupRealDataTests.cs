using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenScrape.Api;
using OpenScrape.Api.Persistence;
using OpenScrape.Contracts;
using OpenScrape.DecisionMaker;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Interfaces;
using OpenScrape.Infrastructure;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// Valida las peticiones de precalentamiento contra los rangos preflop reales.
///
/// El test que usa ApiFactory pasaba porque ese contenedor sustituye
/// IPreflopRangeStore por un doble: validaba el doble y no los datos. En producción
/// el precalentamiento abortaba con "No hay escenario preflop para la situación
/// 'OpenRaise / Button'". Este test corre el motor real contra la base real.
/// </summary>
[TestFixture]
[Explicit("Requiere la base real: se corre a mano cuando cambian los rangos o el precalentamiento")]
public class EngineWarmupRealDataTests
{
    [Test]
    public async Task CadaPeticionDePrecalentamiento_SeResuelveContraLosRangosReales()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Ignore("Sin ConnectionStrings__DefaultConnection no se puede validar contra los rangos reales.");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(RepoRoot(), "src", "OpenScrape.Api", "appsettings.json"), optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataBase(configuration, isDevelopment: false);
        services.Configure<StrategyProfile>(configuration.GetSection("StrategyProfile"));
        services.AddScoped<GetTable>();
        services.AddScoped<IPreflopRangeStore, MartenPreflopRangeStore>();
        services.AddDecisionMaker();

        using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var engine = scope.ServiceProvider.GetRequiredService<IDecisionEngine>();
        var fallos = new List<string>();

        foreach (var request in EngineWarmupService.SampleRequests())
        {
            try
            {
                var result = await engine.DecideAsync(request);

                if (string.IsNullOrWhiteSpace(result.Action))
                    fallos.Add($"{request.Street}: accion vacia");
            }
            catch (Exception ex)
            {
                fallos.Add($"{request.Street}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.That(fallos, Is.Empty, "Precalentamiento invalido: " + string.Join(" | ", fallos));
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OpenScrape.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se encontro OpenScrape.sln desde el directorio de tests.");
    }
}
