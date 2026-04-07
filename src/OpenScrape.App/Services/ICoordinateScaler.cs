namespace OpenScrape.App.Services;

/// <summary>
/// Contrato para el servicio de escalado de coordenadas de regiones.
/// </summary>
public interface ICoordinateScaler
{
    bool IsInitialized { get; }
    int ReferenceWidth { get; }
    int ReferenceHeight { get; }
    void Initialize(int referenceWidth, int referenceHeight);
    (int X, int Y, int Width, int Height) ScaleRegion(int posX, int posY, int width, int height, int currentWidth, int currentHeight);
}
