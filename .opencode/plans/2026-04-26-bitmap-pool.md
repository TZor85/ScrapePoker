# Bitmap Pool Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reducir asignaciones de memoria en hot path del game loop mediante ObjectPool<SKBitmap> y ArrayPool<byte>

**Architecture:** Crear un pool reutilizable de SKBitmap que expanda automáticamente cuando esté lleno. Los bitmaps del pool mantienen dimensiones fixed y se resetean antes de reutilizarse.

**Tech Stack:** .NET System.Buffers.ArrayPool, SkiaSharp.SKBitmap

---

## Task 1: Crear BitmapPool

**Files:**
- Create: `src/OpenScrape.App/Services/BitmapPool.cs`

- [ ] **Step 1: Escribir test para BitmapPool**

```csharp
using Xunit;
using SkiaSharp;

public class BitmapPoolTests
{
    [Fact]
    public void Rent_ReturnsBitmap_WhenPoolHasAvailable()
    {
        var pool = new BitmapPool();
        using var bitmap = pool.Rent(100, 50);
        
        Assert.NotNull(bitmap);
        Assert.Equal(100, bitmap.Width);
        Assert.Equal(50, bitmap.Height);
    }

    [Fact]
    public void Return_ResetsBitmap_ForReuse()
    {
        var pool = new BitmapPool();
        using var bitmap1 = pool.Rent(100, 50);
        
        // Escribir datos en el bitmap
        using var canvas = new SKCanvas(bitmap1);
        canvas.Clear(SKColors.Red);
        
        pool.Return(bitmap1);
        
        // Reutilizar - debería estar limpio
        using var bitmap2 = pool.Rent(100, 50);
        Assert.NotSame(bitmap1, bitmap2); // Nueva instancia o reseteada
    }

    [Fact]
    public void Rent_ExpandsPool_WhenAllInUse()
    {
        var pool = new BitmapPool(maxSize: 2);
        
        using var b1 = pool.Rent(100, 50);
        using var b2 = pool.Rent(100, 50);
        using var b3 = pool.Rent(100, 50); // Debe crear nuevo
        
        Assert.NotNull(b3);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test OpenScrape.App.Tests --filter "BitmapPool" -v`
Expected: FAIL (class not found)

- [ ] **Step 3: Write minimal implementation**

```csharp
using System.Buffers;
using SkiaSharp;

namespace OpenScrape.App.Services;

public class BitmapPool
{
    private readonly ConcurrentBag<SKBitmap> _available = new();
    private readonly int _maxSize;
    private int _count;

    public BitmapPool(int maxSize = 50)
    {
        _maxSize = maxSize;
    }

    public SKBitmap Rent(int width, int height)
    {
        if (_available.TryTake(out var bitmap))
        {
            if (bitmap.Width == width && bitmap.Height == height)
            {
                return bitmap;
            }
            bitmap.Dispose();
        }

        Interlocked.Increment(ref _count);
        return new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
    }

    public void Return(SKBitmap bitmap)
    {
        if (bitmap == null) return;
        
        if (_count <= _maxSize || !_available.TryAdd(bitmap))
        {
            bitmap.Dispose();
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test OpenScrape.App.Tests --filter "BitmapPool" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/BitmapPool.cs
git commit -m "feat: add BitmapPool para reuse en hot path"
```

---

## Task 2: Modificar OcrService.GetCroppedBitmap para usar BitmapPool

**Files:**
- Modify: `src/OpenScrape.App/Services/OcrService.cs:330-352`
- Add DI: constructor injection de BitmapPool

- [ ] **Step 1: Add BitmapPool al constructor de OcrService**

Modificar `OcrService.cs` para inyectar `BitmapPool`:

```csharp
public class OcrService
{
    private readonly BitmapPool _bitmapPool;
    
    public OcrService(/* otros deps */, BitmapPool? bitmapPool = null)
    {
        _bitmapPool = bitmapPool ?? new BitmapPool();
        // ... resto del constructor sin cambios
    }
}
```

- [ ] **Step 2: Modificar GetCroppedBitmap para usar pool**

```csharp
private SKBitmap GetCroppedBitmap(Image sourceImage, int x, int y, int width, int height)
{
    var key = $"{sourceImage.GetHashCode()}_{x}_{y}_{width}_{height}";

    if (_bitmapCache.TryGet(key, out var cachedBitmap))
    {
        return cachedBitmap.Copy();
    }

    using var ms = new MemoryStream();
    sourceImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
    ms.Position = 0;

    using var originalBitmap = SKBitmap.Decode(ms);
    
    // USAR EL POOL en lugar de new SKBitmap
    var croppedBitmap = _bitmapPool.Rent(width, height);
    using var canvas = new SKCanvas(croppedBitmap);

    var sourceRect = new SKRectI(x, y, x + width, y + height);
    canvas.DrawBitmap(originalBitmap, sourceRect, new SKRect(0, 0, width, height));

    _bitmapCache.Set(key, croppedBitmap.Copy());
    return croppedBitmap;
}
```

