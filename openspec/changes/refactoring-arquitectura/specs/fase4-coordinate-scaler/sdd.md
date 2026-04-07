# SDD — Fase 4: CoordinateScaler a Servicio Inyectable

## 1. Propósito

Convertir `CoordinateScaler` de `static class` a servicio con interfaz `ICoordinateScaler`, eliminando estado estático mutable y permitiendo testeo e inyección.

## 2. Call Sites (10 en 5 archivos)

| Archivo | Uso |
|---------|-----|
| Program.cs:124 | `Initialize()` |
| FrmMain.cs:4152-4154 | `IsInitialized`, `Initialize()` |
| FrmMain.cs:4273 | `ScaleRegion()` |
| GetCardsFlopUseCase.cs:103,108 | `IsInitialized`, `ScaleRegion()` |
| GetCardsTurnUseCase.cs:96,101 | `IsInitialized`, `ScaleRegion()` |
| GetCardsRiverUseCase.cs:96,101 | `IsInitialized`, `ScaleRegion()` |

## 3. Decisiones

### 3.1 Interfaz ICoordinateScaler en App/Services/

La interfaz vive en App porque solo App la consume.

### 3.2 Singleton en DI

Las dimensiones de referencia son globales por sesión. Inicialización lazy en primer captura.

### 3.3 Los 3 UseCases de cartas reciben ICoordinateScaler por constructor

Actualmente los UseCases se crean con `new` en FrmMain (migrados en Fase 3). Al registrarlos en DI, pueden recibir `ICoordinateScaler` por constructor.

## 4. Impacto

- Nuevo: `ICoordinateScaler.cs`
- Modificados: `CoordinateScaler.cs`, `Program.cs`, `FrmMain.cs`, 3 GetCardsXxxUseCase
