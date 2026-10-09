# Card Recognition Performance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Acelerar el reconocimiento de cartas de la mesa centralizándolo en un único servicio `ICardRecognizer` que evita el round-trip PNG/base64 y el escaneo O(52), sin perder precisión.

**Architecture:** El pipeline por región pasa de `CropImageToBase64` (PNG/Skia + base64 bajo `lock` global) + `CompareCardsBase64` x52, a: extracción directa de píxeles BGRA con `LockBits` → dHash de 64 bits → shortlist por distancia Hamming (`<= 15`) → comparación pixel-a-pixel exacta (`CalculateSimilarity`, tolerancia 10, umbral 90%) sobre la shortlist → fallback a escaneo completo si la shortlist queda vacía. La métrica final nunca la decide el dHash; siempre es `CalculateSimilarity`, idéntica a la actualidad. Se elimina el `lock` global del hot path. `ImageCropperService` se conserva para preview (base64→imagen).

**Tech Stack:** C# / .NET 10, System.Drawing (`LockBits`), NUnit 4.3.2, BenchmarkDotNet 0.15.2, Marten (vía `ICardCacheService`).

---

## Estructura de archivos

**Nuevos (todos en `src/OpenScrape.App/Services/`):**
- `ICardRecognizer.cs` — `CardMatch` record + interfaz `ICardRecognizer`.
- `PixelComparer.cs` — algoritmo de similitud y Hamming (réplica exacta del actual).
- `DHashCalculator.cs` — dHash de 64 bits desde buffer BGRA.
- `RegionPixelExtractor.cs` — recorte directo a buffer BGRA con `LockBits`.
- `CardTemplate.cs` — record de plantilla precomputada.
- `CardTemplateIndex.cs` — carga y precomputa las 52 plantillas desde `ICardCacheService`.
- `CardRecognitionEngine.cs` — implementación del pipeline.

**Modificados:**
- `src/OpenScrape.App/Program.cs` — registro DI + warm-up.
- `src/OpenScrape.App/Services/TableReaderService.cs` — usa `ICardRecognizer`.
- `src/OpenScrape.App/Aplication/UseCases/GetCardsFlopUseCase.cs` / `Turn` / `River` — usan `ICardRecognizer`.
- `src/OpenScrape.App/Forms/Controls/RegionEditorControl.cs` — usa `ICardRecognizer`.
- `test/OpenScrape.App.Tests/TableReaderServiceTests.cs` — fakes y helper actualizados.
- `test/OpenScrape.App.Tests/RegionEditorControlTests.cs` — fake de recognizer.

**Nuevos (tests / benchmark):**
- `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs`
- `src/Tools/BenchmarkSuite1/CardRecognitionBenchmark.cs`

**No se toca:** `ImageCropperService`, `IImageCropperService`, `CardCacheService`, `ICardCacheService`, `CardDTO`, regiones, `IMetricsCollector`. La telemetría usa la categoría existente `TelemetryCategories.OcrCards = "OCR.Cards"`.

---

## Task 1: PixelComparer

**Files:**
- Create: `src/OpenScrape.App/Services/PixelComparer.cs`
- Test: `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs`

- [ ] **Step 1: Write the failing test**

Crea `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs` con:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Tests;

