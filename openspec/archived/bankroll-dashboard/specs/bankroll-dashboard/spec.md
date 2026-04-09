# Bankroll Dashboard - Especificación

## Overview

| Campo | Valor |
|-------|-------|
| **Nombre** | Bankroll Dashboard |
| **Tipo** | Feature de Analytics |
| **Objetivo** | Proporcionar métricas de gestión de bankroll y riesgo para decisiones de límite |
| **Target** | Jugadores cash games que suben límites progresivamente |

## Motivation

El jugador actual usa €2 buy-in y juega 1-2 mesas. No tiene rakeback y juega microlímites donde el rake es alto. Necesita herramientas para:
- Saber cuándo puede subir de límite con seguridad
- Medir su rendimiento real (después de rake)
- Evitar tilt por bad beats extremos

## Scope

### Incluido
- Tracking de bankroll histórico
- Cálculo de Max Drawdown
- Win Rate con desviación estándar
- Risk of Ruin estimation
- Recomendaciones de límite

### Excluido
- ICM para torneos (no aplica)
- Integración con trackers externos (HH2, PT4)
- Alertas automáticas por email

## Data Model

### BankrollSnapshot
```csharp
public class BankrollSnapshot
{
    public DateTime Timestamp { get; set; }
    public decimal Bankroll { get; set; }
    public decimal BigBlind { get; set; }
    public decimal SessionProfit { get; set; }
    public int SessionHands { get; set; }
}
```

### BankrollStats
```csharp
public class BankrollStats
{
    public decimal CurrentBankroll { get; set; }
    public decimal StartingBankroll { get; set; }
    public decimal PeakBankroll { get; set; }
    public decimal MaxDrawdown { get; set; }      // En €
    public double MaxDrawdownPercent { get; set; } // En %
    public double WinRateBB100 { get; set; }
    public double StdDeviation { get; set; }
    public int TotalSessions { get; set; }
    public int WinningSessions { get; set; }
    public int TotalHands { get; set; }
    public double RiskOfRuin { get; set; }        // Probabilidad 0-1
    public decimal AverageSessionProfit { get; set; }
    public string LimitRecommendation { get; set; }
    public string RiskLevel { get; set; }        // Green/Yellow/Red
}
```

### Modificaciones a GameSession
```csharp
public class GameSession
{
    // ... existentes ...
    
    // Nuevos campos para bankroll tracking
    public decimal StartingBankroll { get; set; }
    public decimal EndingBankroll { get; set; }
    public decimal PeakBankroll { get; set; }
}
```

## Interfaz de Servicio

### IBankrollTrackerService

```csharp
public interface IBankrollTrackerService
{
    BankrollStats GetBankrollStats(int sessionCount = 100);
    void RecordSessionEnd(GameSession session);
    decimal GetCurrentBankroll();
    decimal GetPeakBankroll();
    (double riskOfRuin, string level) CalculateRiskOfRuin();
    string GetLimitRecommendation();
    List<BankrollSnapshot> GetHistory(int count = 50);
}
```

## Algoritmos

### Risk of Ruin Calculation

```
Fórmula: RoR = e^(-2 × BR × WR / σ²)

Donde:
- BR = Bankroll actual en BB (no en €)
- WR = Win Rate (BB/100 / 100) = BB per hand
- σ = Desviación estándar en BB

Implementación:
1. Calcular WR como promedio de BB/100 / 100
2. Calcular σ de las últimas N sesiones
3. Calcular BR = CurrentBankroll / BigBlind
4. Si σ = 0 o WR ≤ 0, RoR = 1 (riesgo máximo)
5. Aplicar fórmula exponencial
```

### Max Drawdown

```
1. Obtener historial de bankroll por sesión
2. Trackear Peak máximo histórico
3. MaxDrawdown = Peak - Minimum después del peak
4. MaxDrawdownPercent = (MaxDrawdown / Peak) × 100
```

### Limit Recommendation

