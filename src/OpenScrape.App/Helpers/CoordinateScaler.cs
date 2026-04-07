using OpenScrape.App.Services;

namespace OpenScrape.App.Helpers;

public class CoordinateScaler : ICoordinateScaler
{
    private int _referenceWidth;
    private int _referenceHeight;
    private bool _isInitialized;

    public void Initialize(int referenceWidth, int referenceHeight)
    {
        if (!_isInitialized && referenceWidth > 0 && referenceHeight > 0)
        {
            _referenceWidth = referenceWidth;
            _referenceHeight = referenceHeight;
            _isInitialized = true;
        }
    }

    public (int X, int Y, int Width, int Height) ScaleRegion(
        int posX, int posY, int width, int height,
        int currentWidth, int currentHeight)
    {
        if (!_isInitialized || _referenceWidth == 0 || _referenceHeight == 0)
        {
            return (posX, posY, width, height);
        }

        double scaleX = (double)currentWidth / _referenceWidth;
        double scaleY = (double)currentHeight / _referenceHeight;
        double scale = (scaleX + scaleY) / 2.0;

        return (
            (int)(posX * scale),
            (int)(posY * scale),
            (int)(width * scale),
            (int)(height * scale)
        );
    }

    public bool IsInitialized => _isInitialized;
    public int ReferenceWidth => _referenceWidth;
    public int ReferenceHeight => _referenceHeight;
}
