# Card Recognition Performance — Design

- **Fecha**: 2026-10-09
- **Rama**: feature/game-improvement
- **Objetivo**: acelerar el reconocimiento de cartas lo máximo posible **sin perder precisión**,
  centralizando la lógica en un único servicio y añadiendo telemetría.

## Problema

El reconocimiento de cartas ya es por visión (no OCR): se recorta la región y se compara
contra las 52 plantillas `CardDTO`. Pero el camino es lento y frágil:

1. **Round-trip de imagen por cada región**: `ImageCropperService.CropImageToBase64`
   codifica la región a PNG (System.Drawing), la decodifica con SkiaSharp, la vuelve a
   codificar a PNG y la convierte a base64 — todo bajo un `lock(_lock)` **global**
   (`ImageCropperService.cs:30`).
2. **Escaneo O(52) con trabajo pesado evitable**: `TableReaderService.ObtainCardsPlayerAsync`
   recorre las 52 plantillas y, por cada una, llama `CompareCardsBase64` (dHash + pixel-a-pixel).
3. **Sin telemetría**: existe `TelemetryCategories.OcrCards` pero **nadie** lo mide, así que
   no hay baseline.
4. **Lógica duplicada en 5 lugares**: `TableReaderService`, `GetCardsFlopUseCase`,
   `GetCardsTurnUseCase`, `GetCardsRiverUseCase`, `RegionEditorControl`.

## Restricciones

- **Precisión idéntica**: la decisión final debe seguir usando el algoritmo exacto actual
  (`CalculateSimilarity`: comparación pixel-a-pixel con `DefaultTolerance = 10`,
  terminación temprana y `SimilarityThreshold = 90.0`).
- **Compatibilidad funcional**: mismos resultados que el código actual con las capturas de
  `output/samples`.
- No se agregan dependencias nuevas (no OpenCvSharp/YOLO en este cambio).

## Solución (Enfoque 1 + 2)

Eliminar los round-trips de imagen del hot path y reducir el conjunto de candidatos con un
índice por dHash, dejando la métrica final intacta.

### Componentes

- **`ICardRecognizer`** (nuevo, `src/OpenScrape.App/Services`) — contrato único:
  ```csharp
  public sealed record CardMatch(CardDTO? Card, double Score);

  public interface ICardRecognizer
  {
      CardMatch Recognize(Bitmap screenshot, int x, int y, int w, int h);
      IReadOnlyList<CardMatch> RankCandidates(Bitmap screenshot, int x, int y, int w, int h);
      bool IsVisible(Bitmap screenshot, int x, int y, int w, int h, double minScore = 80.0);
  }
  ```
  - `Recognize`: mejor carta + score (o `Card == null` si ninguna supera el umbral).
  - `RankCandidates`: candidatos ordenados por score (debug/tests).
  - `IsVisible`: `Recognize(...).Score > minScore` (equivale al `> 80.0` actual).

- **`CardTemplateIndex`** (nuevo, `src/OpenScrape.App/Services`) — carga las 52 `CardDTO` una
  vez vía `ICardCacheService` y precomputa por plantilla:
  - buffer `byte[]` BGRA (LockBits de la imagen decodificada),
  - `ulong DHash`,
  - `Width`/`Height`.
  Sin `ImageBase64` en el hot path. Thread-safe (se construye una vez; lectura concurrente de
  buffers inmutables).

- **`CardRecognitionEngine : ICardRecognizer`** (nuevo) — implementa el pipeline de 2 fases y
  registra `TelemetryCategories.OcrCards` con `IMetricsCollector.Measure`.

### Pipeline (por región)

1. **Recorte directo**: copiar la región a un `byte[]` BGRA con `LockBits` (sin PNG/base64).
2. **dHash** del recorte (mismo algoritmo de 9×8 → 64 bits).
3. **Shortlist**: plantillas con `HammingDistance ≤ DHashMaxDistance` (15, valor actual).
4. **Desempate exacto**: `CalculateSimilarity` sobre la shortlist → mayor `Score`.
5. **Fallback**: si la shortlist queda vacía, escaneo completo de las 52 (exactitud garantizada).

### Precisión

La decisión final usa el **mismo** `CalculateSimilarity`. El dHash solo filtra candidatos: el
ganador siempre se decide por la métrica exacta calculada sobre los supervivientes, nunca por
la distancia Hamming. Ante cualquier duda (shortlist vacía) se cae al escaneo completo, por lo
que el resultado es idéntico al actual.

`Score` es la salida de `CalculateSimilarity`, que ya aplica el gate de `SimilarityThreshold =
90.0` (devuelve `0` por debajo). Por eso `IsVisible(minScore: 80.0)` reproduce exactamente el
`bestMatch > 80.0` actual: los scores son `0` o `≥ 90`.

### Concurrencia

Se elimina el `lock(_lock)` global del hot path. Los buffers de plantillas son inmutables; el
recorte opera sobre una copia local. `System.Drawing` no es thread-safe → sincronización
mínima por operación (no global). `CardTemplateIndex` se expone como Singleton.

### Migración de call sites

Los 5 sitios pasan a `ICardRecognizer`:
- `TableReaderService.ObtainCardsPlayerAsync` / `IsBoardCardVisible` / `CountVisibleBoardCards`
- `GetCardsFlopUseCase`, `GetCardsTurnUseCase`, `GetCardsRiverUseCase`
- `RegionEditorControl` (mantiene base64 solo para el preview; la comparación usa el recognizer)

`ImageCropperService` se conserva (usado por preview/otras rutas) pero deja de ser el motor de
reconocimiento del hot path.

### DI

Registrar `CardTemplateIndex` y `ICardRecognizer` como `Singleton` en el contenedor
(ver `Program.cs` / extensiones de registro existentes).

## Testing

- **Paridad**: para cada una de las 52 plantillas, reconocer su propia imagen → `Score` 100 y
  `Card` correcta.
- **Shortlist no descarta ganadores**: casos borde entre palos rojos (♥/♦) y entre rangos
  cercanos.
- **Paridad con `CompareCardsBase64`**: `Recognize` sobre recortes reales de `output/samples`
  devuelve la misma carta que el camino actual.
- **Benchmark** (`BenchmarkSuite1`): comparar (base64 + 52 comparaciones) vs `ICardRecognizer`
  sobre una imagen 1920×1080.
- **Telemetría**: `OCR.Cards` registra muestras tras el cambio.

## Fuera de alcance

- Matching por escala/tema o ML (YOLO) — va contra el objetivo de velocidad.
- Cambios al formato de `CardDTO` persistido ni a las regiones.

## Criterio de éxito

- Reconocimiento por región más rápido que el actual (medido con benchmark), con resultados
  idénticos sobre las capturas de `output/samples`.
- Lógica de reconocimiento centralizada en `ICardRecognizer` y telemetría `OCR.Cards` activa.
