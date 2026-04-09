# Spec: ScreenReaderService

## Requisito

El sistema DEBE encapsular toda la lógica de OCR, captura de pantalla y detección de color en un servicio `ScreenReaderService : IScreenReaderService`, eliminando el acoplamiento directo de FrmMain con Tesseract, OpenCV y SkiaSharp.

## Conceptos

- **ScreenReaderService**: Singleton que gestiona OcrService, ColorDetectionService, ImageCropperService y GetWindowsScreenUseCase internamente.
- **Normalización**: Conversión de texto OCR crudo a valores numéricos válidos (stacks, bets) con validación contra pot size.
- **Consensus read**: Lectura de bet values con 3 intentos OCR para eliminar artefactos.

## Interfaz Pública

```csharp
public interface IScreenReaderService
{
    Bitmap CaptureScreen(IntPtr windowHandle);
    string ReadPlayerName(Bitmap screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral);
    decimal ReadBetValue(Bitmap screenshot, int posX, int posY, int width, int height, decimal potSize);
    decimal ReadStackValue(Bitmap screenshot, int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber);
    string ReadHandNumber(Bitmap screenshot, int x, int y, int w, int h);
    string ReadText(Bitmap screenshot, int x, int y, int w, int h, double? umbral);
    bool DetectColor(Bitmap screenshot, int x, int y, int w, int h, List<string> targetColors);
    OcrResultWithConfidence ReadWithConfidence(Bitmap screenshot, int x, int y, int w, int h);
    Bitmap PreprocessForOcr(Bitmap source, int x, int y, int w, int h);
}
```

## Escenarios

### Escenario 1: NormalizeBetValue corrige decimal perdido

DADO un texto OCR "593"
Y el pot size es 5.93
CUANDO se invoca ReadBetValue
ENTONCES retorna 5.93m (no 593m)

### Escenario 2: Consensus read elimina artefactos

DADO que la primera lectura OCR retorna "8"
Y la segunda retorna "2.50"
Y la tercera retorna "2.50"
CUANDO se invoca ReadBetValue
ENTONCES retorna 2.50m (mayoría gana)

### Escenario 3: Stack con formato decimal europeo

DADO un texto OCR "1.234,56"
CUANDO se invoca ReadStackValue
ENTONCES retorna 1234.56m

### Escenario 4: Detección de color dealer

DADO un screenshot con el botón de dealer en posición (x, y)
Y los colores target son los del dealer button
CUANDO se invoca DetectColor
ENTONCES retorna true si el pixel central coincide dentro del umbral

### Escenario 5: OCR con baja confianza retrigerea

DADO un screenshot con texto borroso
Y la primera lectura tiene confianza < 0.70
CUANDO se invoca ReadWithConfidence
ENTONCES retorna el resultado con IsHighConfidence = false
Y el caller puede decidir reintentar
