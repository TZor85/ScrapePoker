using Marten;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Forms.Controls;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.App.Aplication.RegionsTableMap;
using OpenScrape.App.Aplication.RegionsTableMap.GetAll;
using OpenScrape.App.Aplication.RegionsTableMap.Update;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;
using System.Drawing;
using System.Windows.Forms;

namespace OpenScrape.App.Tests;

[TestFixture]
public class RegionEditorControlTests
{
    #region Initialize Tests

    [Test]
    public void Initialize_ConTodosLosControles_NoLanzaExcepcion()
    {
        var control = CreateRegionEditorControl();

        Assert.DoesNotThrow(() =>
        {
            control.Initialize(
                new TreeView(), new PictureBox(),
                new TextBox(), new TextBox(), new TextBox(), new TextBox(),
                new TextBox(), new TextBox(),
                new TextBox(), new TextBox(),
                new Button(), new Button(), new Button(), new Button(),
                new GroupBox(),
                new CheckBox(), new CheckBox(), new CheckBox(), new CheckBox(),
                new Button(), new Button(), new Button(), new Button(),
                new Button(), new Button(), new Button(), new Button(),
                new Button(), new Button(), new Button(), new Button(),
                new ComboBox(),
                new PictureBox(), new TextBox(),
                new TextBox(), new PictureBox(),
                new PictureBox(), new PictureBox(),
                null);
        });
    }

    [Test]
    public void Initialize_ConTwRegionsNull_LanzaArgumentNullException()
    {
        var control = CreateRegionEditorControl();

        Assert.That(() => control.Initialize(
                null!, new PictureBox(),
                new TextBox(), new TextBox(), new TextBox(), new TextBox(),
                new TextBox(), new TextBox(),
                new TextBox(), new TextBox(),
                new Button(), new Button(), new Button(), new Button(),
                new GroupBox(),
                new CheckBox(), new CheckBox(), new CheckBox(), new CheckBox(),
                new Button(), new Button(), new Button(), new Button(),
                new Button(), new Button(), new Button(), new Button(),
                new Button(), new Button(), new Button(), new Button(),
                new ComboBox(),
                new PictureBox(), new TextBox(),
                new TextBox(), new PictureBox(),
                new PictureBox(), new PictureBox(),
                null),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("twRegions"));
    }

    #endregion

    #region LoadTreeViewRegions Tests

