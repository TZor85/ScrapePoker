# ADR-0002 — WinForms sobre .NET 10 (`net10.0-windows`) como UI

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha (inferida):** original desde el inicio del proyecto (`249bcdb Initial commit`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

El bot necesita capturar pixels de otra ventana del sistema, dibujar un overlay encima de una mesa de poker y ejecutar lógica con muy baja latencia (<200ms por iteración del game loop). El target es exclusivamente Windows (las salas de poker para PC corren en Windows).

## Decisión

Usar **WinForms con .NET 10 Windows** (`<TargetFramework>net10.0-windows</TargetFramework>` en `OpenScrape.App.csproj`), Allman braces y formularios diseñados con designer + code-behind.

## Alternativas consideradas

1. **WPF.** Considerado pero abandonado. Razón inferida: para el bot de poker el UI es sencillo (5 tabs + overlay translúcido) y la suite legacy de WinForms `System.Drawing` + GDI+ permite manipulación directa de pixels mejor que WPF (que abstrae el render). Hay además experiencia previa del autor con WinForms (commits muy tempranos ya usan `Form1`, `FrmMain`).
2. **MAUI / UWP.** Hay rastros de tentativa: `MainPage.xaml` y `MainPage.xaml.cs` en raíz, ambos vacíos (anomalía Scout). Abandonado, probablemente porque MAUI no soporta bien overlay always-on-top sobre otras apps Windows + multi-target añadía complejidad innecesaria.
3. **Avalonia / WPF + cross-platform.** Rechazado: el target es exclusivamente Windows. No tiene sentido pagar el peaje de cross-platform.
4. **App web (Electron / Blazor Server).** Rechazado: requiere acceso a Win32 (BitBlt, EnumWindows) que es trivial en WinForms y caro de exponer a un proceso web.
5. **CLI sin UI.** Rechazado: el overlay live es la value proposition del producto.

## Consecuencias

**Positivas:**

- Acceso directo a Win32 (BitBlt, PrintWindow, SetWindowPos para overlay) sin ceremony.
- `System.Drawing.Bitmap` + LockBits permite el path crítico de pixel sampling rápido (commit `b078777 perf/core: Optimize pixel sampling and region/card cache`).
- `ISynchronizeInvoke` resuelve el cross-thread invoke desde el game loop background thread.
- Designer auto-genera código repetitivo del UI.

**Negativas:**

- **Lock-in a Windows.** Inviable migrar a Linux/macOS sin reescribir. Aceptable porque el dominio (clientes de poker) ya es Windows-only.
- WinForms tiene escaso soporte de testing automatizado del UI. Los 645+ tests NUnit testean motor + servicios pero **no `FrmMain`** (ahora en parte sí, tras commit `343ad2a` que extrajo helpers). Quedan ~4,244 LOC en `FrmMain` cuya cobertura es manual.
- Designer code mezclado con código del programador en archivos `*.Designer.cs` versionados (con riesgo de merge conflicts si se trabaja en equipo — irrelevante hoy).

**Implicaciones para una migración:**

- En cualquier UI cross-platform (Avalonia, MAUI, Electron) hay que reimplementar:
  - **Overlay always-on-top** alineado con la ventana del cliente de poker (recibe `RECT` de Win32). Avalonia con `Topmost=true` puede aproximarse.
  - **Pixel sampling rápido** desde otra ventana. Bitmap → array byte directo. En cross-platform requiere bindings nativos.
  - **`FrmHistorial` con DataGridView** — 2 grids side-by-side con filtros. Reemplazables por componente equivalente.
- Los **5 tabs del FrmMain** (Juego, Config, Tablas, Logs, Historial) constituyen la UX nuclear y deben preservarse semánticamente.

## Referencias

- `src/OpenScrape.App/OpenScrape.App.csproj` — TargetFramework `net10.0-windows`.
- Commits prehistóricos `e4d5a6c changes in forms`, `bcdd46e Create new form for playing` — historia del UI.
- Anomalía MAUI legacy en `surface.json` (`MainPage.xaml` vacíos).
