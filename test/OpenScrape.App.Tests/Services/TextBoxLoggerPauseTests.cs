using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// Al navegar el histórico el panel deja de mostrar el stream en vivo. El provider
/// no puede quedar con el destino fijado, pero tampoco se puede llamar dos veces
/// a <c>SetTextBoxTarget</c>: hace falta pausar y reanudar explícitamente.
/// </summary>
[TestFixture]
public class TextBoxLoggerPauseTests
{
    [Test]
    public void PauseLive_DejaDeEscribirEnElPanel()
    {
        using var provider = CreateProvider(out var textBox);
        var logger = provider.CreateLogger("OpenScrape.App.Services.HandProcessor");

        logger.LogInformation("antes de pausar");
        provider.PauseLive();
        logger.LogInformation("mientras navego el historico");

        Assert.Multiple(() =>
        {
            Assert.That(textBox.Text, Does.Contain("antes de pausar"));
            Assert.That(textBox.Text, Does.Not.Contain("mientras navego"));
        });
    }

    [Test]
    public void ResumeLive_VuelveAEscribirEnElPanel()
    {
        using var provider = CreateProvider(out var textBox);
        var logger = provider.CreateLogger("OpenScrape.App.Services.HandProcessor");

        provider.PauseLive();
        logger.LogInformation("perdido");
        provider.ResumeLive();
        logger.LogInformation("de vuelta");

        Assert.Multiple(() =>
        {
            Assert.That(textBox.Text, Does.Not.Contain("perdido"));
            Assert.That(textBox.Text, Does.Contain("de vuelta"));
        });
    }

    private static TextBoxLoggerProvider CreateProvider(out TextBox textBox)
    {
        var provider = new TextBoxLoggerProvider(Options.Create(new TextBoxLoggerOptions()));

        textBox = new TextBox { Multiline = true };
        provider.SetTextBoxTarget(textBox);

        return provider;
    }
}
