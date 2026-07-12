using System.Drawing;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato para el servicio de captura de pantalla de la ventana de póker.
/// Extrae GetImageWhilePlaying, GetScaledRegion, SaveReferenceDimensionsToConfig
/// y GetJsonValue de FrmMain.
/// </summary>
public interface ICaptureService
{
    /// <summary>
    /// Handle de la ventana de póker a capturar.
    /// Se establece desde FrmMain cuando se selecciona la ventana.
    /// </summary>
    IntPtr WindowHandle { get; set; }

    /// <summary>
    /// Captura la ventana de póker y retorna el screenshot como Bitmap.
    /// Si el handle es IntPtr.Zero, loguea error y retorna null sin lanzar excepción.
    /// </summary>
    Bitmap? CaptureWindow(IntPtr handle);

    /// <summary>
    /// Captura usando <see cref="WindowHandle"/> (parámetro setteado previamente).
    /// Equivalente a llamar CaptureWindow(WindowHandle).
    /// </summary>
    Bitmap? CaptureWindow();

    /// <summary>
    /// Escala las coordenadas de una región según las dimensiones de referencia.
    /// </summary>
    DomainRegion GetScaledRegion(DomainRegion region);

    /// <summary>
    /// Persiste las dimensiones de referencia en la configuración.
    /// </summary>
    void SaveReferenceDimensions(int width, int height);

    /// <summary>
    /// Lee un valor de la configuración JSON por sección y clave.
    /// </summary>
    string? GetJsonValue(string key, string section);
}
