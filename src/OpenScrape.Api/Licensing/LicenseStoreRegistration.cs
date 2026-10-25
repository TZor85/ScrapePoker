using Marten;

using Microsoft.Extensions.DependencyInjection;

namespace OpenScrape.Api.Licensing;

/// <summary>
/// Configura el esquema de Marten donde viven las licencias. La API que las
/// valida y el CLI que las emite tienen que usar exactamente la misma
/// configuración: si uno escribe en <c>public</c> y el otro lee de
/// <c>license</c>, la key existe pero no se encuentra.
/// </summary>
public static class LicenseStoreRegistration
{
    public const string SchemaName = "license";

    public static IServiceCollection AddLicenseStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureMarten(options =>
            options.Schema.For<License>().DatabaseSchemaName(SchemaName));

        return services;
    }
}
