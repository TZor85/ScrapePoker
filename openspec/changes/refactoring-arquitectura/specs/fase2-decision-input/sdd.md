# SDD — Fase 2: PostflopDecisionInput (Objeto Parámetro)

## 1. Propósito

Reemplazar los 36 parámetros de `PostflopDecisionService.DetermineAction()` por un record inmutable `PostflopDecisionInput`. Esto mejora legibilidad, reduce errores por orden de parámetros, y facilita extensibilidad futura sin romper firmas.

## 2. Alcance

### En alcance
- Crear record `PostflopDecisionInput` en `src/OpenScrape.DecisionMaker/DTOs/`
- Agregar overload `DetermineAction(PostflopDecisionInput)` en `PostflopDecisionService`
- Agregar el overload en `IPostflopDecisionService`
- Migrar los 3 call sites en FrmMain al nuevo overload
- Marcar el overload de 36 parámetros como `[Obsolete]`

### Fuera de alcance
- No se migran los 186+ tests existentes (usan el overload legacy)
- No se modifica la lógica interna de `DetermineAction`
- No se modifica `HandleFacingBet`, `HandleNoBet` ni métodos privados

## 3. Decisiones de Diseño

### 3.1 Record inmutable con `required` para campos obligatorios

Los 6 campos que nunca tienen default significativo se marcan `required`:
- `Equity`, `Street`, `Situation`, `BoardTexture`, `IsInPosition`, `VillainBetSize`

Los 30 restantes tienen `init` con defaults idénticos a los valores por defecto actuales.

### 3.2 Overload legacy se mantiene con `[Obsolete]`

El método de 36 parámetros se marca `[Obsolete("Usar DetermineAction(PostflopDecisionInput) en su lugar")]` pero no se elimina. Los 186+ tests existentes lo usan y se migrarán gradualmente.

El overload legacy delega al nuevo internamente.

### 3.3 Ubicación en DTOs/

El record va en `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs` siguiendo la convención de DTOs del proyecto.

## 4. Impacto

### Nuevos
- `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs`

### Modificados
- `IPostflopDecisionService.cs` — agregar overload
- `PostflopDecisionService.cs` — agregar overload, marcar legacy como obsoleto
- `FrmMain.cs` — migrar 3 call sites (river:1561, flop:1931, turn:2050)

## 5. Criterios de Verificación

- `dotnet build` — 0 errores (warnings de Obsolete en tests son esperados)
- `dotnet test` — 592 tests pasan
- Los 3 call sites en FrmMain usan `new PostflopDecisionInput { ... }`
- El overload de 36 params está marcado `[Obsolete]`
