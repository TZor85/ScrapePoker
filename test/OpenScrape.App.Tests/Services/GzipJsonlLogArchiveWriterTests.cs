using System.IO.Compression;
using System.Text;
using System.Text.Json;
using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El archivo físico es la única copia que queda cuando el día sale de la base,
/// así que tiene que ser un gzip válido con un JSON por evento y sin restos a medias.
/// </summary>
[TestFixture]
public class GzipJsonlLogArchiveWriterTests
{
    private string _folder = null!;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "openscrape-archive-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Test]
    public async Task Write_CreaElArchivoDelDiaEnLaCarpetaConfigurada()
    {
        var writer = new GzipJsonlLogArchiveWriter(_folder);

        await writer.WriteAsync(Day("2026-10-31", "primero"));

        Assert.That(File.Exists(Path.Combine(_folder, "2026-10-31.jsonl.gz")), Is.True);
    }

    [Test]
    public async Task Write_DejaUnaLineaJsonPorEvento()
    {
        var writer = new GzipJsonlLogArchiveWriter(_folder);

        await writer.WriteAsync(Day("2026-10-31", "primero", "segundo"));

        var lines = ReadLines(Path.Combine(_folder, "2026-10-31.jsonl.gz"));

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(Message(lines[0]), Is.EqualTo("primero"));
            Assert.That(Message(lines[1]), Is.EqualTo("segundo"));
        });
    }

    [Test]
    public async Task Write_NoDejaElArchivoTemporal()
    {
        var writer = new GzipJsonlLogArchiveWriter(_folder);

        await writer.WriteAsync(Day("2026-10-31", "primero"));

        Assert.That(Directory.GetFiles(_folder, "*.tmp"), Is.Empty);
    }

    private static string Message(string line)
    {
        using var document = JsonDocument.Parse(line);
        return document.RootElement.GetProperty("message").GetString()!;
    }

    private static List<string> ReadLines(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);

        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
            lines.Add(line);

        return lines;
    }

    private static LogDay Day(string id, params string[] messages)
    {
        var date = DateTime.ParseExact(id, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        return new LogDay
        {
            Id = id,
            Date = date,
            Entries = messages
                .Select((message, index) => new LogEntry
                {
                    TimestampUtc = date.AddMinutes(index).ToUniversalTime(),
                    Level = "Information",
                    Category = "OpenScrape.App.Services.GameLoggerService",
                    Message = message,
                    HandNumber = 42
                })
                .ToList()
        };
    }
}