    [Test]
    public void LoadTreeViewRegions_ConDatos_PueblaTreeView()
    {
        var control = CreateRegionEditorControl();
        var twRegions = new TreeView();

        control.Initialize(
            twRegions, new PictureBox(),
            new TextBox(), new TextBox(), new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new Button(), new Button(), new Button(), new Button(),
            new GroupBox(),
            new CheckBox(), new CheckBox(), new CheckBox(), new CheckBox(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new ComboBox(),
            new PictureBox(), new TextBox(),
            new TextBox(), new PictureBox(),
            new PictureBox(), new PictureBox(),
            CreateSampleCategories());

        Assert.That(twRegions.Nodes.Count, Is.EqualTo(2));
        Assert.That(twRegions.Nodes[0].Text, Is.EqualTo("Table"));
        Assert.That(twRegions.Nodes[1].Text, Is.EqualTo("User"));
    }

    [Test]
    public void LoadTreeViewRegions_ConDatos_PueblaSubNodos()
    {
        var control = CreateRegionEditorControl();
        var twRegions = new TreeView();

        control.Initialize(
            twRegions, new PictureBox(),
            new TextBox(), new TextBox(), new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new Button(), new Button(), new Button(), new Button(),
            new GroupBox(),
            new CheckBox(), new CheckBox(), new CheckBox(), new CheckBox(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new ComboBox(),
            new PictureBox(), new TextBox(),
            new TextBox(), new PictureBox(),
            new PictureBox(), new PictureBox(),
            CreateSampleCategories());

        Assert.That(twRegions.Nodes[0].Nodes.Count, Is.EqualTo(2));
        Assert.That(twRegions.Nodes[0].Nodes[0].Text, Is.EqualTo("tablehand"));
        Assert.That(twRegions.Nodes[0].Nodes[1].Text, Is.EqualTo("tablename"));
    }

    [Test]
    public void LoadTreeViewRegions_ConNull_NoPueblaTreeView()
    {
        var control = CreateRegionEditorControl();
        var twRegions = new TreeView();

        control.Initialize(
            twRegions, new PictureBox(),
            new TextBox(), new TextBox(), new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new Button(), new Button(), new Button(), new Button(),
            new GroupBox(),
            new CheckBox(), new CheckBox(), new CheckBox(), new CheckBox(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new ComboBox(),
            new PictureBox(), new TextBox(),
            new TextBox(), new PictureBox(),
            new PictureBox(), new PictureBox(),
            null);

        Assert.That(twRegions.Nodes.Count, Is.EqualTo(0));
    }

    [Test]
    public void LoadTreeViewRegions_CalledAgain_LimpiaYNuevamentePuebla()
    {
        var control = CreateRegionEditorControl();
        var twRegions = new TreeView();

        control.Initialize(
            twRegions, new PictureBox(),
            new TextBox(), new TextBox(), new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new TextBox(), new TextBox(),
            new Button(), new Button(), new Button(), new Button(),
            new GroupBox(),
            new CheckBox(), new CheckBox(), new CheckBox(), new CheckBox(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new Button(), new Button(), new Button(), new Button(),
            new ComboBox(),
            new PictureBox(), new TextBox(),
            new TextBox(), new PictureBox(),
            new PictureBox(), new PictureBox(),
            CreateSampleCategories());

        Assert.That(twRegions.Nodes.Count, Is.EqualTo(2));

        // Segunda llamada debe limpiar y recargar
        control.LoadTreeViewRegions(new List<RegionTableMap>
        {
            new() { Id = "Only", Regions = new List<DomainRegion>
            {
                new("Only", "only-region", 0, 0, 10, 10, null, null, null, null, null, null, null)
            }}
        });

        Assert.That(twRegions.Nodes.Count, Is.EqualTo(1));
        Assert.That(twRegions.Nodes[0].Text, Is.EqualTo("Only"));
        Assert.That(twRegions.Nodes[0].Nodes[0].Text, Is.EqualTo("only-region"));
    }

    #endregion

    #region Helper Methods

    private static RegionEditorControl CreateRegionEditorControl()
    {
        return new RegionEditorControl(
            new FakeCaptureServiceRegionEditor(),
            new FakeScreenReaderService(),
            null, // OcrService — no disponible en tests (requiere Tesseract)
            new ImageCropperService(),
            new FakeCardRecognizer(),
            new ColorDetectionService(),
            new FakeRegionLookupCache(),
            new FakeRegionTableMapUseCases(),
            new TestLogger<RegionEditorControl>());
    }

    private static List<RegionTableMap> CreateSampleCategories()
    {
        return new List<RegionTableMap>
        {
            new()
            {
                Id = "Table",
                Regions = new List<DomainRegion>
                {
                    new("Table", "tablehand", 0, 0, 100, 20, null, null, null, null, null, null, null),
                    new("Table", "tablename", 0, 25, 100, 20, null, null, null, null, null, null, null)
                }
            },
            new()
            {
                Id = "User",
                Regions = new List<DomainRegion>
                {
                    new("User", "uAction", 10, 10, 50, 15, null, null, null, null, null, null, null)
                }
            }
        };
    }

    #endregion
}

#region Fakes

internal sealed class FakeCaptureServiceRegionEditor : ICaptureService
{
    public IntPtr WindowHandle { get; set; }
    public Bitmap? CaptureWindow(IntPtr handle) => null;
    public Bitmap? CaptureWindow() => null;
    public DomainRegion GetScaledRegion(DomainRegion region) => region;
    public void SaveReferenceDimensions(int width, int height) { }
    public string? GetJsonValue(string key, string section) => null;
}

internal sealed class FakeScreenReaderService : IScreenReaderService
{
    public string ReadPlayerName(Image screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral) => string.Empty;
    public decimal ReadBetValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber, int? playerNum = null) => 0m;
    public decimal ReadStackValue(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => 0m;
    public string ReadHandNumber(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => string.Empty;
    public string ReadText(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => string.Empty;
    public string ReadTextWithMultipleThresholds(Image screenshot, int x, int y, int w, int h, double? umbral, double? inactiveUmbral, bool? isOnlyNumber) => string.Empty;
    public decimal NormalizeBetValue(decimal rawValue, decimal potSize = 0) => rawValue;
    public decimal NormalizeStackValue(decimal rawValue) => rawValue;
}

internal sealed class FakeCardRecognizer : ICardRecognizer
{
    public CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height) => new(null, 0);
    public IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height) => Array.Empty<CardMatch>();
    public bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0) => false;
}

internal sealed class FakeRegionLookupCache : IRegionLookupCache
{
    public DomainRegion? GetRegion(string mapId, string regionName) => null;
    public List<DomainRegion>? GetRegions(string mapId) => null;
}

internal sealed record FakeRegionTableMapUseCases : RegionTableMapUseCases
{
    private static readonly IDocumentStore _fakeStore
        = DocumentStore.For("host=localhost;database=fake;password=fake;username=fake");

    public FakeRegionTableMapUseCases()
        : base(new GetAllRegionTableMap(_fakeStore), new UpdateRegionTableMap(_fakeStore))
    {
    }
}

internal sealed class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

#endregion
