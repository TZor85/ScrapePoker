using System.IO.Compression;
using System.Text;
using System.Text.Json;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Escribe cada día como <c>{carpeta}\{yyyy-MM-dd}.jsonl.gz</c>: gzip con un JSON
/// por línea, que se puede inspeccionar con <c>zcat</c> o cualquier visor de gzip.
/// </summary>
/// <remarks>
/// Escribe primero a un temporal y recién entonces renombra. Así nunca queda con
/// nombre definitivo un archivo a medias, que después parecería un día completo.
/// </remarks>
public sealed class GzipJsonlLogArchiveWriter : ILogArchiveWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _folder;

    public GzipJsonlLogArchiveWriter(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("La carpeta de archivo de logs no puede estar vacía.", nameof(folder));

        _folder = folder;
    }

    public async Task WriteAsync(LogDay day, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(day);

        Directory.CreateDirectory(_folder);

        var path = Path.Combine(_folder, day.Id + ".jsonl.gz");
        var temporaryPath = path + ".tmp";

        await using (var file = File.Create(temporaryPath))
        await using (var gzip = new GZipStream(file, CompressionLevel.SmallestSize))
        await using (var writer = new StreamWriter(gzip, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            foreach (var entry in day.Entries)
            {
                var line = JsonSerializer.Serialize(entry, SerializerOptions);
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }

        File.Move(temporaryPath, path, overwrite: true);
    }
}
