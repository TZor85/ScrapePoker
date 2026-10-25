using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenScrape.App.Tests.Fixtures;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Interfaces;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// Monta la API en proceso. Reemplaza las dependencias con estado
/// (base de datos, azar y simulador) por implementaciones deterministas.
/// </summary>
internal sealed class ApiFactory : WebApplicationFactory<ApiMarker>
{
    private readonly TableDTO _table;
    private readonly int _randomNumber;
    private readonly int _monteCarloSeed;
    private bool _checkRevocation;

    public ApiFactory(TableDTO? table = null, int randomNumber = 30, int monteCarloSeed = 1)
    {
        _table = table ?? FixtureTables.OpenRaiseAks();
        _randomNumber = randomNumber;
        _monteCarloSeed = monteCarloSeed;
        LicenseStore = LicenseTestData.CreateStore();
    }

    public InMemoryLicenseStore LicenseStore { get; }

    public void EnableRevocationCheck() => _checkRevocation = true;

    /// <summary>Cliente con un Bearer real emitido por el servicio de la API.</summary>
    public HttpClient CreateLicensedClient(string plan = "pro")
    {
        var client = CreateClient();
        var id = plan == "pro" ? LicenseTestData.ProLicenseId : LicenseTestData.FreeLicenseId;
        var license = LicenseStore.GetAsync(id).GetAwaiter().GetResult()!;
        var tokens = Services.GetRequiredService<OpenScrape.Api.Licensing.ILicenseTokenService>();
        var (token, _) = tokens.Issue(license);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPreflopRangeStore>();
            services.AddSingleton<IPreflopRangeStore>(new FixedRangeStore(_table));

            services.RemoveAll<OpenScrape.Domain.Interfaces.IRandomProvider>();
            services.AddSingleton<OpenScrape.Domain.Interfaces.IRandomProvider>(new FixedRandomProvider(_randomNumber));

            services.RemoveAll<OpenScrape.DecisionMaker.Interfaces.IRandomProvider>();
            services.AddSingleton<OpenScrape.DecisionMaker.Interfaces.IRandomProvider>(
                new FixedEngineRandomProvider(_randomNumber));

            services.RemoveAll<OpenScrape.Api.Licensing.ILicenseStore>();
            services.AddSingleton<OpenScrape.Api.Licensing.ILicenseStore>(LicenseStore);

            services.Configure<OpenScrape.Api.Licensing.JwtOptions>(options =>
            {
                options.SigningKey = LicenseTestData.SigningKey;
                options.LifetimeHours = 24;
            });
            services.Configure<OpenScrape.Api.Licensing.LicenseOptions>(options =>
            {
                options.Pepper = LicenseTestData.Pepper;
                options.CheckRevocationPerRequest = _checkRevocation;
            });

            services.RemoveAll<MonteCarloSimulator>();
            services.AddSingleton(new MonteCarloSimulator(_monteCarloSeed));
        });
    }

    private sealed class FixedEngineRandomProvider(int value)
        : OpenScrape.DecisionMaker.Interfaces.IRandomProvider
    {
        public double NextDouble() => 0d;

        public int Next(int minValue, int maxValue) => value;
    }
}

/// <summary>Tablas de rango tomadas de los fixtures ya versionados.</summary>
internal static class FixtureTables
{
    public static TableDTO OpenRaiseAks() =>
        PreflopFixtureStore.LoadAll()
            .Single(fixture => fixture.Id == "open-raise-aks")
            .Table;

    public static TableDTO Empty() => new() { Name = "sin-tabla", Positions = [] };
}
