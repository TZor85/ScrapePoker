using Microsoft.Extensions.DependencyInjection;
using OpenScrape.Features.ActionScenario;
using OpenScrape.Features.ActionScenario.Get;
using OpenScrape.Features.Card;
using OpenScrape.Features.Card.GetAll;
using OpenScrape.Features.RegionsTableMap.GetAll;
using OpenScrape.Features.Table;
using OpenScrape.Features.Table.Get;
using OpenScrape.Features.Table.GetAll;

namespace OpenScrape.Features;

public static class Services
{
    public static IServiceCollection AddUseCases(this IServiceCollection services) =>
    services
        .AddScoped<GetActionScenario>()
        .AddScoped<ActionScenarioUseCases>()

        .AddScoped<GetTable>()
        .AddScoped<TableUseCases>()

        .AddScoped<GetAllCards>()
        .AddScoped<CardUseCases>()

        .AddScoped<GetAllRegionTableMap>()
        .AddScoped<GetAllTables>();
}

