namespace OpenScrape.App.Services;

/// <summary>
/// Contrato de OcrService — expone lo que consumen TableReaderService y ScreenReaderService.
/// </summary>
public interface IOcrService
{
    void ClearCache();

    OcrResult ExtractTextFromRegionAndDebug(Image sourceImage, int x, int y, int width, int height,
        double umbral = 0, bool onlyNumber = false);
}