- [ ] **Step 3: Run tests to verify it passes**

Run: `dotnet test OpenScrape.App.Tests --filter "Ocr" -v`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/OpenScrape.App/Services/OcrService.cs
git commit -m "feat: OcrService ahora usa BitmapPool"
```

---

## Task 3: Modificar ProcessBitmap para usar ArrayPool<byte>

**Files:**
- Modify: `src/OpenScrape.App/Services/OcrService.cs:354-380`

- [ ] **Step 1: Modificar ProcessBitmap para usar ArrayPool**

```csharp
private SKBitmap ProcessBitmap(SKBitmap croppedBitmap, int width, int height, double porcentaje)
{
    var processedBitmap = _bitmapPool.Rent(width, height);  // Reutilizar
    var thresholdValue = (int)(porcentaje * 255);

    var srcSpan = croppedBitmap.GetPixelSpan();
    var dstSpan = processedBitmap.GetPixelSpan();

    var totalBytes = width * height * 4;
    
    // USAR ARRAYPOOL para evitar asignaciones
    var buffer = ArrayPool<byte>.Shared.Rent(totalBytes);
    try
    {
        buffer.AsSpan(0, totalBytes).Clear();
        
        for (int i = 0; i < totalBytes; i += 4)
        {
            int b = srcSpan[i];
            int g = srcSpan[i + 1];
            int r = srcSpan[i + 2];

            int brightness = (r + g + b) / 3;
            byte value = brightness > thresholdValue ? (byte)255 : (byte)0;

            dstSpan[i] = value;
            dstSpan[i + 1] = value;
            dstSpan[i + 2] = value;
            dstSpan[i + 3] = 255;
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    return processedBitmap;
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test OpenScrape.App.Tests --filter "ProcessBitmap" -v`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: ProcessBitmap usa ArrayPool<byte>"
```

---

## Task 4: Modificar ImageCropperService para usar BitmapPool

**Files:**
- Modify: `src/OpenScrape.App/Services/ImageCropperService.cs:43`

- [ ] **Step 1: Modificar ImageCropperService para usar pool**

```csharp
public class ImageCropperService
{
    private readonly BitmapPool _bitmapPool = new();
    
    public string CropImageBase64(Image sourceImage, int x, int y, int width, int height)
    {
        lock (_lock)
        {
            using var ms = new MemoryStream();
            sourceImage.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            using var originalBitmap = SKBitmap.Decode(ms);

            // USAR EL POOL
            using var croppedBitmap = _bitmapPool.Rent(width, height);
            
            using var canvas = new SKCanvas(croppedBitmap);

            var sourceRect = new SKRectI(x, y, x + width, y + height);
            var destRect = new SKRectI(0, 0, width, height);

            canvas.DrawBitmap(originalBitmap, sourceRect, destRect);

            using var image = SKImage.FromBitmap(croppedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var croppedMs = new MemoryStream();
            data.SaveTo(croppedMs);

            return Convert.ToBase64String(croppedMs.ToArray());
        }
    }
}
```

- [ ] **Step 2: Run build**

Run: `dotnet build OpenScrape.App`
Expected: BUILD SUCCESS

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: ImageCropperService usa BitmapPool"
```

---

## Task 5: Integración completa y cleanup

**Files:**
- Modify: `src/OpenScrape.App/Program.cs` - registrar BitmapPool como singleton

- [ ] **Step 1: Registrar BitmapPool en DI**

```csharp
// En Program.cs
builder.Services.AddSingleton<BitmapPool>();
```

- [ ] **Step 2: Pass pool a servicios que lo necesitan**

```csharp
builder.Services.AddSingleton<IOcrService>(sp => 
    new OcrService(sp.GetRequiredService<ILogger<OcrService>>(), 
    sp.GetRequiredService<BitmapPool>()));
```

- [ ] **Step 3: Run full test suite**

Run: `dotnet test OpenScrape.sln`
Expected: ALL TESTS PASS

- [ ] **Step 4: Commit**

```bash
git commit -m "feat: integrar BitmapPool en DI"
```

---

## Execution Option

**Plan complete and saved to `.opencode/plans/2026-04-26-bitmap-pool.md`. Two execution options:**

1. **Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**