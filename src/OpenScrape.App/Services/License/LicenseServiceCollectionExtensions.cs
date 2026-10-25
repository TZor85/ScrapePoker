using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenScrape.App.Services.License;

/// <summary>
/// Registra la licencia del motor. El cliente siempre decide por HTTP, así que
/// el store del token y el cliente de activación no dependen de ningún flag:
/// sin ellos, <see cref="Http.HttpDecisionEngine"/> no puede mandar el Bearer.
/// </summary>
public static class LicenseServiceCollectionExtensions
{
    /// <summary>Archivo donde se persiste la licencia cifrada, junto al ejecutable.</summary>
    public const string LicenseFileName = "openscrape.license";

    public static IServiceCollection AddLicense(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var apiBaseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5000";
        services.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

        services.AddSingleton(_ =>
        {
            var secret = configuration["Encrypter:Key"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException(
                    "Falta Encrypter:Key: sin secreto no se puede cifrar la licencia.");

            return new LicenseTokenStore(
                Path.Combine(AppContext.BaseDirectory, LicenseFileName),
                secret);
        });

        services.AddSingleton<LicenseClient>();

        return services;
    }
}
