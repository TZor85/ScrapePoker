## Why

El motor de decisión postflop tiene 6 puntos ciegos que cuestan dinero directamente:

1. **Float exit forzado en bad runout** — Si hero flota flop y brickea turn, el bot fuerza una apuesta de "exit strategy" sin verificar si el board empeoró. Pierde bets innecesarios.
2. **Multiway penalty lineal** — La penalización por extra oponentes es `n × constante`, sin considerar que OOP con 2 villanos detrás es exponencialmente peor (no linealmente). Calls demasiado agresivos en pots multiway.
3. **Sin ajuste postflop por 3-bet pot** — Cuando hero juega un pot 3-beteado, la decisión postflop usa los mismos umbrales que un open raise normal. El rango del villano es mucho más estrecho en 3bet pots.
4. **Sin detección de all-in** — Se aplica fold equity a jugadores que ya están committed. Cálculos de fold equity y reverse implied odds incorrectos.
5. **Check-raise sin guard de SPR** — Con SPR < 1.5, un check-raise compromete el stack completo. No se verifica si la equity justifica ese commit.
6. **SPR transiciones discretas** — SPR 1.9 vs 2.1 cambia bruscamente de push/fold a normal. Debería interpolar suavemente.

## What Changes

1. **Float exit abort** — Antes de ejecutar float exit en turn, verificar: ¿hero mejoró a pair+? ¿El board empeoró (overcard, draw completado)? Si brickeó → Check en vez de Bet.
2. **Multiway penalty exponencial + posicional** — Escalar penalización cuadráticamente para OOP, considerar nº de villanos detrás (no solo total), y añadir penalty por street (turn/river multiway más peligroso que flop).
3. **3-bet pot postflop adjustment** — Detectar `HandSituation` como ThreeBet/FourBet/Squeeze y aplicar +5 a FoldBelow y +3 a ThinValueAbove (rango villano más estrecho exige más equity).
4. **All-in detection** — Nuevo flag `IsAnyoneAllIn` en `PostflopGameContext`. Cuando activo: desactivar fold equity, desactivar reverse implied odds, ajustar SPR al effective stack.
5. **Check-raise SPR guard** — Si SPR < 1.5 y check-raise compromete >70% del stack, exigir equity >= 60% (mucho más estricto que el threshold normal).
6. **SPR interpolación suave** — Reemplazar buckets discretos por interpolación lineal entre zonas (push/fold → normal → deep).

## Capabilities

### New Capabilities

- `allin-detection`: Flag `IsAnyoneAllIn` en PostflopGameContext que ajusta fold equity, reverse implied odds y SPR.
- `threbet-postflop-adjustment`: Ajuste de umbrales cuando la mano se desarrolla en un pot 3-beteado/4-beteado.

### Modified Capabilities

- `float-exit-abort`: Float exit verifica runout y equity antes de forzar bet.
- `multiway-positional-penalty`: Penalización cuadrática OOP + por villanos detrás + por street.
- `checkraise-spr-guard`: Check-raise bloqueado con SPR < 1.5 y equity insuficiente.
- `spr-smooth-interpolation`: Transiciones suaves entre zonas de SPR.

## Impact

- **`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`**: Modificar float exit (600-607), multiway penalty (185-197), check-raise (546-582), SPR adjustment (889-904). Añadir 3bet detection (155-183).
- **`src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs`**: Añadir `IsAnyoneAllIn` flag.
- **`src/OpenScrape.Domain/Entities/StrategyProfile.cs`**: Nuevos parámetros para 3bet adjustment, multiway scaling, check-raise SPR guard.
- **`src/OpenScrape.App/Forms/FrmMain.cs`**: Detectar all-in en game loop, pasar flag a contexto.
- **`OpenScrape.App.Tests/`**: Tests para cada escenario.
