# ADR-0016 — Performance: pixel sampling con LockBits, región y card cache singletons

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** ~2026-03-26 (commit `b078777 perf/core: Optimize pixel sampling and region/card cache`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

El game loop iteraba a frecuencia ~5 Hz capturando bitmap completo y sampling pixels para detectar cambios de estado (cartas visibles, dealer position, etc.). Métricas iniciales mostraban:

- `Bitmap.GetPixel(x, y)` se llamaba **9 veces por región** y por iteración. Internamente hace marshaling COM por llamada → ~15-50µs cada GetPixel.
- 17 lookups de `Region` con `_regions.FirstOrDefault(r => r.Name == ...)` — O(n) en cada uso.
- 3 use cases (`GetAllCardsUseCase`, `GetCardByIdUseCase`, etc.) consultaban Marten de manera redundante.
- LINQ `.Average()` en 3 puntos del color detection.
- Marten sessions en algunos use cases sin `await using` → leak de connection pool.

Latencia total game loop: ~250-400ms. Objetivo: <150ms para no perder cambios de calle rápidos.

## Decisión

1. **`LockBits` + buffer raw** en `OcrService` / pixel sampling:
   - `Bitmap.LockBits` una sola vez por bitmap.
   - `ReadPixelFromBuffer(x, y)` lee directo del array `byte[]` (BGR). Zero marshaling.
   - `UnlockBits` al final.
2. **`RegionLookupCache` singleton:** `Dictionary<string, Region>` construido al cargar `RegionTableMap`. Lookups O(1).
3. **`CardCacheService` singleton:** carga las 52 cartas vía Marten una sola vez (lazy + thread-safe). Las cards se mantienen en memoria toda la vida de la app.
4. **LINQ `.Average()` → manual sum/division** en bucle `foreach` con acumuladores. Zero alloc del enumerable.
5. **Marten sessions con `await using`** en todos los use cases que olvidaban disponer.
6. **Helpers de pixel buffer** dedicados, pequeñas funciones inlinables.

## Alternativas consideradas

1. **Reducir frecuencia del game loop.** Considerado. Rechazado: cambios de calle ocurren <500ms y deben detectarse. Mejor optimizar.
2. **Solo capturar regiones necesarias (ROI capture).** Atractivo. Rechazado por complejidad de coordinar Win32 BitBlt sobre regiones — un `BitBlt` total y luego sampling es más simple y la diferencia de coste no es enorme.
3. **GPU compute para image hash.** Sobre-ingeniería. CPU + dHash custom es <5ms por región.
4. **Caché de bitmap deltas (XOR contra captura previa).** Considerado. Rechazado: requiere mantener buffer histórico y la ganancia es pequeña — los cambios reales son ~10% del frame, no un orden de magnitud.
5. **Mantener `GetPixel` y aceptar la latencia.** Rechazado: 250ms por iteración da lag visible al usuario.

## Consecuencias

**Positivas:**

- **Latencia del game loop ~80-150ms** post-optimización (de 250-400ms). 2-3x mejora.
- **Memory pressure GC reducido** — fewer allocations en el hot path.
- **`RegionLookupCache` y `CardCacheService` reusables** desde `FrmMain`, `TableLayoutService`, `ScreenReaderService`. DRY.
- **Sin DB connections leaked** — el `await using` en todos los use cases asegura disposición.
- Permite escalar a >5 Hz si se necesita.

**Negativas:**

- **Path crítico con `unsafe` implícito** (LockBits expone `IntPtr` al buffer). Riesgo de violación de memoria si se calcula offset mal. Mitigado con tests + revisiones.
- **`CardCacheService` singleton mantiene 52 imágenes en memoria.** ~5-10 MB. Aceptable.
- Path de optimización **acopla** a `System.Drawing.Bitmap` y la representación BGR de Windows. Cambiar a SkiaSharp en el futuro requiere reescribir.

**Implicaciones para una migración:**

- En cualquier stack moderno (Rust con `image`, Python con NumPy / Pillow + `numpy.frombuffer`, Go con `image.Image.At` directo a strides) la optimización es replicable.
- El **patrón "región → cache O(1)"** y **"cards cargadas una vez al arrancar"** son invariantes a preservar.

## Referencias

- Commit `b078777 perf/core: Optimize pixel sampling and region/card cache`.
- `src/OpenScrape.App/Services/RegionLookupCache.cs`.
- `src/OpenScrape.App/Services/CardCacheService.cs`.
- `src/OpenScrape.App/Services/OcrService.cs` — LockBits sampling.
- Memoria proyecto: "Performance Quick Wins ✅ (2026-03-26)".
