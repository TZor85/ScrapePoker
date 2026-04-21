using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests;

[TestFixture]
public class TextBoxLoggerTests
{
    private static TextBoxLoggerProvider BuildProvider(Action<TextBoxLoggerOptions>? configure = null)
    {
        var opts = new TextBoxLoggerOptions();
        configure?.Invoke(opts);
        var provider = new TextBoxLoggerProvider(Options.Create(opts));
        provider.SetScopeProvider(new LoggerExternalScopeProvider());
        return provider;
    }

    private static TextBox NewTextBox()
    {
        var tb = new TextBox { Multiline = true };
        // Forzar creación del handle para que Lines/AppendText funcionen sin Form.
        _ = tb.Handle;
        return tb;
    }

    [Test]
    public void Log_InfoLevel_EscribeLineaFormateada()
    {
        using var provider = BuildProvider();
        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);

        var logger = provider.CreateLogger("OpenScrape.App.Forms.FrmMain");
        logger.LogInformation("Región actualizada: {Region}", "BoardZone");

        Assert.That(tb.Text, Does.Contain("INF FrmMain"));
        Assert.That(tb.Text, Does.Contain("Región actualizada: BoardZone"));
    }

    [Test]
    public void Log_BajoMinimumLevel_NoEscribe()
    {
        using var provider = BuildProvider(o => o.MinimumLevel = LogLevel.Warning);
        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);

        var logger = provider.CreateLogger("Cat");
        logger.LogInformation("ruido");

        Assert.That(logger.IsEnabled(LogLevel.Information), Is.False);
        Assert.That(tb.Text, Is.Empty);
    }

    [Test]
    public void Log_TextBoxDisposed_NoLanza()
    {
        using var provider = BuildProvider();
        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);
        tb.Dispose();

        var logger = provider.CreateLogger("Cat");
        Assert.DoesNotThrow(() => logger.LogError(new InvalidOperationException("boom"), "falló {X}", "y"));
    }

    [Test]
    public void Log_ExcedeMaxLines_RotaBuffer()
    {
        using var provider = BuildProvider(o => o.MaxLines = 5);
        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);

        var logger = provider.CreateLogger("Cat");
        for (int i = 0; i < 8; i++)
            logger.LogInformation("line-{N}", i);

        // Buffer rodante: debe haber exactamente 5 líneas (incluida la última).
        var lines = tb.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.That(lines.Length, Is.LessThanOrEqualTo(5));
        Assert.That(tb.Text, Does.Contain("line-7"));
        Assert.That(tb.Text, Does.Not.Contain("line-0"));
    }

    [Test]
    public void Scope_ConDictionary_SeIncluye()
    {
        using var provider = BuildProvider();
        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);

        var logger = provider.CreateLogger("Cat");
        using (logger.BeginScope(new Dictionary<string, object> { ["HandNumber"] = 42L }))
        {
            logger.LogInformation("flop procesado");
        }

        Assert.That(tb.Text, Does.Contain("HandNumber=42"));
        Assert.That(tb.Text, Does.Contain("flop procesado"));
    }

    [Test]
    public void Scope_Anidado_AcumulaPares()
    {
        using var provider = BuildProvider();
        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);

        var logger = provider.CreateLogger("Cat");
        using (logger.BeginScope(new Dictionary<string, object> { ["SessionId"] = "abc" }))
        using (logger.BeginScope(new Dictionary<string, object> { ["HandNumber"] = 7 }))
        {
            logger.LogInformation("msg");
        }

        Assert.That(tb.Text, Does.Contain("SessionId=abc"));
        Assert.That(tb.Text, Does.Contain("HandNumber=7"));
    }

    [Test]
    public void Provider_SinTarget_DescartaPorDefecto()
    {
        using var provider = BuildProvider();
        var logger = provider.CreateLogger("Cat");

        Assert.DoesNotThrow(() => logger.LogInformation("temprano"));

        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);
        Assert.That(tb.Text, Does.Not.Contain("temprano"));
    }

    [Test]
    public void Provider_BufferingActivo_FlusseaAlRegistrarTarget()
    {
        using var provider = BuildProvider(o => o.BufferUntilTargetReady = true);
        var logger = provider.CreateLogger("Cat");

        logger.LogInformation("uno");
        logger.LogInformation("dos");
        logger.LogInformation("tres");

        var tb = NewTextBox();
        provider.SetTextBoxTarget(tb);

        Assert.That(tb.Text, Does.Contain("uno"));
        Assert.That(tb.Text, Does.Contain("dos"));
        Assert.That(tb.Text, Does.Contain("tres"));

        // Los mensajes posteriores van directos, no se bufferizan.
        logger.LogInformation("cuatro");
        Assert.That(tb.Text, Does.Contain("cuatro"));
    }

    [Test]
    public void AddTextBoxLogger_RegistraProviderSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging(lb => lb.AddTextBoxLogger());
        using var sp = services.BuildServiceProvider();

        var a = sp.GetRequiredService<TextBoxLoggerProvider>();
        var b = sp.GetRequiredService<TextBoxLoggerProvider>();

        Assert.That(a, Is.SameAs(b));

        var providers = sp.GetServices<ILoggerProvider>().OfType<TextBoxLoggerProvider>().ToList();
        Assert.That(providers.Count, Is.GreaterThanOrEqualTo(1));
    }
}
