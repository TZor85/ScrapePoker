using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Extensiones de <see cref="ILoggingBuilder"/> para registrar el
/// <see cref="TextBoxLoggerProvider"/> como singleton accesible desde DI.
/// </summary>
public static class TextBoxLoggerExtensions
{
    public static ILoggingBuilder AddTextBoxLogger(
        this ILoggingBuilder builder,
        Action<TextBoxLoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddConfiguration();

        builder.Services.TryAddSingleton<TextBoxLoggerProvider>();
        builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<TextBoxLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions<TextBoxLoggerOptions, TextBoxLoggerProvider>(builder.Services);

        if (configure is not null)
            builder.Services.Configure(configure);

        return builder;
    }
}
