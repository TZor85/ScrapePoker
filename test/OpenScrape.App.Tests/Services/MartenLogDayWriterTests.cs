using System.Text.Json;
using OpenScrape.App.Services.Logging;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// Los eventos de un mismo día tienen que quedar en un único documento con todos
/// sus detalles adentro, en vez de un documento por evento. El writer agrupa el
/// lote por día local, acumula sobre el documento existente y evita releerlo
/// mientras el día no cambie.
/// </summary>
[TestFixture]
public class MartenLogDayWriterTests
{
    [Test]
    public async Task WriteAsync_DosLotesDelMismoDia_AcumulaLosEventosEnUnSoloDocumento()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store);

        await writer.WriteAsync(new[] { Entry(At(10, 0), "primero"), Entry(At(10, 1), "segundo") });
        await writer.WriteAsync(new[] { Entry(At(10, 2), "tercero") });

        Assert.Multiple(() =>
        {
            Assert.That(store.Days, Has.Count.EqualTo(1));
            var day = store.Days.Values.Single();
            Assert.That(day.Id, Is.EqualTo(LocalDayId(At(10, 0))));
            Assert.That(
                day.Entries.Select(entry => entry.Message),
                Is.EqualTo(new[] { "primero", "segundo", "tercero" }));
        });
    }

    [Test]
    public async Task WriteAsync_LoteConDosDias_EscribeUnDocumentoPorDia()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store);

        await writer.WriteAsync(new[] { Entry(At(23, 59), "hoy"), Entry(At(0, 1, dayOffset: 1), "mañana") });

        Assert.Multiple(() =>
        {
            Assert.That(store.Days, Has.Count.EqualTo(2));
            Assert.That(store.Days[LocalDayId(At(23, 59))].Entries, Has.Count.EqualTo(1));
            Assert.That(store.Days[LocalDayId(At(0, 1, dayOffset: 1))].Entries, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task WriteAsync_MientrasDureElMismoDia_NoVuelveALeerElDocumento()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store);

        await writer.WriteAsync(new[] { Entry(At(9, 0), "primero") });
        await writer.WriteAsync(new[] { Entry(At(9, 1), "segundo") });

        Assert.Multiple(() =>
        {
            Assert.That(store.LoadCount, Is.EqualTo(1), "El día en curso vive en memoria; releerlo en cada flush es puro costo.");
            Assert.That(store.Days.Values.Single().Entries, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task WriteAsync_AlCambiarDeDia_EscribeEnElDocumentoDelDiaNuevo()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store);

        await writer.WriteAsync(new[] { Entry(At(23, 59), "antes") });
        await writer.WriteAsync(new[] { Entry(At(0, 1, dayOffset: 1), "después") });

        Assert.Multiple(() =>
        {
            Assert.That(store.Days, Has.Count.EqualTo(2));
            Assert.That(store.Days[LocalDayId(At(23, 59))].Entries.Single().Message, Is.EqualTo("antes"));
            Assert.That(store.Days[LocalDayId(At(0, 1, dayOffset: 1))].Entries.Single().Message, Is.EqualTo("después"));
        });
    }

    [Test]
    public async Task DeleteOlderThanAsync_BorraLosDiasViejosYConservaElActual()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store);

        await writer.WriteAsync(new[] { Entry(At(10, 0, dayOffset: -8), "viejo") });
        await writer.WriteAsync(new[] { Entry(At(10, 0), "actual") });

        var borrados = await writer.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-7));

        Assert.Multiple(() =>
        {
            Assert.That(borrados, Is.EqualTo(1), "La retención devuelve cuántos días eliminó.");
            Assert.That(store.Days.Keys, Is.EquivalentTo(new[] { LocalDayId(At(10, 0)) }));
        });
    }

    [Test]
    public async Task WriteAsync_SuperaElMaximoDeEntradasDelDia_DescartaLasMasViejas()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store, new MartenLoggerOptions { MaxEntriesPerDay = 3 });

        await writer.WriteAsync(new[]
        {
            Entry(At(10, 0), "uno"),
            Entry(At(10, 1), "dos"),
            Entry(At(10, 2), "tres"),
            Entry(At(10, 3), "cuatro"),
            Entry(At(10, 4), "cinco")
        });

        Assert.That(
            store.Days.Values.Single().Entries.Select(entry => entry.Message),
            Is.EqualTo(new[] { "tres", "cuatro", "cinco" }),
            "El tope por día conserva lo más reciente: lo viejo agranda la reescritura sin aportar.");
    }

    [Test]
    public async Task WriteAsync_AcumulaElTamanoEstimadoDelDia()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store);

        await writer.WriteAsync(new[] { Entry(At(10, 0), "uno") });
        var trasElPrimero = store.Days.Values.Single().SizeBytes;

        await writer.WriteAsync(new[] { Entry(At(10, 1), "dos") });

        Assert.Multiple(() =>
        {
            Assert.That(trasElPrimero, Is.GreaterThan(0));
            Assert.That(store.Days.Values.Single().SizeBytes, Is.GreaterThan(trasElPrimero));
        });
    }

    [Test]
    public async Task EnforceBudgetAsync_SuperaElPresupuesto_BorraLosDiasMasViejos()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store, new MartenLoggerOptions { MaxTotalBytes = 2_000 });

        for (var offset = -2; offset <= 0; offset++)
            await writer.WriteAsync(new[] { Entry(At(10, 0, offset), new string('x', 500)) });

        var borrados = await writer.EnforceBudgetAsync();

        Assert.Multiple(() =>
        {
            Assert.That(borrados, Is.EqualTo(1),
                "Tres días de ~700 bytes con tope de 2000: alcanza con borrar el más viejo.");
            Assert.That(store.Days.Keys, Does.Not.Contain(LocalDayId(At(10, 0, -2))));
            Assert.That(store.Days.Keys, Does.Contain(LocalDayId(At(10, 0))));
        });
    }

    [Test]
    public async Task EnforceBudgetAsync_DentroDelPresupuesto_NoBorraNada()
    {
        var store = new InMemoryLogDayStore();
        var writer = new MartenLogDayWriter(store, new MartenLoggerOptions { MaxTotalBytes = 100_000 });

        await writer.WriteAsync(new[] { Entry(At(10, 0), "uno") });

        Assert.That(await writer.EnforceBudgetAsync(), Is.Zero);
    }

    [Test]
    public void LogDay_SerializaCadaEventoConSuEstructuraCompleta()
    {
        var day = new LogDay
        {
            Id = "2026-10-31",
            Date = new DateTime(2026, 10, 31),
            Entries =
            {
                new LogEntry
                {
                    TimestampUtc = new DateTime(2026, 10, 31, 13, 0, 0, DateTimeKind.Utc),
                    Level = "Warning",
                    Category = "OpenScrape.App.Forms.FrmMain",
                    EventId = 7,
                    ThreadId = 12,
                    Message = "No se pudo guardar el frame",
                    ExceptionType = "System.IO.IOException",
                    ExceptionMessage = "disco lleno",
                    ExceptionStackTrace = "   at ...",
                    Scopes = { ["SessionId"] = "abc", ["HandNumber"] = "9145345661" }
                }
            }
        };

        var json = JsonSerializer.Serialize(day, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var entry = document.RootElement.GetProperty("entries")[0];

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("id").GetString(), Is.EqualTo("2026-10-31"));
            Assert.That(entry.GetProperty("level").GetString(), Is.EqualTo("Warning"));
            Assert.That(entry.GetProperty("category").GetString(), Is.EqualTo("OpenScrape.App.Forms.FrmMain"));
            Assert.That(entry.GetProperty("message").GetString(), Is.EqualTo("No se pudo guardar el frame"));
            Assert.That(entry.GetProperty("exceptionType").GetString(), Is.EqualTo("System.IO.IOException"));
            Assert.That(entry.GetProperty("scopes").GetProperty("SessionId").GetString(), Is.EqualTo("abc"));
            Assert.That(entry.TryGetProperty("id", out _), Is.False,
                "El evento ya no es un documento: el único Id es el del día.");
        });
    }

    private static LogEntry Entry(DateTime timestampUtc, string message) =>
        new() { TimestampUtc = timestampUtc, Level = "Information", Category = "test", Message = message };

    private static DateTime At(int hour, int minute, int dayOffset = 0) =>
        DateTime.Now.Date.AddDays(dayOffset).AddHours(hour).AddMinutes(minute).ToUniversalTime();

    private static string LocalDayId(DateTime timestampUtc) =>
        timestampUtc.ToLocalTime().ToString("yyyy-MM-dd");

    /// <summary>
    /// Doble en memoria que clona al cargar y al guardar: así el test prueba que el
    /// writer persiste de verdad y no que mutó el mismo objeto que le devolvieron.
    /// </summary>
    private sealed class InMemoryLogDayStore : ILogDayStore
    {
        public Dictionary<string, LogDay> Days { get; } = new();

        public int LoadCount { get; private set; }

        public Task<LogDay?> LoadAsync(string id, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return Task.FromResult(Days.TryGetValue(id, out var day) ? Clone(day) : null);
        }

        public Task SaveAsync(LogDay day, CancellationToken cancellationToken = default)
        {
            Days[day.Id] = Clone(day);
            return Task.CompletedTask;
        }

        public Task<int> DeleteBeforeAsync(DateTime cutoffLocalDate, CancellationToken cancellationToken = default)
        {
            var expired = Days
                .Where(pair => DateTime.Parse(pair.Value.Id) < cutoffLocalDate.Date)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var id in expired)
                Days.Remove(id);

            return Task.FromResult(expired.Count);
        }

        public Task<IReadOnlyList<LogDaySummary>> LoadSummariesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LogDaySummary> summaries = Days.Values
                .Select(day => new LogDaySummary(day.Id, day.Date, day.SizeBytes))
                .ToList();

            return Task.FromResult(summaries);
        }

        private static LogDay Clone(LogDay day) =>
            JsonSerializer.Deserialize<LogDay>(
                JsonSerializer.Serialize(day, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
