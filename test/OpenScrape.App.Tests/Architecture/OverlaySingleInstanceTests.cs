using System.Text.RegularExpressions;

namespace OpenScrape.App.Tests.Architecture;

/// <summary>
/// El overlay visible y el overlay que actualiza UiUpdateService deben ser la misma
/// instancia. Si FrmMain crea un FrmOverlay sin registrarlo en UiUpdateService, el
/// formulario mostrado queda congelado en "Esperando datos..." porque las
/// actualizaciones van a otra instancia.
/// </summary>
[TestFixture]
public class OverlaySingleInstanceTests
{
    [Test]
    public void Cada_Creacion_De_FrmOverlay_Se_Registra_En_UiUpdateService()
    {
        var source = File.ReadAllText(FrmMainPath());

        var creadas = Regex.Matches(source, @"new\s+FrmOverlay\s*\(").Count;
        var registradas = Regex.Matches(source, @"_uiUpdateService\.SetOverlay\s*\(\s*_frmOverlay\s*\)").Count;

        Assert.Multiple(() =>
        {
            Assert.That(creadas, Is.GreaterThan(0), "FrmMain debe crear el overlay");
            Assert.That(registradas, Is.EqualTo(creadas),
                "Cada instancia de FrmOverlay debe registrarse con SetOverlay para que " +
                "UiUpdateService actualice el overlay que el usuario ve.");
        });
    }

    [Test]
    public void BtnWindow_Click_Reutiliza_El_Overlay_Existente_Y_Solo_Lo_Recrea_Si_Esta_Desechado()
    {
        var body = BtnWindowBody(File.ReadAllText(FrmMainPath()));

        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("_frmOverlay is null || _frmOverlay.IsDisposed"),
                "btnWindow_Click debe reutilizar el overlay creado en Load si sigue vivo.");
            Assert.That(body, Does.Contain("_frmOverlay.Show()"));
        });
    }

    private static string BtnWindowBody(string source)
    {
        var start = source.IndexOf("private void btnWindow_Click", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "No se encontró btnWindow_Click en FrmMain.cs");

        var end = source.IndexOf("private void btnGrabScreenshot_Click", start, StringComparison.Ordinal);
        Assert.That(end, Is.GreaterThan(start), "No se encontró el método siguiente a btnWindow_Click");

        return source[start..end];
    }

    private static string FrmMainPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "OpenScrape.App", "Forms", "FrmMain.cs");

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("No se encontró FrmMain.cs desde el directorio de tests.");
    }
}
