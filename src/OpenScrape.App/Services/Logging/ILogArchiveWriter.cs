using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Guarda un día de logs en un archivo físico antes de que salga de la base.
/// La retención y el recorte por presupuesto borran documentos: el archivo es la
/// única copia que queda, así que un fallo de escritura debe impedir el borrado.
/// </summary>
public interface ILogArchiveWriter
{
    Task WriteAsync(LogDay day, CancellationToken cancellationToken = default);
}
