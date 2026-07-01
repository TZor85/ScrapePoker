namespace OpenScrape.App.Services;

/// <summary>
/// Contrato mínimo para OcrService — solo expone lo que TableReaderService necesita.
/// </summary>
public interface IOcrService
{
    void ClearCache();
}
