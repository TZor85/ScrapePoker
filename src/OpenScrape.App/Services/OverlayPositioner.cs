using System.Drawing;

using Microsoft.Extensions.Options;

using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación de <see cref="IOverlayPositioner"/>. Aritmética 1:1 de
/// <c>FrmMain.CalculateOverlayPosition</c>.
/// </summary>
public sealed class OverlayPositioner : IOverlayPositioner
{
    private readonly OverlayConfig _config;

    public OverlayPositioner(IOptions<OverlayConfig> config)
    {
        _config = config.Value;
    }

    public Point Calculate(int windowLeft, int windowRight, int windowBottom, int overlayWidth)
    {
        int windowWidth = windowRight - windowLeft;
        int horizontalOffset = (int)(windowWidth * _config.HorizontalOffsetPercent);
        int centerX = windowLeft + (windowWidth / 2);
        int x = centerX - (overlayWidth / 2) - horizontalOffset;
        int y = windowBottom - _config.VerticalOffset;
        return new Point(x, y);
    }
}
