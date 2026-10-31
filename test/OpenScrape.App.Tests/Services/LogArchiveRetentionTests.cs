using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// La retención y el recorte por presupuesto borran días de la base. Antes de
/// borrarlos tienen que quedar en el archivo físico: si el archivado falla, el día
/// se conserva y se reintenta en el próximo ciclo.
/// </summary>
[TestFixture]
public class LogArchiveRetentionTests
{
    [Test]
    public async Task Retencion_ArchivaCadaDiaAntesDeBorrarlo()
    {
        var store = new FakeStore(
            Day(DateTime.Today.AddDays(-10), "viejo"),
            Day(DateTime.Today.AddDays(-9), "menos viejo"));
        var archive = new RecordingArchive();
        var writer = new MartenLogDayWriter(store, new MartenLoggerOptions(), archive);
        var ids = store.Days.Select(day => day.Id).ToArray();

        var deleted = await writer.DeleteOlderThanAsync(DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(archive.Archived, Is.EqualTo(ids));
            Assert.That(deleted, Is.EqualTo(2));
            Assert.That(store.Days, Is.Empty);
        });
    }

    [Test]
    public async Task Retencion_SiElArchivadoFalla_NoBorraEseDia()
    {
        var store = new FakeStore(
            Day(DateTime.Today.AddDays(-10), "viejo"),
            Day(DateTime.Today.AddDays(-9), "menos viejo"));
        var archive = new RecordingArchive();
        archive.Failing.Add(store.Days[1].Id);
        var writer = new MartenLogDayWriter(store, new MartenLoggerOptions(), archive);
        var remainingId = store.Days[1].Id;

        var deleted = await writer.DeleteOlderThanAsync(DateTime.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.EqualTo(1));
            Assert.That(store.Days.Select(day => day.Id), Is.EqualTo(new[] { remainingId }));
        });
    }

    [Test]
    public async Task Presupuesto_ArchivaAntesDeBorrar()
    {
        var store = new FakeStore(
            Day(DateTime.Today.AddDays(-2), "viejo", sizeBytes: 800),
            Day(DateTime.Today.AddDays(-1), "menos viejo", sizeBytes: 800));
        var archive = new RecordingArchive();
        var options = new MartenLoggerOptions { MaxTotalBytes = 1000 };
        var writer = new MartenLogDayWriter(store, options, archive);

        var deleted = await writer.EnforceBudgetAsync();

        Assert.Multiple(() =>
        {
            Assert.That(archive.Archived, Has.Count.EqualTo(1));
            Assert.That(deleted, Is.EqualTo(1));
            Assert.That(store.Days, Has.Count.EqualTo(1));
        });
    }

    private static LogDay Day(DateTime date, string message, long sizeBytes = 500) => new()
    {
        Id = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        Date = date,
        SizeBytes = sizeBytes,
        Entries =
        [
            new LogEntry
            {
                TimestampUtc = date.ToUniversalTime(),
                Level = "Information",
                Category = "OpenScrape.App.Services.GameLoggerService",
                Message = message,
                HandNumber = 7
            }
        ]
    };

    private sealed class RecordingArchive : ILogArchiveWriter
    {
        public List<string> Archived { get; } = [];

        public HashSet<string> Failing { get; } = [];

        public Task WriteAsync(LogDay day, CancellationToken cancellationToken = default)
        {
            if (Failing.Contains(day.Id))
                throw new IOException($"No se pudo archivar {day.Id}.");

            Archived.Add(day.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStore(params LogDay[] days) : ILogDayStore
    {
        public List<LogDay> Days { get; } = [.. days];

        public Task<LogDay?> LoadAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Days.SingleOrDefault(day => day.Id == id));

        public Task SaveAsync(LogDay day, CancellationToken cancellationToken = default)
        {
            Days.RemoveAll(existing => existing.Id == day.Id);
            Days.Add(day);
            return Task.CompletedTask;
        }

        public Task<int> DeleteBeforeAsync(DateTime cutoffLocalDate, CancellationToken cancellationToken = default)
            => Task.FromResult(Days.RemoveAll(day => day.Date < cutoffLocalDate));

        public Task<IReadOnlyList<LogDaySummary>> LoadSummariesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LogDaySummary>>(
                Days.Select(day => new LogDaySummary(day.Id, day.Date, day.SizeBytes)).ToList());
    }
}
