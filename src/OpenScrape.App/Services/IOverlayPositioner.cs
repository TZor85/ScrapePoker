using System.Drawing;

namespace OpenScrape.App.Services;

/// <summary>
/// Calcula la posición destino del overlay sobre la ventana del casino,
/// centrándolo horizontalmente con offset porcentual configurable y
/// desplazándolo verticalmente desde el borde inferior. Extraído de
/// <c>FrmMain.CalculateOverlayPosition</c>.
/// </summary>
public interface IOverlayPositioner
{
    /// <summary>
    /// Calcula la posición del overlay.
    /// </summary>
    /// <param name="windowLeft">Borde izquierdo de la ventana del casino.</param>
    /// <param name="windowRight">Borde derecho de la ventana del casino.</param>
    /// <param name="windowBottom">Borde inferior de la ventana del casino.</param>
    /// <param name="overlayWidth">Ancho del overlay en píxeles.</param>
    /// <returns>Posición (x, y) en coordenadas de pantalla.</returns>
    Point Calculate(int windowLeft, int windowRight, int windowBottom, int overlayWidth);
}
