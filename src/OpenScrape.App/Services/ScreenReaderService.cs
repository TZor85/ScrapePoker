using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación de lectura OCR de pantalla. Encapsula preprocesamiento de imagen,
/// consenso multi-lectura y normalización de valores.
/// </summary>
public class ScreenReaderService : IScreenReaderService
{
    private readonly OcrService _ocrService;
    private readonly ILogger<ScreenReaderService> _logger;

    public ScreenReaderService(OcrService ocrService, ILogger<ScreenReaderService> logger)
    {
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region [Métodos públicos]

    public string ReadPlayerName(Image screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral)
    {
        if (screenshot == null) return string.Empty;

        // Lectura 1: umbral estándar
        string read1;
        using (var ocrResult1 = _ocrService.ExtractTextFromRegionAndDebug(
            screenshot, x, y, w, h, umbral, false))
        {
            read1 = CleanOcrPlayerName(ocrResult1.Text);
        }

        // Lectura 2: umbral bajo (inactive) para capturar más colores
        string read2;
        using (var ocrResult2 = _ocrService.ExtractTextFromRegionAndDebug(
            screenshot, x, y, w, h, inactiveUmbral, false))
        {
            read2 = CleanOcrPlayerName(ocrResult2.Text);
        }

        // Consenso: ambas iguales → seguro
        if (!string.IsNullOrEmpty(read1) && read1 == read2)
            return read1;

        // Si solo una tiene resultado → usarla
        if (string.IsNullOrEmpty(read1)) return read2;
        if (string.IsNullOrEmpty(read2)) return read1;

        // Ambas diferentes → la más larga (más probable correcta)
        return read1.Length >= read2.Length ? read1 : read2;
    }

    public decimal ReadBetValue(Image screenshot, int x, int y, int w, int h,
        double? umbral, double? inactiveUmbral, bool? isOnlyNumber, int? playerNum = null)
    {
        if (screenshot == null) return 0;

        OcrResult? firstOcr = null;
        OcrResult? secondOcr = null;

        try
        {
            // Lectura 1: con preprocesamiento + umbral principal
            using (var preprocessed = PreprocessImageForOCR(screenshot, x, y, w, h))
            {
                firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                    preprocessed, 0, 0, w, h,
                    umbral ?? 0, isOnlyNumber ?? false);
            }

            // Lectura 2: con preprocesamiento + umbral inactivo
            using (var preprocessed = PreprocessImageForOCR(screenshot, x, y, w, h))
            {
                secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                    preprocessed, 0, 0, w, h,
                    inactiveUmbral ?? 0, isOnlyNumber ?? false);
            }

            // Lectura 3: directa sin preprocesamiento (fallback)
            using var thirdOcr = _ocrService.ExtractTextFromRegionAndDebug(
                screenshot, x, y, w, h,
                umbral ?? 0, isOnlyNumber ?? false);

            var clean1 = CleanOcrNumericText(firstOcr.Text);
            var clean2 = CleanOcrNumericText(secondOcr.Text);
            var clean3 = CleanOcrNumericText(thirdOcr.Text);

            _logger.LogDebug("[SetBetValue DEBUG] Region p{PlayerNum}bet - Raw: '{R1}' | '{R2}' | '{R3}' => Clean: '{C1}' | '{C2}' | '{C3}'", playerNum, firstOcr.Text, secondOcr.Text, thirdOcr.Text, clean1, clean2, clean3);

            decimal.TryParse(clean1, NumberStyles.Any, CultureInfo.CurrentCulture, out var ocr1);
            decimal.TryParse(clean2, NumberStyles.Any, CultureInfo.CurrentCulture, out var ocr2);
            decimal.TryParse(clean3, NumberStyles.Any, CultureInfo.CurrentCulture, out var ocr3);

            decimal best = 0;
            _logger.LogDebug("[SetBetValue CONSENSUS] ocr1={Ocr1}, ocr2={Ocr2}, ocr3={Ocr3}", ocr1, ocr2, ocr3);

            // Priorizar ocr3 (lectura directa) cuando tiene valor
            if (ocr3 != 0m)
            {
                _logger.LogDebug("[SetBetValue] Usando ocr3={Ocr3} (lectura directa)", ocr3);
                best = ocr3;
            }
            else if (ocr1 == ocr2)
                best = ocr1;
            else if (ocr1 != 0m)
                best = ocr1;
            else if (ocr2 != 0m)
                best = ocr2;

            _logger.LogDebug("[SetBetValue] RETURN best={Best}", best);
            return best;
        }
        finally
        {
            firstOcr?.Dispose();
            secondOcr?.Dispose();
        }
    }

    public decimal ReadStackValue(Image screenshot, int x, int y, int w, int h,
        double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
    {
        if (screenshot == null) return 0;

        OcrResult? firstOcr = null;
        OcrResult? secondOcr = null;

        // Lectura 1: con umbral principal y preprocesamiento
        using (var preprocessed = PreprocessImageForOCR(screenshot, x, y, w, h))
        {
            firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                preprocessed, 0, 0, w, h,
                umbral ?? 0, isOnlyNumber ?? false);
        }

        // Lectura 2: con umbral inactivo y preprocesamiento
        using (var preprocessed = PreprocessImageForOCR(screenshot, x, y, w, h))
        {
            secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                preprocessed, 0, 0, w, h,
                inactiveUmbral ?? 0, isOnlyNumber ?? false);
        }

        // Lectura 3: directa sin preprocesamiento (como fallback)
        using var thirdOcr = _ocrService.ExtractTextFromRegionAndDebug(
            screenshot, x, y, w, h,
            umbral ?? 0, isOnlyNumber ?? false);

        var result = string.Empty;

        if (isOnlyNumber.HasValue == true)
        {
            var cleanFirst = CleanOcrNumericText(firstOcr.Text);
            var cleanSecond = CleanOcrNumericText(secondOcr.Text);
            var cleanThird = CleanOcrNumericText(thirdOcr.Text);

            decimal.TryParse(cleanFirst, NumberStyles.Any, CultureInfo.CurrentCulture, out var ocr1);
            decimal.TryParse(cleanSecond, NumberStyles.Any, CultureInfo.CurrentCulture, out var ocr2);
            decimal.TryParse(cleanThird, NumberStyles.Any, CultureInfo.CurrentCulture, out var ocr3);

            decimal best;
            if (ocr1 == ocr2 && ocr1 == ocr3)
                best = ocr1;
            else if (ocr1 == ocr2)
                best = ocr1;
            else if (ocr1 == ocr3)
                best = ocr1;
            else if (ocr2 == ocr3)
                best = ocr2;
            else
                best = ocr3; // Sin consenso → preferir lectura directa

            result = best.ToString();

            _logger.LogDebug("[STACK] OCR lecturas: '{R1}'→{Ocr1}, '{R2}'→{Ocr2}, '{R3}'→{Ocr3}, best={Best}", firstOcr.Text, ocr1, secondOcr.Text, ocr2, thirdOcr.Text, ocr3, best);
        }

        firstOcr?.Dispose();
        secondOcr?.Dispose();

        if (decimal.TryParse(result, out var stack))
            return stack;

        return 0;
    }

    public string ReadHandNumber(Image screenshot, int x, int y, int w, int h,
        double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
    {
        if (screenshot == null) return string.Empty;

        OcrResult? firstOcr = null;
        OcrResult? secondOcr = null;

        // Lectura 1: con umbral principal y preprocesamiento
        using (var preprocessed = PreprocessImageForOCR(screenshot, x, y, w, h))
        {
            firstOcr = _ocrService.ExtractTextFromRegionAndDebug(
                preprocessed, 0, 0, w, h,
                umbral ?? 0, isOnlyNumber ?? false);
        }

        // Lectura 2: con umbral inactivo y preprocesamiento
        using (var preprocessed = PreprocessImageForOCR(screenshot, x, y, w, h))
        {
            secondOcr = _ocrService.ExtractTextFromRegionAndDebug(
                preprocessed, 0, 0, w, h,
                inactiveUmbral ?? 0, isOnlyNumber ?? false);
        }

        // Lectura 3: directa sin preprocesamiento (fallback)
        using var thirdOcr = _ocrService.ExtractTextFromRegionAndDebug(
            screenshot, x, y, w, h,
            umbral ?? 0, isOnlyNumber ?? false);

        var clean1 = CleanOcrHandNumber(firstOcr.Text);
        var clean2 = CleanOcrHandNumber(secondOcr.Text);
        var clean3 = CleanOcrHandNumber(thirdOcr.Text);

        // Consenso: si 2+ lecturas coinciden, usar ese valor
        string best;
        if (clean1 == clean2 && clean1 == clean3)
            best = clean1;
        else if (clean1 == clean2)
            best = clean1;
        else if (clean1 == clean3)
            best = clean1;
        else if (clean2 == clean3)
            best = clean2;
        else
            best = clean3; // Sin consenso → preferir lectura directa

        _logger.LogDebug("[HAND#] OCR lecturas: '{R1}'→{C1}, '{R2}'→{C2}, '{R3}'→{C3}, best={Best}", firstOcr.Text, clean1, secondOcr.Text, clean2, thirdOcr.Text, clean3, best);

        firstOcr?.Dispose();
        secondOcr?.Dispose();

        return best;
    }

    public string ReadText(Image screenshot, int x, int y, int w, int h,
        double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
    {
        if (screenshot == null) return string.Empty;

        using var ocr = _ocrService.ExtractTextFromRegionAndDebug(
            screenshot, x, y, w, h,
            umbral ?? 0, isOnlyNumber ?? false);

        // Si no se obtiene texto O confianza baja, intentar con umbral inactivo
        if (string.IsNullOrEmpty(ocr.Text) || !ocr.IsHighConfidence)
        {
            using var ocrRetry = _ocrService.ExtractTextFromRegionAndDebug(
                screenshot, x, y, w, h,
                inactiveUmbral ?? 0, isOnlyNumber ?? false);

            if (!string.IsNullOrEmpty(ocrRetry.Text) &&
                (string.IsNullOrEmpty(ocr.Text) || ocrRetry.Confidence > ocr.Confidence))
                return ocrRetry.Text;
        }

        return ocr.Text ?? string.Empty;
    }

    public string ReadTextWithMultipleThresholds(Image screenshot, int x, int y, int w, int h,
        double? umbral, double? inactiveUmbral, bool? isOnlyNumber)
    {
        var thresholds = new List<double?> { umbral, inactiveUmbral, 0.1, 0.2, 0.3, 0.4, 0.5 };

        foreach (var threshold in thresholds.Distinct())
        {
            var text = ReadText(screenshot, x, y, w, h, threshold, threshold, isOnlyNumber);
            if (!string.IsNullOrEmpty(text) && text.Contains("SIT"))
            {
                return text;
            }
        }

        // Si ninguno contiene "SIT", devolver el mejor resultado
        return ReadText(screenshot, x, y, w, h, umbral, inactiveUmbral, isOnlyNumber);
    }

    public decimal NormalizeBetValue(decimal rawValue, decimal potSize = 0)
    {
        if (rawValue <= 0)
            return 0;

        var rawStr = rawValue.ToString();
        bool hasDecimalSeparator = rawStr.Contains(',') || rawStr.Contains('.');

        // Artefacto OCR: "8" espurio al inicio (ej: "850" → "50", "815,50" → "15,50")
        if (hasDecimalSeparator)
        {
            var separator = rawStr.Contains(',') ? ',' : '.';
            var parts = rawStr.Split(separator);

            if (parts[0].Length > 2 && parts[0][0] == '8')
            {
                var corrected = parts[0][1..] + separator + parts[1];
                if (decimal.TryParse(corrected, NumberStyles.Any,
                    CultureInfo.CurrentCulture, out var correctedValue))
                {
                    _logger.LogDebug("[BET] OCR artefacto '8' corregido: {Raw} → {Corrected}", rawStr, corrected);
                    return correctedValue;
                }
            }
        }

        // Separador decimal perdido: bet de 5.93 se lee como 593
        bool suspiciouslyLarge = !hasDecimalSeparator && rawStr.Length >= 3 &&
            (rawValue >= 300 || (potSize > 0 && rawValue > potSize * 5));

        if (suspiciouslyLarge)
        {
            var corrected = rawStr[..^2] + "," + rawStr[^2..];
            if (decimal.TryParse(corrected, NumberStyles.Any,
                CultureInfo.CurrentCulture, out var correctedValue))
            {
                _logger.LogDebug("[BET] OCR separador decimal perdido corregido: {Raw} → {Corrected} (pot={Pot})", rawStr, corrected, potSize);
                return correctedValue;
            }
        }

        return rawValue;
    }

    public decimal NormalizeStackValue(decimal rawValue)
    {
        if (rawValue <= 0)
            return 0;

        var rawStr = rawValue.ToString();
        bool hasDecimalSeparator = rawStr.Contains(',') || rawStr.Contains('.');

        // Solo corregir artefacto "8" cuando ya tiene separador decimal
        if (hasDecimalSeparator)
        {
            var separator = rawStr.Contains(',') ? ',' : '.';
            var parts = rawStr.Split(separator);

            // Artefacto OCR: "8" espurio al inicio (ej: "812,50" → "12,50")
            if (parts[0].Length > 2 && parts[0][0] == '8')
            {
                var corrected = parts[0][1..] + separator + parts[1];
                if (decimal.TryParse(corrected, NumberStyles.Any,
                    CultureInfo.CurrentCulture, out var correctedValue))
                {
                    _logger.LogDebug("[STACK] OCR artefacto '8' corregido: {Raw} → {Corrected}", rawStr, corrected);
                    return correctedValue;
                }
            }
        }

        // Separador decimal perdido: stacks raramente > 300 BB
        if (!hasDecimalSeparator && rawValue >= 500 && rawStr.Length >= 4)
        {
            var corrected = rawStr[..^2] + "," + rawStr[^2..];
            if (decimal.TryParse(corrected, NumberStyles.Any,
                CultureInfo.CurrentCulture, out var correctedValue))
            {
                _logger.LogDebug("[STACK] OCR separador decimal perdido corregido: {Raw} → {Corrected}", rawStr, corrected);
                return correctedValue;
            }
        }

        return rawValue;
    }

    #endregion

    #region [Métodos privados — preprocesamiento]

    private static Bitmap PreprocessImageForOCR(Image sourceImage, int x, int y, int width, int height)
    {
        // Extraer la región
        var regionRect = new Rectangle(x, y, width, height);
        var regionBitmap = new Bitmap(width, height);
        using (var g = Graphics.FromImage(regionBitmap))
        {
            g.DrawImage(sourceImage, new Rectangle(0, 0, width, height), regionRect, GraphicsUnit.Pixel);
        }

        // Convertir a escala de grises
        var grayBitmap = new Bitmap(width, height);
        using (var gGray = Graphics.FromImage(grayBitmap))
        {
            var colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
            });
            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);
            gGray.DrawImage(regionBitmap, new Rectangle(0, 0, width, height),
                0, 0, width, height, GraphicsUnit.Pixel, attributes);
        }
        regionBitmap.Dispose();

        // Binarización con umbral adaptativo simple
        var binaryBitmap = new Bitmap(width, height);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                var pixel = grayBitmap.GetPixel(i, j);
                var gray = (pixel.R + pixel.G + pixel.B) / 3;
                var binaryColor = gray > 128 ? Color.White : Color.Black;
                binaryBitmap.SetPixel(i, j, binaryColor);
            }
        }
        grayBitmap.Dispose();

        return binaryBitmap;
    }

    #endregion

    #region [Métodos privados — limpieza de texto OCR]

    private static string CleanOcrPlayerName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        var cleaned = Regex.Replace(rawName.Trim(), @"[^a-zA-Z0-9_\- ]", "");
        cleaned = cleaned.Trim(' ', '_', '-');

        return cleaned.Length >= 2 ? cleaned : string.Empty;
    }

    private static string CleanOcrNumericText(string? ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return "0";

        var original = ocrText.Trim().ToUpper();

        // Detectar patrones como "1 BB", "2BB" (BB puede verse como 88 o B8)
        var bbRegex = new Regex(@"^(\d+)\s*[IBS]{2,3}$", RegexOptions.IgnoreCase);
        var bbMatch = bbRegex.Match(original);
        if (bbMatch.Success && int.TryParse(bbMatch.Groups[1].Value, out var bbValue))
        {
            return bbValue.ToString();
        }

        // "88" al final que viene de "BB"
        var bb88Regex = new Regex(@"^(\d+)\s*88\s*$");
        var bb88Match = bb88Regex.Match(original);
        if (bb88Match.Success && int.TryParse(bb88Match.Groups[1].Value, out var bbValue88))
        {
            return bbValue88.ToString();
        }

        // Si el texto termina en "88" después de dígitos
        var endsWith88 = Regex.Match(original, @"^(\d+).*88\s*$");
        if (endsWith88.Success && int.TryParse(endsWith88.Groups[1].Value, out var endsValue))
        {
            return endsValue.ToString();
        }

        // Eliminar espacios, letras y caracteres no numéricos excepto separadores decimales
        var cleaned = new string(original.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());

        // Si el resultado es muy largo (más de 6 dígitos), tomar solo los primeros 6
        if (cleaned.Length > 6)
            cleaned = cleaned[..6];

        return string.IsNullOrEmpty(cleaned) ? "0" : cleaned;
    }

    private static string CleanOcrHandNumber(string? ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return string.Empty;

        var cleaned = new string(ocrText.Where(char.IsDigit).ToArray());
        return cleaned;
    }

    #endregion
}
