namespace OpenScrape.Domain.Entities;

/// <summary>
/// Configuración visual del overlay de la mesa de poker.
/// Se carga desde la sección "OverlayConfig" de appsettings.json.
/// </summary>
public class OverlayConfig
{
    public double Opacity { get; set; } = 0.8;
    public float FontSize { get; set; } = 10;
    public float ActionFontSize { get; set; } = 14;
    public int VerticalOffset { get; set; } = 75;
    public double HorizontalOffsetPercent { get; set; } = 0.15;
}
