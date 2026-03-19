namespace OpenScrape.App.Helpers;

public static class CoordinateScaler
{
    private static int _referenceWidth;
    private static int _referenceHeight;
    private static bool _isInitialized;

    public static void Initialize(int referenceWidth, int referenceHeight)
    {
        if (!_isInitialized && referenceWidth > 0 && referenceHeight > 0)
        {
            _referenceWidth = referenceWidth;
            _referenceHeight = referenceHeight;
            _isInitialized = true;
        }
    }

    public static (int X, int Y, int Width, int Height) ScaleRegion(
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

    public static bool IsInitialized => _isInitialized;
    public static int ReferenceWidth => _referenceWidth;
    public static int ReferenceHeight => _referenceHeight;

    public static void Reset()
    {
        _isInitialized = false;
        _referenceWidth = 0;
        _referenceHeight = 0;
    }
}
