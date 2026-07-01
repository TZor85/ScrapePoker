using System.Drawing;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato mínimo para ImageCropperService — solo expone lo que TableReaderService necesita.
/// </summary>
public interface IImageCropperService
{
    string CropImageToBase64(Image sourceImage, int x, int y, int width, int height);
    double CompareCardsBase64(string? imageBase641, string? imageBase642);
}