[TestFixture]
public class CardRecognitionEngineTests
{
    [Test]
    public void PixelComparer_IdenticalBuffers_Returns100()
    {
        var buffer = new byte[4 * 4 * 4]; // 4x4
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(i % 256);

        var score = PixelComparer.CalculateSimilarity(buffer, buffer, 4, 4);

        Assert.That(score, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public void PixelComparer_CompletelyDifferent_Returns0()
    {
        var a = new byte[4 * 4 * 4];
        var b = new byte[4 * 4 * 4];
        for (int i = 0; i < a.Length; i += 4)
        {
            a[i] = 0; a[i + 1] = 0; a[i + 2] = 0;
            b[i] = 255; b[i + 1] = 255; b[i + 2] = 255;
        }

        var score = PixelComparer.CalculateSimilarity(a, b, 4, 4);

        Assert.That(score, Is.EqualTo(0.0));
    }

    [Test]
    public void PixelComparer_HammingDistance_CountsDifferingBits()
    {
        Assert.That(PixelComparer.HammingDistance(0UL, 0UL), Is.EqualTo(0));
        Assert.That(PixelComparer.HammingDistance(0b1011UL, 0b1001UL), Is.EqualTo(1));
        Assert.That(PixelComparer.HammingDistance(ulong.MaxValue, 0UL), Is.EqualTo(64));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: FAIL de compilación — `PixelComparer` no existe.

- [ ] **Step 3: Write minimal implementation**

Crea `src/OpenScrape.App/Services/PixelComparer.cs`:

```csharp
namespace OpenScrape.App.Services;

/// <summary>
/// Comparación pixel-a-pixel de buffers BGRA contiguos. Replica exactamente el
/// algoritmo de <see cref="ImageCropperService"/>: tolerancia 10 por canal R/G/B,
/// umbral de similitud 90% y terminación temprana cada 32 píxeles.
/// </summary>
public static class PixelComparer
{
    public const int DefaultTolerance = 10;
    public const double SimilarityThreshold = 90.0;
    private const int VerificationInterval = 32;

    /// <summary>
    /// Devuelve el porcentaje de similitud si es >= 90; en caso contrario 0.
    /// </summary>
    public static double CalculateSimilarity(byte[] pixels1, byte[] pixels2, int width, int height)
    {
        int totalPixels = width * height;
        if (totalPixels == 0)
            return 0;

        long requiredLength = (long)totalPixels * 4;
        if (pixels1.Length < requiredLength || pixels2.Length < requiredLength)
            return 0;

        int similarPixels = 0;
        int minThreshold = (int)(totalPixels * SimilarityThreshold / 100.0);
        int processedPixels = 0;

        int length = totalPixels * 4;
        for (int i = 0; i < length; i += 4)
        {
            if (IsPixelSimilar(pixels1, pixels2, i))
                similarPixels++;

            processedPixels++;

            if (processedPixels % VerificationInterval == 0)
            {
                int remaining = totalPixels - processedPixels;
                if (similarPixels + remaining < minThreshold)
                    return 0;
            }
        }

        double percentage = (double)similarPixels / totalPixels * 100;
        return percentage >= SimilarityThreshold ? percentage : 0;
    }

    /// <summary>
    /// Distancia Hamming entre dos hashes de 64 bits (algoritmo de Brian Kernighan).
    /// </summary>
    public static int HammingDistance(ulong hash1, ulong hash2)
    {
        ulong xor = hash1 ^ hash2;
        int count = 0;
        while (xor != 0)
        {
            xor &= xor - 1;
            count++;
        }
        return count;
    }

    private static bool IsPixelSimilar(byte[] pixels1, byte[] pixels2, int offset)
    {
        return Math.Abs(pixels1[offset + 2] - pixels2[offset + 2]) <= DefaultTolerance && // R
               Math.Abs(pixels1[offset + 1] - pixels2[offset + 1]) <= DefaultTolerance && // G
               Math.Abs(pixels1[offset] - pixels2[offset]) <= DefaultTolerance;           // B
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/PixelComparer.cs test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs
git commit -m "[feat/cards]: Add PixelComparer for card similarity scoring"
```

---

## Task 2: DHashCalculator

**Files:**
- Create: `src/OpenScrape.App/Services/DHashCalculator.cs`
- Test: `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs`

- [ ] **Step 1: Write the failing test**

Añade dentro de `CardRecognitionEngineTests`:

```csharp
    [Test]
    public void DHashCalculator_IdenticalBuffers_ReturnsSameHash()
    {
        var bgra = CreatePatternBgra(30, 40, seed: 1);

        var h1 = DHashCalculator.Compute(bgra, 30, 40);
        var h2 = DHashCalculator.Compute(bgra, 30, 40);

        Assert.That(h1, Is.EqualTo(h2));
    }

    [Test]
    public void DHashCalculator_DifferentPatterns_LargeDistance()
    {
        var ba = CreatePatternBgra(30, 40, seed: 1);
        var bb = CreatePatternBgra(30, 40, seed: 99);

        var distance = PixelComparer.HammingDistance(
            DHashCalculator.Compute(ba, 30, 40),
            DHashCalculator.Compute(bb, 30, 40));

        Assert.That(distance, Is.GreaterThan(0));
    }
```

Y al final de la clase, los helpers (antes del cierre `}` de `CardRecognitionEngineTests`):

```csharp
    private static byte[] CreatePatternBgra(int width, int height, int seed)
    {
        var buffer = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int o = (y * width + x) * 4;
                byte v = (byte)((x * 7 + y * 13 + seed * 53) % 256);
                buffer[o] = (byte)((v + seed) % 256); // B
                buffer[o + 1] = (byte)(255 - v);       // G
                buffer[o + 2] = v;                      // R
                buffer[o + 3] = 255;                    // A
            }
        }
        return buffer;
    }

    private static Bitmap CreatePatternBitmap(int width, int height, int seed)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte v = (byte)((x * 7 + y * 13 + seed * 53) % 256);
                bmp.SetPixel(x, y, Color.FromArgb(255, v, (byte)(255 - v), (byte)((v + seed) % 256)));
            }
        }
        return bmp;
    }

    private static string ToBase64(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: FAIL de compilación — `DHashCalculator` no existe.

- [ ] **Step 3: Write minimal implementation**

Crea `src/OpenScrape.App/Services/DHashCalculator.cs`:

```csharp
namespace OpenScrape.App.Services;

/// <summary>
/// Calcula un dHash de 64 bits a partir de un buffer BGRA contiguo. Hace un downsample
/// por promedio de cajas a 9x8 en escala de grises y compara píxeles horizontalmente
/// adyacentes. La misma implementación se usa para plantillas y regiones, garantizando
/// que imágenes idénticas produzcan distancia Hamming 0.
/// </summary>
public static class DHashCalculator
{
    private const int TargetWidth = 9;
    private const int TargetHeight = 8;

    public static ulong Compute(byte[] bgra, int width, int height)
    {
        if (width <= 0 || height <= 0 || bgra.Length < (long)width * height * 4)
            return 0;

        var gray = new int[TargetWidth * TargetHeight];

        for (int ty = 0; ty < TargetHeight; ty++)
        {
            int y0 = ty * height / TargetHeight;
            int y1 = (ty + 1) * height / TargetHeight;
            if (y1 <= y0) y1 = Math.Min(height, y0 + 1);

            for (int tx = 0; tx < TargetWidth; tx++)
            {
                int x0 = tx * width / TargetWidth;
                int x1 = (tx + 1) * width / TargetWidth;
                if (x1 <= x0) x1 = Math.Min(width, x0 + 1);

                long sum = 0;
                long count = 0;
                for (int y = y0; y < y1; y++)
                {
                    int rowOffset = y * width * 4;
                    for (int x = x0; x < x1; x++)
                    {
                        int offset = rowOffset + x * 4;
                        sum += (bgra[offset] + bgra[offset + 1] + bgra[offset + 2]) / 3;
                        count++;
                    }
                }

                gray[ty * TargetWidth + tx] = count > 0 ? (int)(sum / count) : 0;
            }
        }

        ulong hash = 0;
        int bitIndex = 0;
        for (int y = 0; y < TargetHeight; y++)
        {
            for (int x = 0; x < TargetWidth - 1; x++)
            {
                if (gray[y * TargetWidth + x] > gray[y * TargetWidth + x + 1])
                    hash |= 1UL << bitIndex;
                bitIndex++;
            }
        }

        return hash;
    }
}
```

> Nota: los tests de esta tarea construyen el buffer BGRA a mano (`CreatePatternBgra`), por lo que no dependen de `RegionPixelExtractor` (Task 3).

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: PASS (tests de PixelComparer y DHashCalculator).

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/DHashCalculator.cs test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs
git commit -m "[feat/cards]: Add DHashCalculator for card shortlist prefilter"
```

---

## Task 3: RegionPixelExtractor

**Files:**
- Create: `src/OpenScrape.App/Services/RegionPixelExtractor.cs`
- Test: `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs`

- [ ] **Step 1: Write the failing test**

Añade dentro de `CardRecognitionEngineTests`:

```csharp
    [Test]
    public void RegionPixelExtractor_ExtractsRequestedRectangle()
    {
        using var bmp = new Bitmap(50, 50, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.Green);
        using (var g = Graphics.FromImage(bmp))
            g.FillRectangle(Brushes.Red, new Rectangle(10, 10, 20, 20));

        var buffer = RegionPixelExtractor.ExtractBgra(bmp, 10, 10, 20, 20);

        Assert.That(buffer, Is.Not.Null);
        Assert.That(buffer!.Length, Is.EqualTo(20 * 20 * 4));
        // Primer píxel de la región debe ser rojo (B=0, G=0, R=255)
        Assert.That(buffer[2], Is.EqualTo(255)); // R
        Assert.That(buffer[1], Is.EqualTo(0));   // G
        Assert.That(buffer[0], Is.EqualTo(0));   // B
    }

    [Test]
    public void RegionPixelExtractor_OutOfBounds_ReturnsNull()
    {
        using var bmp = new Bitmap(50, 50, PixelFormat.Format32bppArgb);

        Assert.That(RegionPixelExtractor.ExtractBgra(bmp, 40, 40, 20, 20), Is.Null);
        Assert.That(RegionPixelExtractor.ExtractBgra(bmp, 0, 0, 0, 10), Is.Null);
        Assert.That(RegionPixelExtractor.ExtractBgra(bmp, 0, 0, 10, -1), Is.Null);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: FAIL de compilación — `RegionPixelExtractor` no existe.

- [ ] **Step 3: Write minimal implementation**

Crea `src/OpenScrape.App/Services/RegionPixelExtractor.cs`:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenScrape.App.Services;

/// <summary>
/// Extrae los píxeles BGRA de un rectángulo de un <see cref="Bitmap"/> a un buffer
/// contiguo (sin PNG ni base64). Devuelve <c>null</c> si el rectángulo no es válido
/// o queda fuera de los límites de la imagen.
/// </summary>
public static class RegionPixelExtractor
{
    public static byte[]? ExtractBgra(Bitmap source, int x, int y, int width, int height)
    {
        if (source == null || width <= 0 || height <= 0)
            return null;
        if (x < 0 || y < 0 || x + width > source.Width || y + height > source.Height)
            return null;

        var rect = new Rectangle(x, y, width, height);
        BitmapData? data = null;
        try
        {
            data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int rowBytes = width * 4;
            var buffer = new byte[rowBytes * height];
            for (int row = 0; row < height; row++)
            {
                IntPtr src = IntPtr.Add(data.Scan0, row * data.Stride);
                Marshal.Copy(src, buffer, row * rowBytes, rowBytes);
            }
            return buffer;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (data != null)
                source.UnlockBits(data);
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/RegionPixelExtractor.cs test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs
git commit -m "[feat/cards]: Add RegionPixelExtractor using direct LockBits"
```

---

## Task 4: ICardRecognizer y CardTemplate

**Files:**
- Create: `src/OpenScrape.App/Services/ICardRecognizer.cs`
- Create: `src/OpenScrape.App/Services/CardTemplate.cs`

- [ ] **Step 1: Write the implementation**

Crea `src/OpenScrape.App/Services/ICardRecognizer.cs`:

```csharp
using System.Drawing;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Resultado del reconocimiento de una carta en una región de la imagen.
/// <c>Score</c> 0 indica ausencia de coincidencia; <c>Score</c> >= 90 indica coincidencia fiable.
/// </summary>
public readonly record struct CardMatch(CardDTO? Card, double Score);

/// <summary>
/// Reconoce cartas comparando una región recortada de la mesa contra las plantillas
/// precomputadas. Reemplaza el bucle <c>CropImageToBase64</c> + <c>CompareCardsBase64</c> x52.
/// </summary>
public interface ICardRecognizer
{
    /// <summary>
    /// Reconoce la mejor carta para la región indicada. Devuelve <c>CardMatch(null, 0)</c>
    /// si ninguna plantilla supera el umbral de similitud.
    /// </summary>
    CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height);

    /// <summary>
    /// Devuelve todos los candidatos con <c>Score &gt; 0</c> ordenados de mayor a menor.
    /// Útil para depuración e inspección de la shortlist.
    /// </summary>
    IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height);

    /// <summary>
    /// Indica si la región contiene una carta reconocible (<c>Score &gt; minScore</c>).
    /// </summary>
    bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0);
}
```

Crea `src/OpenScrape.App/Services/CardTemplate.cs`:

```csharp
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Plantilla de carta precomputada: píxeles BGRA contiguos, dimensiones y dHash.
/// Evita decodificar base64 y recomputar el hash en cada comparación.
/// </summary>
public sealed record CardTemplate(CardDTO Card, byte[] Bgra, int Width, int Height, ulong DHash);
```

- [ ] **Step 2: Build para verificar compilación**

Run: `dotnet build src/OpenScrape.App/OpenScrape.App.csproj`
Expected: Build correcto, exit code 0.

- [ ] **Step 3: Commit**

```bash
git add src/OpenScrape.App/Services/ICardRecognizer.cs src/OpenScrape.App/Services/CardTemplate.cs
git commit -m "[feat/cards]: Add ICardRecognizer contract and CardTemplate"
```

---

## Task 5: CardTemplateIndex

**Files:**
- Create: `src/OpenScrape.App/Services/CardTemplateIndex.cs`
- Test: `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs`

- [ ] **Step 1: Write the failing test**

Añade dentro de `CardRecognitionEngineTests`:

```csharp
    [Test]
    public async Task CardTemplateIndex_Initialize_BuildsTemplatesFromCache()
    {
        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 },
            new() { Name = "King Spades", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 2)), Force = 13, Suit = 3 },
            new() { Name = "NoImage", ImageBase64 = null, Force = 0, Suit = 0 }
        };
        var index = new CardTemplateIndex(new FakeCardCacheService(cards));

        await index.InitializeAsync();

        Assert.That(index.IsInitialized, Is.True);
        Assert.That(index.Templates.Count, Is.EqualTo(2), "Las cartas sin ImageBase64 se omiten");
        Assert.That(index.Templates[0].Width, Is.EqualTo(30));
        Assert.That(index.Templates[0].Height, Is.EqualTo(40));
    }

    [Test]
    public async Task CardTemplateIndex_Initialize_IsIdempotent()
    {
        var cards = new List<CardDTO>
        {
            new() { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 }
        };
        var index = new CardTemplateIndex(new FakeCardCacheService(cards));

        await index.InitializeAsync();
        await index.InitializeAsync();

        Assert.That(index.Templates.Count, Is.EqualTo(1));
    }

    [Test]
    public void CardTemplateIndex_Templates_ThrowsBeforeInitialize()
    {
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO>()));

        Assert.That(() => _ = index.Templates, Throws.InvalidOperationException);
    }
```

Añade el fake al final de la clase (antes del cierre `}`):

```csharp
    private sealed class FakeCardCacheService : ICardCacheService
    {
        private readonly List<CardDTO> _cards;
        public FakeCardCacheService(List<CardDTO> cards) => _cards = cards;
        public Task<List<CardDTO>> GetCardsAsync() => Task.FromResult(_cards);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: FAIL de compilación — `CardTemplateIndex` no existe.

- [ ] **Step 3: Write minimal implementation**

Crea `src/OpenScrape.App/Services/CardTemplateIndex.cs`:

```csharp
using System.Drawing;
using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Índice de las plantillas de carta precomputadas (BGRA + dHash + dimensiones).
/// Se inicializa una sola vez desde <see cref="ICardCacheService"/> (Marten).
/// </summary>
public sealed class CardTemplateIndex
{
    private readonly ICardCacheService _cardCacheService;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IReadOnlyList<CardTemplate>? _templates;

    public CardTemplateIndex(ICardCacheService cardCacheService)
    {
        _cardCacheService = cardCacheService ?? throw new ArgumentNullException(nameof(cardCacheService));
    }

    public bool IsInitialized => _templates != null;

    public IReadOnlyList<CardTemplate> Templates
    {
        get
        {
            if (_templates == null)
                throw new InvalidOperationException("CardTemplateIndex no inicializado. Llame a InitializeAsync primero.");
            return _templates;
        }
    }

    public async Task InitializeAsync()
    {
        if (_templates != null)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_templates != null)
                return;

            var cards = await _cardCacheService.GetCardsAsync();
            var templates = new List<CardTemplate>(cards.Count);

            foreach (var card in cards)
            {
                var template = BuildTemplate(card);
                if (template != null)
                    templates.Add(template);
            }

            _templates = templates;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static CardTemplate? BuildTemplate(CardDTO card)
    {
        if (string.IsNullOrEmpty(card.ImageBase64))
            return null;

        try
        {
            var bytes = DecodeBase64(card.ImageBase64);
            using var ms = new MemoryStream(bytes, writable: false);
            using var image = Image.FromStream(ms);
            using var bitmap = new Bitmap(image);

            var bgra = RegionPixelExtractor.ExtractBgra(bitmap, 0, 0, bitmap.Width, bitmap.Height);
            if (bgra == null)
                return null;

            ulong dHash = DHashCalculator.Compute(bgra, bitmap.Width, bitmap.Height);
            return new CardTemplate(card, bgra, bitmap.Width, bitmap.Height, dHash);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] DecodeBase64(string base64)
    {
        ReadOnlySpan<char> span = base64.AsSpan();
        int commaIndex = span.IndexOf(',');
        if (commaIndex >= 0)
            span = span.Slice(commaIndex + 1);
        return Convert.FromBase64String(span.ToString());
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/CardTemplateIndex.cs test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs
git commit -m "[feat/cards]: Add CardTemplateIndex with one-time precompute"
```

---

## Task 6: CardRecognitionEngine

**Files:**
- Create: `src/OpenScrape.App/Services/CardRecognitionEngine.cs`
- Test: `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs`

- [ ] **Step 1: Write the failing test**

Añade dentro de `CardRecognitionEngineTests`:

```csharp
    [Test]
    public async Task Recognize_IdenticalRegion_ReturnsScore100()
    {
        var cardBmp = CreatePatternBitmap(30, 40, seed: 1);
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(cardBmp), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.DrawImage(cardBmp, 20, 30);

        var match = engine.Recognize(screenshot, 20, 30, 30, 40);

        Assert.That(match.Card, Is.Not.Null);
        Assert.That(match.Card!.Name, Is.EqualTo("Ace Hearts"));
        Assert.That(match.Score, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public async Task Recognize_DimensionMismatch_ReturnsNullCard()
    {
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.Clear(Color.Green);

        var match = engine.Recognize(screenshot, 0, 0, 10, 10);

        Assert.That(match.Card, Is.Null);
        Assert.That(match.Score, Is.EqualTo(0.0));
    }

    [Test]
    public async Task IsVisible_ReturnsTrue_When_ScoreAboveMin()
    {
        var cardBmp = CreatePatternBitmap(30, 40, seed: 1);
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(cardBmp), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var engine = new CardRecognitionEngine(index);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.DrawImage(cardBmp, 20, 30);

        Assert.That(engine.IsVisible(screenshot, 20, 30, 30, 40, 80.0), Is.True);
    }

    [Test]
    public async Task Recognize_RecordsTelemetry_OcrCards()
    {
        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = ToBase64(CreatePatternBitmap(30, 40, 1)), Force = 14, Suit = 1 };
        var index = new CardTemplateIndex(new FakeCardCacheService(new List<CardDTO> { card }));
        await index.InitializeAsync();
        var metrics = new FakeMetricsCollector();
        var engine = new CardRecognitionEngine(index, metrics);

        using var screenshot = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(screenshot))
            g.DrawImage(CreatePatternBitmap(30, 40, 1), 20, 30);

        engine.Recognize(screenshot, 20, 30, 30, 40);

        Assert.That(metrics.MeasuredCategories, Contains.Item(TelemetryCategories.OcrCards));
    }
```

Añade el fake al final de la clase:

```csharp
    private sealed class FakeMetricsCollector : IMetricsCollector
    {
        public List<string> MeasuredCategories { get; } = new();

        public ScopedMeasurement Measure(string category)
        {
            MeasuredCategories.Add(category);
            return default;
        }

        public void Record(string category, TimeSpan elapsed) { }
        public ScopedMeasurement MeasureSessionOnly(string category)
        {
            MeasuredCategories.Add(category);
            return default;
        }
        public void RecordSessionOnly(string category, TimeSpan elapsed) { }
        public void StartHand(string handId) { }
        public OpenScrape.Domain.ValueObjects.TelemetryAggregate? EndHand() => null;
        public MetricsSnapshot SnapshotSession() =>
            new(null,
                new Dictionary<string, OpenScrape.Domain.ValueObjects.CategoryStats>(),
                new Dictionary<string, OpenScrape.Domain.ValueObjects.CategoryStats>());
        public void ResetSession() { }
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: FAIL de compilación — `CardRecognitionEngine` no existe.

- [ ] **Step 3: Write minimal implementation**

Crea `src/OpenScrape.App/Services/CardRecognitionEngine.cs`:

```csharp
using System.Drawing;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Services;

/// <summary>
/// Motor de reconocimiento de cartas. Pipeline por región: recorte directo LockBits →
/// dHash → shortlist por distancia Hamming (&lt;= 15) → comparación pixel-a-pixel exacta
/// sobre la shortlist → fallback a escaneo completo si la shortlist queda vacía.
/// La métrica final es siempre <see cref="PixelComparer.CalculateSimilarity"/>; el dHash
/// solo filtra candidatos, nunca decide el ganador.
/// </summary>
public sealed class CardRecognitionEngine : ICardRecognizer
{
    internal const int ShortlistMaxDistance = 15;

    private readonly CardTemplateIndex _templateIndex;
    private readonly IMetricsCollector? _metrics;

    public CardRecognitionEngine(CardTemplateIndex templateIndex, IMetricsCollector? metrics = null)
    {
        _templateIndex = templateIndex ?? throw new ArgumentNullException(nameof(templateIndex));
        _metrics = metrics;
    }

    public CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height)
    {
        var candidates = RankCandidates(screenshot, x, y, width, height);
        return candidates.Count > 0 ? candidates[0] : new CardMatch(null, 0);
    }

    public IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height)
    {
        ScopedMeasurement? measurement = _metrics?.Measure(TelemetryCategories.OcrCards);
        try
        {
            var regionPixels = RegionPixelExtractor.ExtractBgra(screenshot, x, y, width, height);
            if (regionPixels == null)
                return Array.Empty<CardMatch>();

            var templates = _templateIndex.Templates;
            ulong regionHash = DHashCalculator.Compute(regionPixels, width, height);

            var shortlist = new List<CardTemplate>();
            foreach (var template in templates)
            {
                if (template.Width != width || template.Height != height)
                    continue;
                if (PixelComparer.HammingDistance(template.DHash, regionHash) <= ShortlistMaxDistance)
                    shortlist.Add(template);
            }

            var toScore = shortlist.Count > 0 ? shortlist : BuildFallback(templates, width, height);

            var matches = new List<CardMatch>(toScore.Count);
            foreach (var template in toScore)
            {
                double score = PixelComparer.CalculateSimilarity(template.Bgra, regionPixels, width, height);
                if (score > 0)
                    matches.Add(new CardMatch(template.Card, score));
            }

            matches.Sort((a, b) => b.Score.CompareTo(a.Score));
            return matches;
        }
        finally
        {
            measurement?.Dispose();
        }
    }

    public bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0)
    {
        return Recognize(screenshot, x, y, width, height).Score > minScore;
    }

    private static List<CardTemplate> BuildFallback(IReadOnlyList<CardTemplate> templates, int width, int height)
    {
        var fallback = new List<CardTemplate>(templates.Count);
        foreach (var template in templates)
        {
            if (template.Width == width && template.Height == height)
                fallback.Add(template);
        }
        return fallback;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: PASS (todos los tests de la clase).

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/CardRecognitionEngine.cs test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs
git commit -m "[feat/cards]: Add CardRecognitionEngine with dHash shortlist pipeline"
```

---

## Task 7: Paridad con ImageCropperService y telemetría baseline

**Files:**
- Test: `test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs`

- [ ] **Step 1: Write the failing test**

Añade dentro de `CardRecognitionEngineTests`:

```csharp
    [Test]
    public void PixelComparer_MatchesImageCropperService_ForIdenticalImages()
    {
        var bmp = CreatePatternBitmap(30, 40, seed: 7);
        var base64 = ToBase64(bmp);
        var cropper = new ImageCropperService();

        var cropperScore = cropper.CompareCardsBase64(base64, base64);

        var bgra = RegionPixelExtractor.ExtractBgra(bmp, 0, 0, 30, 40)!;
        var comparerScore = PixelComparer.CalculateSimilarity(bgra, bgra, 30, 40);

        Assert.That(cropperScore, Is.EqualTo(100.0).Within(0.001));
        Assert.That(comparerScore, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public void PixelComparer_MatchesImageCropperService_ForDifferentImages()
    {
        var bmpA = CreatePatternBitmap(30, 40, seed: 1);
        var bmpB = CreatePatternBitmap(30, 40, seed: 200);
        var cropper = new ImageCropperService();

        var cropperScore = cropper.CompareCardsBase64(ToBase64(bmpA), ToBase64(bmpB));

        var bgraA = RegionPixelExtractor.ExtractBgra(bmpA, 0, 0, 30, 40)!;
        var bgraB = RegionPixelExtractor.ExtractBgra(bmpB, 0, 0, 30, 40)!;
        var comparerScore = PixelComparer.CalculateSimilarity(bgraA, bgraB, 30, 40);

        Assert.That(cropperScore, Is.EqualTo(0.0));
        Assert.That(comparerScore, Is.EqualTo(0.0));
    }
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~CardRecognitionEngineTests"`
Expected: PASS. Si `PixelComparer_MatchesImageCropperService_ForIdenticalImages` falla en el score de `ImageCropperService`, revisá que el base64 no incluya prefijo data-uri (no lo incluye) y que las dimensiones coincidan.

- [ ] **Step 3: Commit**

```bash
git add test/OpenScrape.App.Tests/CardRecognitionEngineTests.cs
git commit -m "[test/cards]: Add parity tests against ImageCropperService"
```

---

## Task 8: Registro DI y warm-up en Program.cs

**Files:**
- Modify: `src/OpenScrape.App/Program.cs`

- [ ] **Step 1: Registrar los servicios**

En `src/OpenScrape.App/Program.cs`, justo después de la línea 190 (`services.AddSingleton<ICardCacheService>(sp => sp.GetRequiredService<CardCacheService>());`), añade:

```csharp
                    // Reconocimiento de cartas centralizado (plantillas precomputadas + dHash shortlist)
                    services.AddSingleton<CardTemplateIndex>();
                    services.AddSingleton<ICardRecognizer, CardRecognitionEngine>();
```

- [ ] **Step 2: Warm-up antes de Application.Run**

En el mismo archivo, dentro del bloque de inicialización del `CoordinateScaler` (después del `if (...) { scaler.Initialize(...); }` que termina en la línea ~250), añade:

```csharp
            // Precomputar plantillas de cartas antes de arrancar la UI (evita latencia en la primera mano)
            host.Services.GetRequiredService<CardTemplateIndex>().InitializeAsync().GetAwaiter().GetResult();
```

- [ ] **Step 3: Build para verificar**

Run: `dotnet build src/OpenScrape.App/OpenScrape.App.csproj`
Expected: Build correcto, exit code 0.

- [ ] **Step 4: Commit**

```bash
git add src/OpenScrape.App/Program.cs
git commit -m "[feat/cards]: Register CardTemplateIndex and ICardRecognizer in DI"
```

---

## Task 9: Migrar TableReaderService

**Files:**
- Modify: `src/OpenScrape.App/Services/TableReaderService.cs`
- Modify: `test/OpenScrape.App.Tests/TableReaderServiceTests.cs`

- [ ] **Step 1: Actualizar los tests (fakes + helper)**

En `test/OpenScrape.App.Tests/TableReaderServiceTests.cs`:

Reemplaza el helper `CreateService` (líneas 395-412) por:

```csharp
    private static TableReaderService CreateService(
        IRegionLookupCache? regionCache = null,
        IScreenReaderService? screenReader = null,
        ICaptureService? captureService = null,
        ICardRecognizer? cardRecognizer = null,
        IOcrService? ocrService = null,
        ILogger<TableReaderService>? logger = null)
    {
        return new TableReaderService(
            regionCache ?? new FakeRegionLookupCache(),
            screenReader ?? new FakeScreenReaderService(),
            captureService ?? new FakeCaptureService(),
            cardRecognizer ?? new FakeCardRecognizerService(),
            ocrService ?? new FakeOcrService(),
            logger ?? new FakeLogger<TableReaderService>());
    }
```

Reemplaza las clases fake `FakeImageCropperService` (líneas 454-489) y `FakeCardCacheService` (líneas 491-504) por:

```csharp
    private sealed class FakeCardRecognizerService : ICardRecognizer
    {
        private CardMatch _result = new(null, 0);
        private readonly Queue<CardMatch> _queued = new();

        public void SetResult(CardDTO? card, double score) => _result = new CardMatch(card, score);

        public void Enqueue(CardMatch match) => _queued.Enqueue(match);

        public CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height)
            => _queued.Count > 0 ? _queued.Dequeue() : _result;

        public IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height)
        {
            var match = Recognize(screenshot, x, y, width, height);
            return match.Card != null ? new List<CardMatch> { match } : new List<CardMatch>();
        }

        public bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0)
            => Recognize(screenshot, x, y, width, height).Score > minScore;
    }
```

Ahora actualiza los tests que usaban `FakeImageCropperService`/`FakeCardCacheService`/priming:

> Nota de firmas: los siguientes tests ya no necesitan `await`, así que cambiá su firma de `public async Task` a `public void`: `IsBoardCardVisible_ReturnsTrue_When_ImageMatchesAboveThreshold`, `IsBoardCardVisible_ReturnsFalse_When_ImageMatchesBelowThreshold`, `CountVisibleBoardCards_Returns3_When_ThreeCardsVisible` (renombrado), `CountVisibleBoardCards_Returns0_When_NoCardsVisible`, `ObtainCardsPlayer_DoesNotThrow_When_NoRegions`. Conservan `async Task` solo `ObtainCardsPlayer_Populates_HoleCards` (tiene `await`) y los tests de `SetPotValue` ya son `void`.

**Test `IsBoardCardVisible_ReturnsTrue_When_ImageMatchesAboveThreshold`** — Arrange/Act/Assert:

```csharp
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 10, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 85.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "Card1");

        Assert.That(result, Is.True, "Debe retornar true con score > 80");
```

**Test `IsBoardCardVisible_ReturnsFalse_When_ImageMatchesBelowThreshold`** — mismo Arrange pero `recognizer.SetResult(card, 70.0);` y `Assert.That(result, Is.False, ...)`.

**Test `IsBoardCardVisible_ReturnsFalse_When_RegionNotFound`** — no requiere recognizer:

```csharp
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        var service = CreateService(regionCache: regionCache, captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "NonExistent");

        Assert.That(result, Is.False, "Debe retornar false si la región no existe");
```

**Test `IsBoardCardVisible_ReturnsFalse_When_RegionIsNotHash`** — igual que antes pero sin recognizer:

```csharp
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card2", new DomainRegion(
            Category: "Board", Name: "Card2",
            PosX: 50, PosY: 20, Width: 30, Height: 40,
            IsHash: false, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var service = CreateService(regionCache: regionCache, captureService: new FakeCaptureService());

        var result = service.IsBoardCardVisible(screenshot, "Card2");

        Assert.That(result, Is.False, "Debe retornar false para región no-hash");
```

**Test `CountVisibleBoardCards_Returns3_When_ThreeCardsVisible`** — renómbralo a `CountVisibleBoardCards_Returns5_When_AllVisible` y usa recognizer con score 85:

```csharp
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        for (int i = 1; i <= 5; i++)
        {
            regionCache.AddRegion("Board", $"Card{i}", new DomainRegion(
                Category: "Board", Name: $"Card{i}",
                PosX: i * 40, PosY: 20, Width: 30, Height: 40,
                IsHash: true, IsColor: null, IsBoard: null,
                Color: null, IsOnlyNumber: null,
                InactiveUmbral: null, Umbral: null));
        }

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 85.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var count = service.CountVisibleBoardCards(screenshot);

        Assert.That(count, Is.EqualTo(5), "Debe contar 5 cartas visibles (score > 80)");
```

**Test `CountVisibleBoardCards_Returns0_When_NoCardsVisible`**:

```csharp
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("Board", "Card1", new DomainRegion(
            Category: "Board", Name: "Card1",
            PosX: 40, PosY: 20, Width: 30, Height: 40,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var card = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.SetResult(card, 10.0);

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var count = service.CountVisibleBoardCards(screenshot);

        Assert.That(count, Is.EqualTo(0), "Debe retornar 0 si ninguna carta es visible");
```

**Test `ObtainCardsPlayer_Populates_HoleCards`** — usa `Enqueue` con dos cartas:

```csharp
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        regionCache.AddRegion("User", "u0cardface0", new DomainRegion(
            Category: "User", Name: "u0cardface0",
            PosX: 10, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));
        regionCache.AddRegion("User", "u0cardface1", new DomainRegion(
            Category: "User", Name: "u0cardface1",
            PosX: 40, PosY: 10, Width: 25, Height: 35,
            IsHash: true, IsColor: null, IsBoard: null,
            Color: null, IsOnlyNumber: null,
            InactiveUmbral: null, Umbral: null));

        var ace = new CardDTO { Name = "Ace Hearts", ImageBase64 = "aceh-base64", Force = 14, Suit = 1 };
        var king = new CardDTO { Name = "King Spades", ImageBase64 = "kings-base64", Force = 13, Suit = 3 };
        var recognizer = new FakeCardRecognizerService();
        recognizer.Enqueue(new CardMatch(ace, 95.0));
        recognizer.Enqueue(new CardMatch(king, 95.0));

        var service = CreateService(
            regionCache: regionCache,
            cardRecognizer: recognizer,
            captureService: new FakeCaptureService());

        var state = new PlayerGameState();

        await service.ObtainCardsPlayerAsync(screenshot, state);

        Assert.Multiple(() =>
        {
            Assert.That(state.HoleCard1Face, Is.EqualTo("Ace"), "Debe establecer HoleCard1Face");
            Assert.That(state.HoleCard1Rank, Is.EqualTo(14), "Debe establecer HoleCard1Rank");
            Assert.That(state.HoleCard2Face, Is.EqualTo("King"), "Debe establecer HoleCard2Face");
        });
```

**Test `ObtainCardsPlayer_DoesNotThrow_When_NoRegions`**:

```csharp
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var regionCache = new FakeRegionLookupCache();
        var service = CreateService(regionCache: regionCache, captureService: new FakeCaptureService());
        var state = new PlayerGameState();

        Assert.DoesNotThrowAsync(() => service.ObtainCardsPlayerAsync(screenshot, state));
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~TableReaderServiceTests"`
Expected: FAIL de compilación — `TableReaderService` todavía espera `IImageCropperService` e `ICardCacheService`.

- [ ] **Step 3: Migrar TableReaderService**

En `src/OpenScrape.App/Services/TableReaderService.cs`:

Reemplaza los campos (líneas 21-26) y el constructor (líneas 28-44) por:

```csharp
    private readonly IRegionLookupCache _regionLookupCache;
    private readonly IScreenReaderService _screenReader;
    private readonly ICaptureService _captureService;
    private readonly ICardRecognizer _cardRecognizer;
    private readonly IOcrService _ocrService;
    private readonly ILogger<TableReaderService> _logger;

    public TableReaderService(
        IRegionLookupCache regionLookupCache,
        IScreenReaderService screenReader,
        ICaptureService captureService,
        ICardRecognizer cardRecognizer,
        IOcrService ocrService,
        ILogger<TableReaderService> logger)
    {
        _regionLookupCache = regionLookupCache ?? throw new ArgumentNullException(nameof(regionLookupCache));
        _screenReader = screenReader ?? throw new ArgumentNullException(nameof(screenReader));
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _cardRecognizer = cardRecognizer ?? throw new ArgumentNullException(nameof(cardRecognizer));
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
```

Reemplaza `ObtainCardsPlayerAsync` (líneas 151-203) por:

```csharp
    /// <inheritdoc />
    public Task ObtainCardsPlayerAsync(Bitmap screenshot, PlayerGameState state)
    {
        var userRegionsList = _regionLookupCache.GetRegions("User");
        if (userRegionsList == null || screenshot == null)
            return Task.CompletedTask;

        foreach (var region in userRegionsList.Where(w => w.IsHash == true))
        {
            var scaled = ScaleRegion(region);
            var match = _cardRecognizer.Recognize(screenshot, scaled.X, scaled.Y, scaled.Width, scaled.Height);
            if (match.Card is null)
                continue;

            switch (region.Name)
            {
                case "u0cardface0":
                    state.HoleCard1Face = match.Card.Name.Split(" ")[0];
                    state.HoleCard1Rank = match.Card.Force;
                    state.HoleCard1Suit = match.Card.Suit;
                    break;
                case "u0cardface1":
                    state.HoleCard2Face = match.Card.Name.Split(" ")[0];
                    state.HoleCard2Rank = match.Card.Force;
                    state.HoleCard2Suit = match.Card.Suit;
                    break;
            }
        }

        return Task.CompletedTask;
    }
```

Reemplaza `IsBoardCardVisible` (líneas 205-228) por:

```csharp
    /// <inheritdoc />
    public bool IsBoardCardVisible(Bitmap screenshot, string cardRegionName)
    {
        if (screenshot == null)
            return false;

        var cardRegion = _regionLookupCache.GetRegion("Board", cardRegionName);
        if (cardRegion != null && cardRegion.IsHash != true)
            cardRegion = null;
        if (cardRegion == null)
            return false;

        var scaled = ScaleRegion(cardRegion);
        return _cardRecognizer.IsVisible(screenshot, scaled.X, scaled.Y, scaled.Width, scaled.Height, 80.0);
    }
```

- [ ] **Step 4: Limpiar usings y run tests**

Eliminá de `TableReaderService.cs` los usings que queden sin uso (`using OpenScrape.Domain.Dtos;` si ya no se usa `CardDTO`; el `using OpenScrape.App.Helpers;` se mantiene por `PlayerRegionParser`).

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~TableReaderServiceTests"`
Expected: PASS (todos).

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/TableReaderService.cs test/OpenScrape.App.Tests/TableReaderServiceTests.cs
git commit -m "[refactor/cards]: Migrate TableReaderService to ICardRecognizer"
```

---

## Task 10: Migrar UseCases Flop/Turn/River

**Files:**
- Modify: `src/OpenScrape.App/Aplication/UseCases/GetCardsFlopUseCase.cs`
- Modify: `src/OpenScrape.App/Aplication/UseCases/GetCardsTurnUseCase.cs`
- Modify: `src/OpenScrape.App/Aplication/UseCases/GetCardsRiverUseCase.cs`

- [ ] **Step 1: Migrar GetCardsFlopUseCase**

Reemplaza los campos y el constructor (líneas 11-20) por:

```csharp
        private readonly ICardRecognizer _cardRecognizer;
        private readonly ICoordinateScaler _coordinateScaler;

        public GetCardsFlopUseCase(ICardRecognizer cardRecognizer, ICoordinateScaler coordinateScaler)
        {
            _cardRecognizer = cardRecognizer;
            _coordinateScaler = coordinateScaler;
        }
```

Reemplaza el cuerpo de `ExecuteAsync` (líneas 22-100) por:

```csharp
        public Task<GetCardsFlopUseCaseResponse> ExecuteAsync(GetCardsFlopUseCaseRequest request)
        {
            var response = new GetCardsFlopUseCaseResponse();

            var regionTableMap = request.RegionsTableMap?.FirstOrDefault(f => f.Id == "Board");
            if (regionTableMap == null || regionTableMap.Regions == null || request.Image is not Bitmap bitmap)
                return Task.FromResult(response);

            foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
            {
                var (x, y, width, height) = ScaleCoordinates(
                    region.PosX, region.PosY, region.Width, region.Height,
                    request.CurrentImageWidth, request.CurrentImageHeight);

                var match = _cardRecognizer.Recognize(bitmap, x, y, width, height);
                var card = match.Card ?? new CardDTO { Name = string.Empty };

                var name = string.Empty;
                var force = 0;
                var suit = 0;
                var location = 0;

                switch (region.Name)
                {
                    case "Card1":
                        name = card.Name.Split(" ")[0];
                        force = card.Force;
                        suit = card.Suit;
                        location = 1;
                        break;
                    case "Card2":
                        name = card.Name.Split(" ")[0];
                        force = card.Force;
                        suit = card.Suit;
                        location = 2;
                        break;
                    case "Card3":
                        name = card.Name.Split(" ")[0];
                        force = card.Force;
                        suit = card.Suit;
                        location = 3;
                        break;
                    default:
                        break;
                }

                if (response.DataBoard.Count < 3)
                {
                    response.DataBoard.Add(new BoardData
                    {
                        Name = name,
                        Force = force,
                        Suit = suit,
                        Position = BoardPosition.Flop,
                        Location = location
                    });
                }
            }

            return Task.FromResult(response);
        }
```

Añadí `using System.Drawing;` al inicio del archivo (arriba de `using OpenScrape.App.Entities;`).

- [ ] **Step 2: Migrar GetCardsTurnUseCase**

Reemplaza los campos y constructor (líneas 11-20) por:

```csharp
    private readonly ICardRecognizer _cardRecognizer;
    private readonly ICoordinateScaler _coordinateScaler;

    public GetCardsTurnUseCase(ICardRecognizer cardRecognizer, ICoordinateScaler coordinateScaler)
    {
        _cardRecognizer = cardRecognizer;
        _coordinateScaler = coordinateScaler;
    }
```

Dentro de `ExecuteAsync`, reemplaza desde `var cardsImages = await _cardCache.GetCardsAsync();` (línea 30) hasta el cierre del `foreach` (línea 90) por:

```csharp
        foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
        {
            var (x, y, width, height) = ScaleCoordinates(
                region.PosX, region.PosY, region.Width, region.Height,
                request.CurrentImageWidth, request.CurrentImageHeight);

            var match = _cardRecognizer.Recognize(bitmap, x, y, width, height);
            var card = match.Card ?? new CardDTO { Name = string.Empty };

            var name = string.Empty;
            var force = 0;
            var suit = 0;
            var location = 0;

            switch (region.Name)
            {
                case "Card4":
                    name = card.Name.Split(" ")[0];
                    force = card.Force;
                    suit = card.Suit;
                    location = 4;
                    break;
                default:
                    break;
            }

            if (region.Name == "Card4")
            {
                response.DataBoard = request.DataBoard ?? new List<BoardData>();

                if (response.DataBoard?.Where(w => w.Position == BoardPosition.Flop).ToList().Count == 3)
                {
                    response.DataBoard.Add(new BoardData
                    {
                        Name = name,
                        Force = force,
                        Suit = suit,
                        Position = BoardPosition.Turn,
                        Location = location
                    });
                }
            }
        }

        return Task.FromResult(response);
```

Y cambia la firma del método y el guard inicial. La firma pasa a:

```csharp
    public Task<GetCardsTurnUseCaseResponse> ExecuteAsync(GetCardsTurnUseCaseRequest request)
```

El guard de la línea 27 pasa a:

```csharp
        if (regionTableMap == null || regionTableMap.Regions == null || request.Image is not Bitmap bitmap)
            return Task.FromResult(response);
```

Añade también `using System.Drawing;` al inicio.

- [ ] **Step 3: Migrar GetCardsRiverUseCase**

Análogo a Turn. Reemplaza campos/constructor (líneas 11-20) por:

```csharp
    private readonly ICardRecognizer _cardRecognizer;
    private readonly ICoordinateScaler _coordinateScaler;

    public GetCardsRiverUseCase(ICardRecognizer cardRecognizer, ICoordinateScaler coordinateScaler)
    {
        _cardRecognizer = cardRecognizer;
        _coordinateScaler = coordinateScaler;
    }
```

Firma:

```csharp
    public Task<GetCardsRiverUseCaseResponse> ExecuteAsync(GetCardsRiverUseCaseRequest request)
```

Guard (línea 27):

```csharp
        if (regionTableMap == null || regionTableMap.Regions == null || request.Image is not Bitmap bitmap)
            return Task.FromResult(response);
```

Reemplaza desde `var cardsImages = await _cardCache.GetCardsAsync();` (línea 30) hasta el cierre del `foreach` (línea 90) por:

```csharp
        foreach (var region in regionTableMap.Regions.Where(w => w.IsHash == true))
        {
            var (x, y, width, height) = ScaleCoordinates(
                region.PosX, region.PosY, region.Width, region.Height,
                request.CurrentImageWidth, request.CurrentImageHeight);

            var match = _cardRecognizer.Recognize(bitmap, x, y, width, height);
            var card = match.Card ?? new CardDTO { Name = string.Empty };

            var name = string.Empty;
            var force = 0;
            var suit = 0;
            var location = 0;

            switch (region.Name)
            {
                case "Card5":
                    name = card.Name.Split(" ")[0];
                    force = card.Force;
                    suit = card.Suit;
                    location = 5;
                    break;
                default:
                    break;
            }

            if (region.Name == "Card5")
            {
                response.DataBoard = request.DataBoard ?? new List<BoardData>();

                if (response.DataBoard?.Where(w => w.Position == BoardPosition.Turn).ToList().Count == 1)
                {
                    response.DataBoard.Add(new BoardData
                    {
                        Name = name,
                        Force = force,
                        Suit = suit,
                        Position = BoardPosition.River,
                        Location = location
                    });
                }
            }
        }

        return Task.FromResult(response);
```

Añade `using System.Drawing;` al inicio.

- [ ] **Step 4: Build y tests**

Run: `dotnet build src/OpenScrape.App/OpenScrape.App.csproj`
Expected: Build correcto.

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Aplication/UseCases/GetCardsFlopUseCase.cs src/OpenScrape.App/Aplication/UseCases/GetCardsTurnUseCase.cs src/OpenScrape.App/Aplication/UseCases/GetCardsRiverUseCase.cs
git commit -m "[refactor/cards]: Migrate card use cases to ICardRecognizer"
```

---

## Task 11: Migrar RegionEditorControl

**Files:**
- Modify: `src/OpenScrape.App/Forms/Controls/RegionEditorControl.cs`
- Modify: `test/OpenScrape.App.Tests/RegionEditorControlTests.cs`

- [ ] **Step 1: Actualizar tests**

En `test/OpenScrape.App.Tests/RegionEditorControlTests.cs`, reemplaza el helper `CreateRegionEditorControl` (líneas 200-212) por:

```csharp
    private static RegionEditorControl CreateRegionEditorControl()
    {
        return new RegionEditorControl(
            new FakeCaptureServiceRegionEditor(),
            new FakeScreenReaderService(),
            null, // OcrService — no disponible en tests (requiere Tesseract)
            new ImageCropperService(),
            new FakeCardRecognizer(),
            new ColorDetectionService(),
            new FakeRegionLookupCache(),
            new FakeRegionTableMapUseCases(),
            new TestLogger<RegionEditorControl>());
    }
```

Reemplaza la clase `FakeCardCacheService` (líneas 265-271) por:

```csharp
internal sealed class FakeCardRecognizer : ICardRecognizer
{
    public CardMatch Recognize(Bitmap screenshot, int x, int y, int width, int height) => new(null, 0);
    public IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int width, int height) => Array.Empty<CardMatch>();
    public bool IsVisible(Bitmap screenshot, int x, int y, int width, int height, double minScore = 80.0) => false;
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~RegionEditorControlTests"`
Expected: FAIL de compilación — `RegionEditorControl` todavía espera `CardCacheService`.

- [ ] **Step 3: Migrar RegionEditorControl**

En `src/OpenScrape.App/Forms/Controls/RegionEditorControl.cs`:

Reemplaza el campo `_cardCache` (línea 25) por:

```csharp
    private readonly ICardRecognizer _cardRecognizer;
```

Elimina el campo `private List<CardDTO>? _cardsImages;` (línea 55).

Reemplaza el parámetro `CardCacheService cardCache` del constructor (línea 76) por `ICardRecognizer cardRecognizer`, y la asignación (línea 86) por:

```csharp
        _cardRecognizer = cardRecognizer ?? throw new ArgumentNullException(nameof(cardRecognizer));
```

Reemplaza el método `BtnTestCarta_Click` (líneas 388-430) por:

```csharp
    private void BtnTestCarta_Click(object? sender, EventArgs e)
    {
        if (_selectedRegion == null || _pbImage?.Image is not Bitmap bitmap)
        {
            _logger.LogError("No se ha seleccionado una región o la imagen es nula.");
            return;
        }

        try
        {
            var match = _cardRecognizer.Recognize(
                bitmap,
                _selectedRegion.PosX,
                _selectedRegion.PosY,
                _selectedRegion.Width,
                _selectedRegion.Height);

            if (match.Card?.ImageBase64 is { Length: > 0 } imageBase64)
            {
                _pbTestCarta!.Image?.Dispose();
                _pbTestCarta.Image = _imageCropper.Base64ToImage(imageBase64);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al probar carta");
        }
    }
```

- [ ] **Step 4: Limpiar usings y run tests**

Eliminá de `RegionEditorControl.cs` el `using OpenScrape.Domain.Dtos;` si no se usa ya (verificá que no haya otros usos de `CardDTO`). El `_imageCropper` se mantiene para `Base64ToImage`.

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~RegionEditorControlTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Forms/Controls/RegionEditorControl.cs test/OpenScrape.App.Tests/RegionEditorControlTests.cs
git commit -m "[refactor/cards]: Migrate RegionEditorControl to ICardRecognizer"
```

---

## Task 12: Benchmark CardRecognition

**Files:**
- Create: `src/Tools/BenchmarkSuite1/CardRecognitionBenchmark.cs`

- [ ] **Step 1: Escribir el benchmark**

Crea `src/Tools/BenchmarkSuite1/CardRecognitionBenchmark.cs`:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using BenchmarkDotNet.Attributes;
using OpenScrape.App.Services;
using OpenScrape.Domain.Dtos;

namespace BenchmarkSuite1;

/// <summary>
/// Compara el pipeline nuevo (ICardRecognizer: LockBits + dHash shortlist) contra el
/// baseline (CropImageToBase64 + CompareCardsBase64 x52) sobre una región 1920x1080.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CardRecognitionBenchmark
{
    private const int RegionWidth = 60;
    private const int RegionHeight = 80;

    private Bitmap _screenshot = null!;
    private ImageCropperService _cropper = null!;
    private CardRecognitionEngine _engine = null!;
    private List<CardDTO> _cards = null!;
    private string _regionBase64 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _screenshot = new Bitmap(1920, 1080, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(_screenshot))
            g.Clear(Color.FromArgb(30, 120, 60));

        _cards = new List<CardDTO>(52);
        for (int i = 0; i < 52; i++)
        {
            using var cardBmp = CreatePatternBitmap(RegionWidth, RegionHeight, i);
            _cards.Add(new CardDTO
            {
                Name = $"Card{i}",
                ImageBase64 = ToBase64(cardBmp),
                Force = (i % 13) + 2,
                Suit = i / 13
            });
            if (i == 0)
            {
                using var g = Graphics.FromImage(_screenshot);
                g.DrawImage(cardBmp, 100, 100);
            }
        }

        var index = new CardTemplateIndex(new FakeCardCacheService(_cards));
        index.InitializeAsync().GetAwaiter().GetResult();
        _engine = new CardRecognitionEngine(index);

        _cropper = new ImageCropperService();
        using var regionBmp = new Bitmap(RegionWidth, RegionHeight, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(regionBmp))
            g.DrawImage(_screenshot, new Rectangle(0, 0, RegionWidth, RegionHeight), new Rectangle(100, 100, RegionWidth, RegionHeight), GraphicsUnit.Pixel);
        _regionBase64 = ToBase64(regionBmp);
    }

    [Benchmark(Baseline = true)]
    public double CompareBase64All52()
    {
        double best = 0;
        foreach (var card in _cards)
        {
            if (string.IsNullOrEmpty(card.ImageBase64))
                continue;
            var score = _cropper.CompareCardsBase64(card.ImageBase64, _regionBase64);
            if (score > best)
                best = score;
        }
        return best;
    }

    [Benchmark]
    public double RecognizeWithEngine()
    {
        var match = _engine.Recognize(_screenshot, 100, 100, RegionWidth, RegionHeight);
        return match.Score;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _screenshot.Dispose();
    }

    private static Bitmap CreatePatternBitmap(int width, int height, int seed)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte v = (byte)((x * 7 + y * 13 + seed * 53) % 256);
                bmp.SetPixel(x, y, Color.FromArgb(255, v, (byte)(255 - v), (byte)((v + seed) % 256)));
            }
        }
        return bmp;
    }

    private static string ToBase64(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    private sealed class FakeCardCacheService : ICardCacheService
    {
        private readonly List<CardDTO> _cards;
        public FakeCardCacheService(List<CardDTO> cards) => _cards = cards;
        public Task<List<CardDTO>> GetCardsAsync() => Task.FromResult(_cards);
    }
}
```

- [ ] **Step 2: Build del benchmark**

Run: `dotnet build src/Tools/BenchmarkSuite1/BenchmarkSuite1.csproj`
Expected: Build correcto, exit code 0.

- [ ] **Step 3: Commit**

```bash
git add src/Tools/BenchmarkSuite1/CardRecognitionBenchmark.cs
git commit -m "[perf/cards]: Add CardRecognition benchmark"
```

---

## Task 13: Verificación final

**Files:** ninguno (verificación).

- [ ] **Step 1: Build Debug completo**

Run: `dotnet build OpenScrape.sln`
Expected: Exit code 0.

- [ ] **Step 2: Build Release completo**

Run: `dotnet build OpenScrape.sln --configuration Release`
Expected: Exit code 0.

- [ ] **Step 3: Format**

Run: `dotnet format --verify-no-changes OpenScrape.sln`
Expected: Sin cambios. Si reporta, correr `dotnet format OpenScrape.sln` y commitear el formato.

- [ ] **Step 4: Suite completa de tests**

Run: `dotnet test OpenScrape.sln`
Expected: Todos los tests pasan (>= 1173 tests pre-existentes + los nuevos). Warnings <= 75.

- [ ] **Step 5: Commit final (si hubo ajustes de formato/usings)**

```bash
git add -A
git commit -m "[chore/cards]: Final cleanup after card recognition migration"
```

---

## Self-review (cobertura del spec)

- **`ICardRecognizer` con Recognize/RankCandidates/IsVisible + `CardMatch(CardDTO?, double)`:** Task 4.
- **`CardTemplateIndex` precómputo desde ICardCacheService (BGRA + dHash + dimensiones):** Task 5.
- **Pipeline LockBits → dHash → shortlist Hamming <= 15 → CalculateSimilarity sobre shortlist → fallback completo:** Task 6 (`CardRecognitionEngine`). El fallback se activa cuando `shortlist.Count == 0` en `BuildFallback`.
- **Precisión idéntica (tolerance 10, threshold 90, scores 0 o >= 90):** Task 1 (`PixelComparer` réplica exacta) + Task 7 (paridad contra `ImageCropperService`).
- **`IsVisible(80.0)` reproduce `> 80.0`:** Task 9 (`TableReaderService`.
- **Eliminar `lock` global del hot path:** el nuevo motor no usa `ImageCropperService` en el camino caliente; `ImageCropperService` se conserva solo para preview.
- **Migrar los 5 call sites:** TableReaderService (Task 9), Flop/Turn/River (Task 10), RegionEditorControl (Task 11).
- **DI Singleton + warm-up:** Task 8.
- **Telemetría OcrCards:** Task 6 (`CardRecognitionEngine.RankCandidates` mide `TelemetryCategories.OcrCards`).
- **Testing: paridad 52 plantillas / self-match 100 / dims distintas / shortlist / benchmark 1920x1080:** Tasks 1-7 y 12.
- **Fuera de alcance (no tocar):** ML/YOLO, `CardDTO`, regiones, `ImageCropperService`, `ICardCacheService`. Respetado.
