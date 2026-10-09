# Ideas de mejora desde repositorios públicos (>1000★)

Investigación de repositorios públicos de GitHub y GitLab con más de 1000 estrellas,
de los que coger ideas para mejorar OpenScrape.

- **Fecha**: 2026-10-09
- **Método**: consulta directa a las APIs (GitHub Search API y `/repos`, GitLab API v4).
- **Conclusión GitLab**: no existe ningún repo relevante con >1000★ (el máximo en poker
  es ~34★, algunos de *planning-poker*). Todas las conclusiones provienen de GitHub.

---

## 1. Inventario de repos relevantes (estrellas verificadas)

| Área | Repositorio (★) | Notas |
|---|---|---|
| **OCR / texto** | PaddleOCR (90 844), tesseract-ocr/tesseract (76 884), hiroi-sora/Umi-OCR (47 720), RapidAI/RapidOCR (8 087), charlesw/tesseract (2 462), Sdcb.PaddleSharp (1 455), dotnet/machinelearning (9 355) | tesseract y charlesw/tesseract ya están en uso vía NuGet |
| **Detección de cartas (CV)** | ultralytics/ultralytics (62 335), ultralytics/yolov5 (58 127), microsoft/onnxruntime (22 042), shimat/opencvsharp (6 115), dusty-nv/jetson-inference (9 012), dme-compunet/YoloSharp (564), geaxgx/playing-card-detection (439) | OpenCvSharp ya está en uso |
| **Captura de pantalla** | obsproject/obs-studio (77 193), ShareX/ShareX (39 956), MathewSachin/Captura (10 850), SnapXL/SnapX (1 046), smasherprog/screen_capture_lite (726) | ShareX y Captura son C# |
| **IA / estrategia poker** | kwai/DouZero (4 675), datamllab/rlcard (3 573), bupticybee/TexasSolver (2 577), dickreuter/Poker (2 476), fedden/poker_ai (1 587), charliehess/slack-poker-bot (1 680), b-inary/wasm-postflop (596), b-inary/postflop-solver (373), andrewprock/pokerstove (886) | TexasSolver y postflop-solver son GTO/CFR |
| **Hand evaluator / equity** | HenryRLee/PokerHandEvaluator (522), uoftcprg/pokerkit (503), zekyll/OMPEval (231) | Ninguno supera 1000★ |
| **Arquitectura / UI .NET** | AvaloniaUI/Avalonia (31 633), jasontaylordev/CleanArchitecture (20 634), ardalis/CleanArchitecture (18 512), kgrzybek/modular-monolith-with-ddd (14 060), lepoco/wpfui (9 685), Taiizor/ReaLTaiizor (2 353), RobinPerris/DarkUI (869), michel-pi/GameOverlay.Net (429) | ReaLTaiizor es drop-in para WinForms |
| **Competidor directo** | OpenHoldem/openholdembot (255) | NO cumple >1000★ |

---

## 2. Ideas de mejora (con evidencia)

### Idea 1 — OCR híbrido y desacoplado
Sustituir el `lock(_lock)` global de `OcrService.cs` (~línea 83), que serializa **todo** el OCR,
por un pool de engines Tesseract. Añadir **PaddleOCR/RapidOCR** vía `Sdcb.PaddleSharp`
(modelos ONNX de detección + reconocimiento, locales y ligeros) como motor alternativo, con
Tesseract como fallback.
- Refs: PaddleOCR 90.8k, RapidOCR 8.1k, Umi-OCR 47.7k, Sdcb.PaddleSharp 1.4k.
- Evidencia: ONNX local ligero bate a Tesseract en fuentes estilizadas.

### Idea 2 — Reconocer cartas por CV, no por OCR
Templates de las 52 cartas + `Cv2.MatchTemplate` multi-escala (OpenCvSharp ya está en uso) o
YOLOv8 ONNX (`YoloSharp`). Más robusto que OCR ante temas/fuentes de distintos clientes.
Encaja con `CardCacheService.cs`, que ya carga los 52 `CardDTO`.
- Refs: ultralytics 62.3k, yolov5 58.1k, YoloSharp 564, playing-card-detection 439.

### Idea 3 — Captura GPU/robusta
Migrar de GDI `BitBlt` a **Windows.Graphics.Capture** (patrón de ShareX/Captura) para ventanas
aceleradas. Ataca directamente el bug reciente de imágenes DWM cacheadas en
`CaptureWindowsHelper`.
- Refs: ShareX 40k, Captura 10.8k, obs-studio 77k.

### Idea 4 — Hand evaluator perfect-hash O(1)
Tabla tipo TwoPlusTwo / `PokerHandEvaluator` para evaluar 7 cartas en tiempo constante.
Acelera `MonteCarloSimulator`, cuello de botella por decisión.
- Refs: PokerHandEvaluator 522, OMPEval 231, pokerstove 886, pokerkit 503.

### Idea 5 — Calibrar estrategia con GTO / self-play offline
Usar CFR de **TexasSolver**/**postflop-solver** y self-play de **rlcard**/**DouZero** para generar
rangos y oponentes sintéticos, alimentando `AutoCalibrationService`, `StrategyBacktester` y
`ExploitabilityCalculator` en lugar de heurísticas sueltas.
- Refs: TexasSolver 2.6k, postflop-solver 373, rlcard 3.6k, DouZero 4.7k, dickreuter/Poker 2.5k,
  fedden/poker_ai 1.6k.

### Idea 6 — UI/overlay moderno
`ReaLTaiizor` (drop-in WinForms) o migrar a WPF-UI/Avalonia; overlay nativo con `GameOverlay.Net`.
- Refs: ReaLTaiizor 2.3k, wpfui 9.7k, Avalonia 31.6k, GameOverlay.Net 429, DarkUI 869.

### Idea 7 — Arquitectura
Reforzar la separación Features/Domain con patrones de `ardalis/CleanArchitecture` +
`modular-monolith-with-ddd`.
- Refs: jasontaylordev/CleanArchitecture 20.6k, ardalis 18.5k, modular-monolith-with-ddd 14k.

---

## 3. Plan propuesto

- **Fase 0 — Decidir foco**: elegir 1-2 áreas. Recomendado: **#2 (CV de cartas)** y luego **#3 (captura)**.
- **Fase 1 — PoC aislado**: por área, en rama `feature/improve-code`. Medir precisión/latencia vs.
  lo actual usando las capturas de `output/samples`.
- **Fase 2 — Integración**: vía interfaces existentes (`ICaptureService`, `IOcrService`,
  `IHandEvaluator`) manteniendo paridad funcional, con tests.
- **Fase 3 — Benchmark**: medir con `BenchmarkSuite1` y evaluar.
