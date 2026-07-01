using System.Drawing;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato mínimo para captura de pantalla de ventana.
/// Extraído para permitir testing de CaptureService sin depender
/// de la clase concreta GetWindowsScreenUseCase.
/// </summary>
public interface IGetWindowsScreenUseCase
{
    /// <summary>
    /// Captura la ventana asociada al handle y retorna la imagen.
    /// </summary>
    Image Execute(IntPtr handle);
}
