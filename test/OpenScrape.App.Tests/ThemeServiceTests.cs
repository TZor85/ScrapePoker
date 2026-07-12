using System.Drawing;
using System.Windows.Forms;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del ThemeService. Verifica que los métodos de estilo visual
/// configuran correctamente las propiedades de los controles WinForms.
/// Todos los tests usan controles reales (no mocks) para asegurar
/// compatibilidad con el entorno WinForms.
/// </summary>
[TestFixture]
public class ThemeServiceTests
{
    private IThemeService _themeService = null!;

    [SetUp]
    public void SetUp()
    {
        _themeService = new ThemeService();
    }

    [Test]
    public void InitializeVisualStyles_SetsFormBackColor()
    {
        using var form = new Form();
        _themeService.InitializeVisualStyles(form);
        Assert.That(form.BackColor, Is.EqualTo(AppThemeHelper.BackgroundMain));
    }

    [Test]
    public void InitializeVisualStyles_SetsFormFont()
    {
        using var form = new Form();
        _themeService.InitializeVisualStyles(form);
        Assert.That(form.Font.Name, Is.EqualTo("Segoe UI"));
        Assert.That(form.Font.Size, Is.EqualTo(9f));
    }

    [Test]
    public void ApplyModernTabStyle_SetsTabControlAppearance()
    {
        using var tabControl = new TabControl();
        _themeService.ApplyModernTabStyle(tabControl);
        Assert.That(tabControl.Appearance, Is.EqualTo(TabAppearance.FlatButtons));
        Assert.That(tabControl.Font.Name, Is.EqualTo("Segoe UI"));
    }

    [Test]
    public void ApplyModernTabStyle_AppliesTabStyleByTabName()
    {
        using var tabControl = new TabControl();

        var juegoTab = new TabPage { Name = "tbJuego" };
        var configTab = new TabPage { Name = "tbConfig" };
        var tablesTab = new TabPage { Name = "tbTables" };
        var logsTab = new TabPage { Name = "tbLogs" };
        var historialTab = new TabPage { Name = "tpHistorial" };
        var bankrollTab = new TabPage { Name = "tpBankroll" };

        tabControl.TabPages.Add(juegoTab);
        tabControl.TabPages.Add(configTab);
        tabControl.TabPages.Add(tablesTab);
        tabControl.TabPages.Add(logsTab);
        tabControl.TabPages.Add(historialTab);
        tabControl.TabPages.Add(bankrollTab);

        _themeService.ApplyModernTabStyle(tabControl);

        // All tabs should have the background and padding set
        foreach (TabPage tab in tabControl.TabPages)
        {
            Assert.That(tab.BackColor, Is.EqualTo(AppThemeHelper.BackgroundMain));
            Assert.That(tab.Padding, Is.EqualTo(new Padding(10)));
        }
    }

    [Test]
    public void ApplyGameTabStyle_SetsCardPictureBoxes()
    {
        using var gameTab = new TabPage { Name = "tbJuego" };

        var cardNames = new[] { "pbHeroCard0", "pbHeroCard1", "pbBoard1", "pbBoard2", "pbBoard3", "pbBoard4", "pbBoard5" };
        foreach (var name in cardNames)
        {
            var pb = new PictureBox { Name = name };
            gameTab.Controls.Add(pb);
        }

        _themeService.ApplyGameTabStyle(gameTab);

        foreach (var name in cardNames)
        {
            var pb = gameTab.Controls.Find(name, true).FirstOrDefault() as PictureBox;
            Assert.That(pb, Is.Not.Null);
            Assert.That(pb!.BackColor, Is.EqualTo(AppThemeHelper.BackgroundCard));
            Assert.That(pb.BorderStyle, Is.EqualTo(BorderStyle.None));
            Assert.That(pb.SizeMode, Is.EqualTo(PictureBoxSizeMode.StretchImage));
        }
    }

    [Test]
    public void ApplyConfigTabStyle_SetsTwRegionsBackColor()
    {
        using var configTab = new TabPage { Name = "tbConfig" };
        var tw = new TreeView { Name = "twRegionsConfig" };
        configTab.Controls.Add(tw);

        _themeService.ApplyConfigTabStyle(configTab);

        Assert.That(tw.BackColor, Is.EqualTo(AppThemeHelper.BackgroundCard));
        Assert.That(tw.BorderStyle, Is.EqualTo(BorderStyle.None));
    }

    [Test]
    public void ApplyTablesTabStyle_SetsDgvHandsStyle()
    {
        using var tablesTab = new TabPage { Name = "tbTables" };
        var dgv = new DataGridView { Name = "dgvHands" };
        tablesTab.Controls.Add(dgv);

        _themeService.ApplyTablesTabStyle(tablesTab);

        Assert.That(dgv.EnableHeadersVisualStyles, Is.False);
        Assert.That(dgv.BackgroundColor, Is.EqualTo(AppThemeHelper.BackgroundMain));
        Assert.That(dgv.BorderStyle, Is.EqualTo(BorderStyle.None));
        Assert.That(dgv.GridColor, Is.EqualTo(AppThemeHelper.BorderLight));
    }

    [Test]
    public void ApplyLogsTabStyle_SetsTbResumeStyle()
    {
        using var logsTab = new TabPage { Name = "tbLogs" };
        var tb = new TextBox { Name = "tbResume" };
        logsTab.Controls.Add(tb);

        _themeService.ApplyLogsTabStyle(logsTab);

        Assert.That(tb.BackColor, Is.EqualTo(AppThemeHelper.PrimaryDark));
        Assert.That(tb.Font.Name, Is.EqualTo("Consolas"));
        Assert.That(tb.BorderStyle, Is.EqualTo(BorderStyle.None));
    }

    [Test]
    public void ApplyHistorialTabStyle_SetsDgvSessionsStyle()
    {
        using var historialTab = new TabPage { Name = "tpHistorial" };
        var dgv = new DataGridView { Name = "dgvSessions" };
        historialTab.Controls.Add(dgv);

        _themeService.ApplyHistorialTabStyle(historialTab);

        Assert.That(dgv.EnableHeadersVisualStyles, Is.False);
        Assert.That(dgv.BackgroundColor, Is.EqualTo(AppThemeHelper.BackgroundMain));
        Assert.That(dgv.ColumnHeadersDefaultCellStyle.BackColor, Is.EqualTo(AppThemeHelper.PrimaryDark));
        Assert.That(dgv.ColumnHeadersDefaultCellStyle.ForeColor, Is.EqualTo(Color.White));
    }

    [Test]
    public void ApplyBankrollTabStyle_SetsDgvBankrollStyle()
    {
        using var bankrollTab = new TabPage { Name = "tpBankroll" };
        var dgv = new DataGridView { Name = "dgvBankrollHistory" };
        bankrollTab.Controls.Add(dgv);

        _themeService.ApplyBankrollTabStyle(bankrollTab);

        Assert.That(dgv.EnableHeadersVisualStyles, Is.False);
        Assert.That(dgv.BackgroundColor, Is.EqualTo(AppThemeHelper.BackgroundMain));
        Assert.That(dgv.ColumnHeadersDefaultCellStyle.BackColor, Is.EqualTo(AppThemeHelper.PrimaryDark));
        Assert.That(dgv.ColumnHeadersDefaultCellStyle.ForeColor, Is.EqualTo(Color.White));
        Assert.That(dgv.CellBorderStyle, Is.EqualTo(DataGridViewCellBorderStyle.SingleHorizontal));
    }
}
