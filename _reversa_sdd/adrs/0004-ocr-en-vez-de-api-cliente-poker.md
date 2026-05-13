# ADR-0004 — Scraping pasivo (OCR + visión artificial) en lugar de integración con la API del cliente de poker

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha (inferida):** decisión foundational, anterior al primer commit
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

Para que el bot recomiende jugadas, necesita conocer:

- Cartas del hero, board, número de jugadores activos, posiciones, stacks, bets, pot, hand number, dealer, sit-out states.

Las salas de poker (PokerStars, GG, etc.) **no exponen APIs públicas** para terceros, y sus TOS prohíben hooks en su proceso. Acceder a memoria, hooks Win32 a sus dialogs o intercepción de tráfico cifrado constituyen "tools no autorizadas" y dan ban perma.

## Decisión

Adoptar **scraping pasivo de pixels**:

- Captura periódica (`BitBlt` / `PrintWindow`) de la ventana del cliente de poker.
- **OCR (Tesseract)** sobre regiones predefinidas para texto (stacks, bets, hand number, alias).
- **Comparación de hash de imagen (dHash)** sobre regiones de cartas (52 tarjetas pre-cacheadas en `CardCacheService`).
- **Análisis de color** (`ColorDetectionService`) para botones de acción y dealer.
- **Mapa de regiones por sala** (`RegionTableMap`, `tableMap.json`) — coordenadas relativas a un layout de referencia, escaladas con `CoordinateScaler`.

Resultado: el bot **observa pasivamente** sin hooks ni inyección. La spec del producto (inferida) es: "el usuario sigue pulsando los botones; el bot solo recomienda".

## Alternativas consideradas

1. **Hook Win32 a controles del cliente.** Rechazado: TOS-violation, ban garantizado al detectarse.
2. **Inyección DLL / Detours.** Rechazado: similar al anterior, además detectable por anti-cheat de la sala.
3. **Sniffing de tráfico cifrado (man-in-the-middle).** Rechazado: TLS pinning hace esto inviable sin cert custom + costo legal.
4. **Hand history file watching.** Algunas salas escriben archivos de history en disco. Rechazado como **fuente única** porque se actualizan **al final de la mano** — inútil para decisiones live. Pero útil **post-hoc** (de hecho commit `50adef2 feat/historial: Import sessions from XML files` añade importer XML para análisis offline).
5. **Captura de audio + voice recognition.** Absurdo, pero descartado mentalmente.
6. **Pedir al usuario que rellene la mesa manualmente.** Rechazado: rompe la UX (lentísimo, fuente de errores).
7. **Computer vision con ML (CNN para detectar cartas).** Considerado en la prehistoria: hubo un sprint ML.NET (`9acdcc0 [feature/ML]: Add RolOopHelper for poker action prediction`). Rechazado posteriormente (commit `2360036`) porque el coste de mantenimiento del modelo + dataset > el coste del approach OCR + dHash.

## Consecuencias

**Positivas:**

- **Cumplimiento parcial del TOS** (no inyección, no hooks). El bot solo "lee" lo que el ojo del usuario también ve. Riesgo legal residual pero menor.
- Portable a múltiples salas sin reverse engineering: solo requiere calibrar regiones (`tableMap.json`).
- Sin dependencia de actualizaciones del cliente: si la sala cambia su DLL interna, el bot sigue funcionando mientras el layout visual se mantenga.

**Negativas:**

- **Robustez frágil.** OCR falla con efectos visuales, animaciones, resoluciones distintas, multi-monitor. Decenas de bug fixes sobre OCR (`OCR-1`..`OCR-4`, `BF1-9`, etc.) son la prueba.
- **Latencia.** Cada iteración del game loop captura → OCR → procesa. Optimizado con cache de regiones, dHash de cartas, LockBits, MC adaptativo. Telemetría detalla bottlenecks.
- **Calibración requerida por sala.** `RegionsTableMap` debe rellenarse para cada cliente / resolución / tema. UX hostil para usuario nuevo.
- **OCR de números con decimales** es notoriamente difícil — `NormalizeBetValue` corrige errores como "593" → "5.93".
- **Single-language**: OCR está limitado a `eng.traineddata` (commit `1343857 refactor/ocr: Add IDisposable and switch to English OCR` — antes era español). Cambio rompe instalaciones donde el cliente está en otro idioma.
- **Sensibilidad a temas y skins.** Cualquier cambio de tema visual del cliente puede invalidar la calibración.

**Implicaciones para una migración:**

- Esta decisión **define el dominio del producto.** Una migración a otra arquitectura debe preservar OCR + dHash + RegionMap. El motor de decisión (`OpenScrape.DecisionMaker`) es independiente de la fuente de datos y portable.
- Si se quisiera "modernizar" hacia un servicio web que reciba el estado pre-procesado, la app desktop seguiría siendo necesaria como sensor.

## Referencias

- `src/OpenScrape.App/Services/OcrService.cs` — Tesseract setup.
- `src/OpenScrape.App/Services/ScreenReaderService.cs` — multi-read consensus (495 LOC).
- `src/OpenScrape.App/Services/CardCacheService.cs` — dHash de las 52 cartas.
- `src/OpenScrape.App/Services/CoordinateScaler.cs` — escalado por resolución.
- Commits `OCR-1`..`OCR-4` (`6726f0e fix/ocr: Robust bet OCR, river validation & 4-bet logic`).
- ADR-0005 (eliminación de ML.NET).
