using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using Marten;
using OpenScrape.Api.Endpoints;
using OpenScrape.Api.Licensing;
using OpenScrape.DecisionMaker;
using OpenScrape.Domain;
using OpenScrape.Domain.Entities;
using OpenScrape.Features;
using OpenScrape.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// El perfil de estrategia es la única configuración que el motor necesita en el servidor.
builder.Services.Configure<StrategyProfile>(builder.Configuration.GetSection("StrategyProfile"));

builder.Services.AddDecisionMaker();
builder.Services.AddUseCases();
builder.Services.AddDataBase(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.ConfigureMarten(options =>
    options.Schema.For<License>().DatabaseSchemaName("license"));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<LicenseOptions>(builder.Configuration.GetSection("License"));
builder.Services.AddSingleton<ILicenseKeyHasher, LicenseKeyHasher>();
builder.Services.AddSingleton<ILicenseTokenService, JwtLicenseTokenService>();
builder.Services.AddScoped<ILicenseStore, MartenLicenseStore>();
builder.Services.AddPlanRateLimiting(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

// Fallar rápido si el perfil quedó incompleto: sin él ninguna decisión es confiable.
StrategyProfileValidator.Validate(app.Services.GetRequiredService<IOptions<StrategyProfile>>().Value);

app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapLicenseEndpoints();
app.MapDecisionEndpoints();

app.Run();

/// <summary>Marca el ensamblado de la API para que los tests monten el host en proceso.</summary>
public sealed class ApiMarker;
