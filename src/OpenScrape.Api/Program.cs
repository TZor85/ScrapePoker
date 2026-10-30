using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using System.Text;
using Marten;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenScrape.Contracts;
using OpenScrape.Api.Endpoints;
using OpenScrape.Api.Licensing;
using OpenScrape.Api.Persistence;
using OpenScrape.DecisionMaker;
using OpenScrape.Domain;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Interfaces;
using OpenScrape.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Cuando corre como servicio de Windows, el content root pasa a ser la carpeta
// del ejecutable (no System32), así que appsettings.json se encuentra igual.
// En consola es un no-op.
builder.Host.UseWindowsService();

// El perfil de estrategia es la única configuración que el motor necesita en el servidor.
builder.Services.Configure<StrategyProfile>(builder.Configuration.GetSection("StrategyProfile"));

builder.Services.AddDecisionMaker();
builder.Services.AddScoped<GetTable>();
builder.Services.AddScoped<IPreflopRangeStore, MartenPreflopRangeStore>();
builder.Services.AddDataBase(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddLicenseStore();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<LicenseOptions>(builder.Configuration.GetSection("License"));
builder.Services.AddSingleton<ILicenseKeyHasher, LicenseKeyHasher>();
builder.Services.AddSingleton<ILicenseTokenService, JwtLicenseTokenService>();
builder.Services.AddScoped<ILicenseStore, MartenLicenseStore>();
builder.Services.AddPlanRateLimiting(builder.Configuration);
builder.Services.AddHostedService<LicenseStartupValidation>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwt) =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.SigningKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// Fallar rápido si el perfil quedó incompleto: sin él ninguna decisión es confiable.
StrategyProfileValidator.Validate(app.Services.GetRequiredService<IOptions<StrategyProfile>>().Value);

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
    {
        response.ContentType = "application/json";
        var code = response.StatusCode == StatusCodes.Status401Unauthorized
            ? "unauthorized"
            : "license_revoked";
        await response.WriteAsJsonAsync(new EngineError(code, "Se requiere una licencia válida."));
    }
});

app.UseAuthentication();
app.UseMiddleware<LicenseRevocationMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapLicenseEndpoints();
app.MapDecisionEndpoints();

app.Run();

/// <summary>Marca el ensamblado de la API para que los tests monten el host en proceso.</summary>
public sealed class ApiMarker;
