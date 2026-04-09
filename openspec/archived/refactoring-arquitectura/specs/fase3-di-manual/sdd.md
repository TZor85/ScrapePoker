# SDD — Fase 3: Migrar Servicios Manuales a DI

## 1. Propósito

Eliminar las 7 instancias creadas con `new` en FrmMain, registrándolas en el contenedor DI. Esto permite sustituir implementaciones en tests y elimina acoplamiento directo.

## 2. Servicios a Migrar

| # | Servicio | Constructor | Línea FrmMain | Lifetime |
|---|----------|-------------|---------------|----------|
| 1 | OcrService | Parameterless | 460 | Singleton |
| 2 | ColorDetectionService | Parameterless | 459 | Singleton |
| 3 | ImageCropperService | Parameterless | 446 | Singleton |
| 4 | SetFlopForceBoardUseCase | Parameterless | 450 | Singleton |
| 5 | GetHashImageUseCase | Parameterless (static) | 451 | Singleton |
| 6 | GetCropImageUseCase | Parameterless (static) | 452 | Singleton |
| 7 | DetectionLoggerService | Parameterless | 510 | Singleton |

Todos son parameterless — registro directo sin factory.

## 3. Decisiones

### 3.1 No se crean interfaces nuevas para estos servicios en esta fase

Los 3 use cases (4-6) ya tienen interfaces (`ISetFlopForceBoardUseCase`, `IGetHashImageUseCase`, `IGetCropImageUseCase`). Los servicios 1-3 y 7 se registran por tipo concreto. Las interfaces se crearán en fases posteriores si se necesitan para testing.

### 3.2 Lifetime Singleton

Todos son stateless o con caché interno que debe persistir durante la vida de la app.

## 4. Impacto

- `Program.cs` — 7 registros nuevos
- `FrmMain.cs` — eliminar 7 `new`, inyectar por constructor
