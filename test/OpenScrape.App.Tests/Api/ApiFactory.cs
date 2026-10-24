using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenScrape.App.Tests.Fixtures;
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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPreflopRangeStore>();
            services.AddSingleton<IPreflopRangeStore>(new FixedRangeStore(FixtureTables.OpenRaiseAks()));

            services.RemoveAll<IRandomProvider>();
            services.AddSingleton<IRandomProvider>(new FixedRandomProvider(30));

            services.RemoveAll<OpenScrape.DecisionMaker.Interfaces.IRandomProvider>();
            services.AddSingleton<OpenScrape.DecisionMaker.Interfaces.IRandomProvider>(
                new FixedEngineRandomProvider(30));

            services.RemoveAll<MonteCarloSimulator>();
            services.AddSingleton(new MonteCarloSimulator(1));
        });
    }

    /// <summary>El motor declara su propio contrato de azar; se fija igual que el del dominio.</summary>
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