| Condición | Recomendación |
|-----------|---------------|
| RoR < 5% && CurrentBankroll >= 25 × BuyInMax | "SUBIR: Bankroll sólido" |
| RoR 5-15% | "MANTENER: Riesgo aceptable" |
| RoR > 15% | "BAJAR: Riesgo alto" |
| MaxDrawdownPercent > 20% | "BAJAR: Drawdown excesivo" |
| TotalSessions < 20 | "DATOS: Collecting more data..." |
| WinRateBB100 < 0 | "ANALIZAR: Win rate negativo" |

## UI Specification

### Layout

```
┌─────────────────────────────────────────────────────────────┐
│  BANKROLL DASHBOARD                                         │
├─────────────────────────────────────────────────────────────┤
│  ┌───────────────────────┐  ┌──────────────────────────────┐│
│  │  €€€ BANKROLL        │  │  RIESGO DE RUINA              ││
│  │  Current: €XXX.XX    │  │  RoR: X.XX%    [VERDE]        ││
│  │  Peak:    €XXX.XX    │  │                               ││
│  │  MaxDD:   -XX.X%     │  │  RECOMENDACIÓN:               ││
│  └───────────────────────┘  │  "SUBIR / MANTENER / BAJAR"  ││
│                             └──────────────────────────────┘│
├─────────────────────────────────────────────────────────────┤
│  RENDIMIENTO                                                │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Win Rate:  X.X BB/100  │  Std Dev:  X.X BB/100         ││
│  │ Sesiones:  XXX         │  Ganadas:  XXX (XX%)          ││
│  │ Manos:    XXXX        │  Media:   €X.XX / sesión       ││
│  └─────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────┤
│  HISTORIAL DE BANKROLL                                      │
│  [Gráfico de línea: Bankroll vs Tiempo]                    │
├─────────────────────────────────────────────────────────────┤
│  ÚLTIMAS SESIONES                                          │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Fecha       │ Manos │ Profit  │ BB/100 │ Bankroll      ││
│  │ XX/XX/XXXX │  XXX  │ +€X.XX  │  X.X    │ €XXX.XX       ││
│  │ ...        │       │         │         │               ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Colores

| Métrica | Verde | Amarillo | Rojo |
|---------|-------|----------|------|
| RoR | < 5% | 5-15% | > 15% |
| Max Drawdown | < 10% | 10-20% | > 20% |
| Win Rate | > 5 BB/100 | 0-5 BB/100 | < 0 |

### Ubicación

- **Tab:** Nuevo tab "Bankroll" en FrmMain
- **Acceso:** Click en botón del sidebar o tab directamente

## Testing

### Unit Tests Requeridos

```csharp
public class BankrollTrackerServiceTests
{
    [Test] public void GetBankrollStats_WithNoSessions_ReturnsZeroedStats()
    [Test] public void GetBankrollStats_WithSessions_CalculatesWinRate()
    [Test] public void CalculateRiskOfRuin_HighWinRate_ReturnsLowRisk()
    [Test] public void CalculateRiskOfRuin_NegativeWinRate_ReturnsHighRisk()
    [Test] public void GetMaxDrawdown_TracksPeakAndValley()
    [Test] public void GetLimitRecommendation_BankrollSolid_ReturnsSubir()
    [Test] public void GetLimitRecommendation_Downward_ReturnsBajar()
    [Test] public void GetHistory_ReturnsCorrectCount()
}
```

## Acceptance Criteria

1. ✅ Dashboard muestra bankroll actual, peak y max drawdown
2. ✅ Win rate calculado como promedio de BB/100 de últimas 100 sesiones
3. ✅ Risk of Ruin calculado y mostrado con color (verde/amarillo/rojo)
4. ✅ Recomendación de límite visible y actualizada
5. ✅ Historial de bankroll mostrable
6. ✅ Tests unitarios pasando
7. ✅ Build sin errores

## Notas de Implementación

- Usar sesiones existentes de Marten para cálculo
- BigBlind configurable por sesión (guardar en GameSession)
- Actualizar stats al iniciar sesión nueva
- No mostrar recomedaciones hasta tener 20+ sesiones
