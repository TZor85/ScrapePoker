using BenchmarkDotNet.Attributes;
using OpenScrape.App.Helpers;
using OpenScrape.App.Services;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.VSDiagnostics;

namespace BenchmarkSuite1;
[SimpleJob]
[CPUUsageDiagnoser]
public class ImageProcessingBenchmark
{
    private ImagePreprocessorHelper _preprocessor;
    private OcrService _ocrService;
    private ImageCropperService _imageCropperService;
    private Bitmap _originalImage;
    private Rectangle _testRegion;
    [GlobalSetup]
    public void Setup()
    {
        _preprocessor = new ImagePreprocessorHelper();
        _ocrService = new OcrService();
        _imageCropperService = new ImageCropperService();
        // Crear una imagen de prueba realista de 1920x1080
        _originalImage = new Bitmap(1920, 1080, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(_originalImage))
        {
            // Simular contenido típico de una mesa de póker
            graphics.FillRectangle(Brushes.DarkGreen, 0, 0, 1920, 1080);
            graphics.FillRectangle(Brushes.White, 100, 100, 200, 50);
            graphics.FillRectangle(Brushes.Gray, 350, 200, 150, 30);
            graphics.FillRectangle(Brushes.Black, 200, 350, 300, 80);
            graphics.FillRectangle(Brushes.Red, 800, 400, 100, 100);
            // Añadir algo de ruido para simular condiciones reales
            var random = new Random(42);
            for (int i = 0; i < 1000; i++)
            {
                var x = random.Next(0, 1920);
                var y = random.Next(0, 1080);
                var color = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));
                graphics.FillRectangle(new SolidBrush(color), x, y, 2, 2);
            }
        }

        _testRegion = new Rectangle(100, 100, 300, 200);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _originalImage?.Dispose();
        _ocrService?.Dispose();
    }

    [Benchmark]
    public void PreprocessImageForOCR()
    {
        // Crear una copia de la imagen original para cada iteración
        using var imageCopy = new Bitmap(_originalImage);
        using var result = _preprocessor.PreprocessImageForOCR(imageCopy, _testRegion);
    // El resultado se eliminará automáticamente al salir del using
    }

    [Benchmark]
    public void CropImageToBase64()
    {
        // Crear una copia de la imagen original para cada iteración
        using var imageCopy = new Bitmap(_originalImage);
        var result = _imageCropperService.CropImageToBase64(imageCopy, _testRegion.X, _testRegion.Y, _testRegion.Width, _testRegion.Height);
    }

    [Benchmark]
    public void ExtractTextFromRegionAndDebug()
    {
        // Crear una copia de la imagen original para cada iteración
        using var imageCopy = new Bitmap(_originalImage);
        using var result = _ocrService.ExtractTextFromRegionAndDebug(imageCopy, _testRegion.X, _testRegion.Y, _testRegion.Width, _testRegion.Height, 128, false);
        result?.Image?.Dispose();
    }

    [Benchmark]
    public void ProcessImageChain()
    {
        // Crear una copia de la imagen original para cada iteración
        using var imageCopy = new Bitmap(_originalImage);
        using var preprocessed = _preprocessor.PreprocessImageForOCR(imageCopy, _testRegion);
        using var ocrResult = _ocrService.ExtractTextFromRegionAndDebug(preprocessed, 0, 0, preprocessed.Width, preprocessed.Height, 128, false);
        ocrResult?.Image?.Dispose();
    }
}