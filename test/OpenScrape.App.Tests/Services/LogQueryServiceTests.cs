using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// Consulta de los logs ya persistidos: el panel en vivo no alcanza para volver
/// a una mano concreta, y los documentos por día ya tienen el número de mano.
/// </summary>
[TestFixture]
public class LogQueryServiceTests
{
    [Test]
    public async Task GetDaysAsync_DevuelveLosDiasGuardadosDelMasNuevoAlMasViejo()
    {
        var store = new FakeLogDayStore();
        store.Days.Add(Day("2026-10-29"));
        store.Days.Add(Day("2026-10-31"));
        var service = new LogQueryService(store);

        var days = await service.GetDaysAsync();

        Assert.That(days.Select(day => day.Id), Is.EqualTo(new[] { "2026-10-31", "2026-10-29" }));
    }

    [Test]
    public async Task GetDaysAsync_SinLogsGuardados_DevuelveVacio()
    {
        var service = new LogQueryService(new FakeLogDayStore());

        Assert.That(await service.GetDaysAsync(), Is.Empty);
    }

    [Test]
    public async Task GetHandsAsync_DevuelveLasManosDistintasOrdenadas()
    {
        var store = new FakeLogDayStore();
        store.Days.Add(Day("2026-10-31", 120, 118, 120, 119));
        var service = new LogQueryService(store);

        Assert.That(await service.GetHandsAsync("2026-10-31"), Is.EqualTo(new long[] { 118, 119, 120 }));
    }

    [Test]
    public async Task GetHandsAsync_DiaInexistente_DevuelveVacio()
    {
        var service = new LogQueryService(new FakeLogDayStore());

        Assert.That(await service.GetHandsAsync("2026-01-01"), Is.Empty);
    }

    [Test]
    public async Task GetEntriesAsync_FiltraPorManoYOrdenaPorHora()
    {
        var store = new FakeLogDayStore();
        var day = Day("2026-10-31", 120, 119);
        day.Entries.Reverse();
        store.Days.Add(day);
        var service = new LogQueryService(store);

        var entries = await service.GetEntriesAsync("2026-10-31", 120);

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries.All(entry => entry.HandNumber == 120), Is.True);
            Assert.That(entries.Select(entry => entry.TimestampUtc), Is.Ordered);
        });
    }

    private static LogDay Day(string id, params long[] hands)
    {
        var date = DateTime.ParseExact(id, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var entries = new List<LogEntry>();

        foreach (var hand in hands)
        {
            entries.Add(Entry(date, hand, 0));
            entries.Add(Entry(date, hand, 1));
        }

        return new LogDay
        {
            Id = id,
            Date = date,
            Entries = entries
        };
    }

    private static LogEntry Entry(DateTime date, long hand, int minute) =>
        new()
        {
            TimestampUtc = date.AddMinutes(minute).ToUniversalTime(),
            Level = "Information",
            Category = "OpenScrape.App.Services.GameLoggerService",
            Message = $"mano {hand} minuto {minute}",
            HandNumber = hand
        };

    [Test]
    public async Task GetEntriesAsync_SinMano_DevuelveTodoElDiaOrdenadoPorHora()
    {
        var day = Day("2026-10-31", 120, 118);
        var store = new FakeLogDayStore();
        store.Days.Add(day);
        var service = new LogQueryService(store);

        var entries = await service.GetEntriesAsync(day.Id);

        // Minuto 0: ambas manos. Minuto 1: ambas manos otra vez.
        Assert.That(
            entries.Select(entry => entry.HandNumber).ToArray(),
            Is.EqualTo(new long?[] { 120, 118, 120, 118 }));
    }

    private sealed class FakeLogDayStore : ILogDayStore
    {
        public List<LogDay> Days { get; } = new();

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
