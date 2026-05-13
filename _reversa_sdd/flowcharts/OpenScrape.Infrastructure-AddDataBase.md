# Flowchart por función — `Services.AddDataBase()`

> Detalle del único método público del módulo `OpenScrape.Infrastructure`.
> Generado por el Arqueólogo del Reversa.

**Archivo:** `src/OpenScrape.Infrastructure/Services.cs:11`
**Firma:** `public static void AddDataBase(this IServiceCollection services, IConfiguration configuration, bool IsDevelopment)`
**Retorno:** `void` — muta `services` por efecto colateral.
**Llamadores conocidos:** `OpenScrape.App/Program.cs:52` (única llamada).

---

## Diagrama de control

```mermaid
flowchart TD
    A[Entrada AddDataBase<br/>services, configuration, IsDevelopment] --> B[services.AddMarten lambda]

    B --> C["string cs = configuration.GetConnectionString DefaultConnection !"]
    C -. null-forgiving operator '!' .-> CErr["NRE en runtime si la clave no existe"]
    C --> D["options.Connection cs"]

    D --> E["options.UseSystemTextJsonForSerialization()"]

    E --> F[Bucle conceptual: registrar 7 índices]

    F --> F1["GameSession: index EndTime"]
    F1 --> F2["GameSession: index SessionId"]
    F2 --> F3["GameSession: index TableName"]
    F3 --> F4["HandRecord: index GameSessionId"]
    F4 --> F5["HandRecord: index Timestamp"]
    F5 --> F6["HandRecord: index compuesto GameSessionId+Timestamp"]
    F6 --> F7["HandRecord: index HeroPosition"]

    F7 --> G{"if IsDevelopment"}
    G -- true --> G1["options.AutoCreateSchemaObjects = AutoCreate.All"]
    G -- false --> G2["No-op: dejar AutoCreate por defecto = None"]

    G1 --> H[Fin lambda]
    G2 --> H

    H --> I[Marten registra IDocumentStore en DI<br/>como singleton]
    I --> Z([Retorno void al caller])

    CErr --> ZErr([Aborto del Host builder])

    style C fill:#ffe6cc
    style CErr fill:#fdd
    style G1 fill:#dfd
    style G2 fill:#fce4ec
```

## Tabla de invariantes y supuestos

| # | Invariante | Estado | Comentario |
|---|------------|--------|------------|
| 1 | `services != null` | 🟡 INFERIDO | No se valida; método de extensión asume no-null. |
| 2 | `configuration != null` | 🟡 INFERIDO | Sin validación. |
| 3 | `ConnectionStrings:DefaultConnection` está presente | 🔴 SIN VALIDAR | Operador `!` enmascara el null check; fallaría con NRE. |
| 4 | `IsDevelopment` refleja realmente el entorno | 🔴 ROTO | `Program.cs:52` lo hardcodea a `true`. |
| 5 | Marten admite `Schema.For<T>().Index(…)` repetido | 🟢 CONFIRMADO | Cada llamada agrega un índice; orden no importa. |
| 6 | Tipos `GameSession` y `HandRecord` tienen propiedad `Id` (string) | 🟢 CONFIRMADO | Convención Marten satisfecha. |
| 7 | `STJ` puede deserializar todos los tipos de `HandRecord` | 🟢 CONFIRMADO | `decimal`, `DateTime`, `enum`, `List<>`, `string?` son STJ-compatibles. `TelemetryAggregate?` también. |

## Riesgos identificados

| Severidad | Riesgo | Mitigación sugerida |
|-----------|--------|---------------------|
| 🔴 alta | Push to production con `IsDevelopment=true` provocará migraciones automáticas no auditadas | Cambiar `Program.cs:52` a `context.HostingEnvironment.IsDevelopment()` |
| 🔴 alta | NRE silencioso si falta `DefaultConnection` | Validar la cadena antes y lanzar `InvalidOperationException` con mensaje claro |
| 🟡 media | Sin override del esquema → Marten escribe en `public` por defecto | Considerar `options.DatabaseSchemaName = "openscrape"` para aislamiento multi-app |
| 🟡 media | Sin política de retry/timeout en la conexión | Marten 8 admite `options.Advanced.*`; evaluar para conexión Neon (latencia variable) |
| 🟡 baja | Sin `options.Logger(...)` registrado → no hay traza SQL | Considerar logger condicional en Development |
