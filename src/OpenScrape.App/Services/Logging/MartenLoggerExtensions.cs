using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Extensiones de <see cref="ILoggingBuilder"/> para registrar el proveedor que
/// persiste los logs en Marten/Postgres.
/// </summary>
public static class MartenLoggerExtensions
{
    public static ILoggingBuilder AddMartenLogger(
        this ILoggingBuilder builder,
        Action<MartenLoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddConfiguration();

        builder.Services.TryAddSingleton<ILogEntryWriter, MartenLogEntryWriter>();

        // El writer se resuelve recién dentro del worker: resolver el
        // IDocumentStore en el constructor del proveedor provocaría un ciclo
        // DocumentStore -> ILoggerFactory -> ILoggerProvider -> DocumentStore.
        builder.Services.TryAddSingleton<MartenLoggerProvider>(sp =>
            new MartenLoggerProvider(
                () => sp.GetRequiredService<ILogEntryWriter>(),
                sp.GetRequiredService<IOptions<MartenLoggerOptions>>()));

        builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<MartenLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions<MartenLoggerOptions, MartenLoggerProvider>(builder.Services);

        if (configure is not null)
            builder.Services.Configure(configure);

        return builder;
    }
}
