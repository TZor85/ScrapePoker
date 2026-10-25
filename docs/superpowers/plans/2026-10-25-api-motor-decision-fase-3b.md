# Fase 3b — Licencias y cuotas: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Proteger `/v1/decisions` con licencias por key: activación anónima, JWT de 24 h, cuotas por plan, CLI de administración y pantalla de activación en el cliente.

**Architecture:** El documento `License` vive en Marten y se accede por `ILicenseStore`. `ILicenseTokenService` emite y valida JWT HS256. La API protege `/v1/decisions` con el middleware de autenticación, aplica rate limiting particionado por `sub` con límite según plan y opcionalmente revalida la revocación por request. El cliente guarda key y token cifrados con `Encrypter`, adjunta el Bearer y abre `FormLicense` cuando falta.

**Tech Stack:** .NET 10, ASP.NET Core (`Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.RateLimiting`), Marten 9.51, NUnit, WinForms.

**Spec:** `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md` (secciones `POST /v1/license/activate`, `Licencias y seguridad`, `Cliente`, `Despliegue`).

## Global Constraints

- Castellano en comentarios, tests, mensajes de commit y documentación.
- Sin atribución de IA en commits.
- `OpenScrape.Contracts` no referencia Marten, ASP.NET, `Features`, `Infrastructure`, `DecisionMaker` ni `App`.
- `OpenScrape.Api` y `OpenScrape.Contracts` en `net10.0`; `App` sigue en `net10.0-windows`.
- La key en claro no se persiste ni se loguea; sólo `SHA-256(pepper + key)`.
- JWT HS256, vida 24 h, claims `sub`, `plan`, `iat`, `exp`.
- Cuotas: `pro` 60 req/min, `free` 10 req/min, configurables por `RateLimit:Plans:<plan>:PermitsPerMinute`.
- Códigos: 400 JSON inválido, 401 token ausente/vencido/firma inválida, 403 revocada o plan sin acceso, 422 request imposible, 429 cuota, 500 sin detalles internos.
- Errores con forma `{ "code": "...", "message": "..." }` (`EngineError`).
- `GET /health` sigue siendo liveness sin tocar la base.
- Cada task cierra con `dotnet test OpenScrape.sln` verde y `dotnet format --verify-no-changes` limpio.
- 0 advertencias en Release con `-p:TreatWarningsAsErrors=true`.

## Review Focus

Las cinco clases de entrada o fallas que la spec implica y que los tests de las tasks deben fijar, de mayor a menor riesgo:

1. **Token válido de una licencia revocada** → con `License:CheckRevocationPerRequest=false` sigue entrando hasta vencer; con `true` devuelve 403. Un operador espera que activar el flag corte en el acto.
2. **Reloj y expiración** → un token emitido con `exp` hace 1 s devuelve 401, no 500; un token sin `sub` o sin `plan` devuelve 401, no 403.
3. **Cuota por plan** → `free` corta en la request 11 del minuto y `pro` no; el 429 no consume cuota de otro `sub` ni de otro plan.
4. **Activación con key malformada o de otro formato** → 401 con `license_invalid`, nunca 500, y no filtra si la key existía.
5. **Cliente sin token** → `HttpDecisionEngine` no manda request sin Bearer y `FrmMain` abre `FormLicense` una sola vez, sin loop de reintentos.

---

## File Map

| Archivo | Responsabilidad |
|---|---|
| `src/OpenScrape.Contracts/Licensing/LicenseActivateRequest.cs` | `{ LicenseKey }` |
| `src/OpenScrape.Contracts/Licensing/LicenseActivateResponse.cs` | `{ Token, ExpiresAt, Plan }` |
| `src/OpenScrape.Api/Licensing/License.cs` | Documento Marten |
| `src/OpenScrape.Api/Licensing/ILicenseStore.cs` | Alta, búsqueda por hash, revocación, listado |
| `src/OpenScrape.Api/Licensing/MartenLicenseStore.cs` | Implementación sobre `IDocumentStore` |
| `src/OpenScrape.Api/Licensing/ILicenseKeyHasher.cs` + `LicenseKeyHasher.cs` | SHA-256 con pepper |
| `src/OpenScrape.Api/Licensing/LicenseKeyGenerator.cs` | `OSPK-XXXX-XXXX-XXXX` |
| `src/OpenScrape.Api/Licensing/ILicenseTokenService.cs` + `JwtLicenseTokenService.cs` | Emitir y validar JWT |
| `src/OpenScrape.Api/Licensing/JwtOptions.cs` + `LicenseOptions.cs` | Configuración |
| `src/OpenScrape.Api/Licensing/LicenseRevocationMiddleware.cs` | Corte inmediato opcional |
| `src/OpenScrape.Api/Endpoints/LicenseEndpoints.cs` | `POST /v1/license/activate` |
| `src/OpenScrape.Api/RateLimiting/PlanRateLimiting.cs` | Política particionada por `sub` y por IP |
| `src/OpenScrape.Api/Program.cs` | Auth, rate limiting, DI |
| `src/Tools/LicenseCli/LicenseCli.csproj` + `Program.cs` | `create` / `revoke` / `list` |
| `src/OpenScrape.App/Services/License/LicenseTokenStore.cs` | Key y token cifrados |
| `src/OpenScrape.App/Services/License/LicenseRequiredException.cs` | 401/403 en el cliente |
| `src/OpenScrape.App/Services/License/LicenseClient.cs` | Llamada a activación |
| `src/OpenScrape.App/Forms/FormLicense.cs` | Pantalla de activación |
| `test/OpenScrape.App.Tests/Api/LicenseTestData.cs` | Store en memoria, tokens y keys de prueba |
| `test/OpenScrape.App.Tests/Api/LicenseEndpointTests.cs` | Activación |
| `test/OpenScrape.App.Tests/Api/DecisionAuthTests.cs` | 401/403/429 |
| `test/OpenScrape.App.Tests/Services/LicenseTokenStoreTests.cs` | Cifrado y expiración local |

---

### Task 1: Contrato de licencias

**Files:**
- Create: `src/OpenScrape.Contracts/Licensing/LicenseActivateRequest.cs`
- Create: `src/OpenScrape.Contracts/Licensing/LicenseActivateResponse.cs`
- Test: `test/OpenScrape.App.Tests/Contracts/LicenseContractSerializationTests.cs`

**Interfaces:**
- Consumes: `System.Text.Json`.
- Produces:
  - `sealed record LicenseActivateRequest { required string LicenseKey { get; init; } }`
  - `sealed record LicenseActivateResponse { required string Token { get; init; } required DateTimeOffset ExpiresAt { get; init; } required string Plan { get; init; } }`

- [ ] **Step 1: Escribir el test de serialización**

```csharp
[Test]
public void Request_CamelCase_DeserializaLicenseKey()
{
    var request = JsonSerializer.Deserialize<LicenseActivateRequest>(
        """{"licenseKey":"OSPK-1111-2222-3333"}""", JsonConfig.Options);
    Assert.That(request!.LicenseKey, Is.EqualTo("OSPK-1111-2222-3333"));
}

[Test]
public void Response_RoundTrip_ConservaTokenPlanYVencimiento()
{
    var original = new LicenseActivateResponse
    {
        Token = "abc.def.ghi",
        Plan = "pro",
        ExpiresAt = new DateTimeOffset(2026, 10, 26, 15, 0, 0, TimeSpan.Zero)
    };
    var json = JsonSerializer.Serialize(original, JsonConfig.Options);
    Assert.That(json, Does.Contain("\"plan\":\"pro\""));
    Assert.That(JsonSerializer.Deserialize<LicenseActivateResponse>(json, JsonConfig.Options),
        Is.EqualTo(original));
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~LicenseContractSerializationTests"`
Expected: FAIL con `CS0246: LicenseActivateRequest no se encontró`.

- [ ] **Step 3: Crear los dos records**

`LicenseActivateRequest` con `required string LicenseKey`; `LicenseActivateResponse` con `required string Token`, `required DateTimeOffset ExpiresAt`, `required string Plan`. Sin lógica.

- [ ] **Step 4: Correr y verificar que pasa**

Run: el mismo comando del Step 2. Expected: PASS 2/2.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.Contracts/Licensing test/OpenScrape.App.Tests/Contracts/LicenseContractSerializationTests.cs
git commit -m "feat(contracts): definir el contrato de activacion de licencias"
```

---

### Task 2: Almacén, hash y tokens

**Files:**
- Create: `src/OpenScrape.Api/Licensing/License.cs`
- Create: `src/OpenScrape.Api/Licensing/ILicenseStore.cs`
- Create: `src/OpenScrape.Api/Licensing/ILicenseKeyHasher.cs`
- Create: `src/OpenScrape.Api/Licensing/LicenseKeyHasher.cs`
- Create: `src/OpenScrape.Api/Licensing/LicenseKeyGenerator.cs`
- Create: `src/OpenScrape.Api/Licensing/JwtOptions.cs`
- Create: `src/OpenScrape.Api/Licensing/ILicenseTokenService.cs`
- Create: `src/OpenScrape.Api/Licensing/JwtLicenseTokenService.cs`
- Test: `test/OpenScrape.App.Tests/Api/LicenseKeyHasherTests.cs`
- Test: `test/OpenScrape.App.Tests/Api/JwtLicenseTokenServiceTests.cs`

**Interfaces:**
- Consumes: `System.Security.Cryptography`, `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`.
- Produces:
  - `sealed class License { Guid Id; string KeyHash; string Plan; DateTimeOffset ExpiresAt; bool Revoked; DateTimeOffset CreatedAt; }`
  - `interface ILicenseStore { Task<License?> FindByKeyHashAsync(string keyHash, CancellationToken); Task<License?> GetAsync(Guid id, CancellationToken); Task AddAsync(License license, CancellationToken); Task<bool> RevokeAsync(Guid id, CancellationToken); Task<IReadOnlyList<License>> ListAsync(CancellationToken); }`
  - `interface ILicenseKeyHasher { string Hash(string key); }`
  - `static string LicenseKeyGenerator.Create()` → `OSPK-XXXX-XXXX-XXXX` con alfabeto sin `0/O/1/I`.
  - `record JwtOptions { required string SigningKey; string Issuer = "openscrape"; int LifetimeHours = 24; }`
  - `interface ILicenseTokenService { (string Token, DateTimeOffset ExpiresAt) Issue(License license); bool TryValidate(string token, out LicenseClaims claims); }`
  - `readonly record struct LicenseClaims(Guid Subject, string Plan, DateTimeOffset ExpiresAt);`

- [ ] **Step 1: Test del hasher (rojo)**

```csharp
[Test]
public void Hash_MismaKeyYMismoPepper_EsEstable()
{
    var hasher = new LicenseKeyHasher(new LicenseOptions { Pepper = "pimienta" });
    Assert.That(hasher.Hash("OSPK-1111-2222-3333"),
        Is.EqualTo(hasher.Hash("OSPK-1111-2222-3333")));
}

[Test]
public void Hash_PepperDistinto_CambiaElHash()
{
    var a = new LicenseKeyHasher(new LicenseOptions { Pepper = "a" }).Hash("OSPK-1111-2222-3333");
    var b = new LicenseKeyHasher(new LicenseOptions { Pepper = "b" }).Hash("OSPK-1111-2222-3333");
    Assert.That(a, Is.Not.EqualTo(b));
}
```

- [ ] **Step 2: Correr y verificar rojo**

Run: `--filter "FullyQualifiedName~LicenseKeyHasherTests"` → FAIL `CS0246`.
`LicenseOptions` se define acá con `Pepper` y `bool CheckRevocationPerRequest = false`.

- [ ] **Step 3: Implementar hasher y generador**

`Hash` = hex minúscula de `SHA256(UTF8(pepper + key))`. `Create()` = `"OSPK-"` + 3 grupos de 4 caracteres de `"ABCDEFGHJKLMNPQRSTUVWXYZ23456789"` con `RandomNumberGenerator`.

- [ ] **Step 4: Test del token (rojo)**

```csharp
[Test]
public void Issue_YLuegoValidate_DevuelveElSujetoYElPlan()
{
    var service = new JwtLicenseTokenService(Options.Create(new JwtOptions { SigningKey = Key }));
    var license = new License { Id = Guid.NewGuid(), Plan = "pro", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) };
    var (token, expiresAt) = service.Issue(license);
    Assert.That(service.TryValidate(token, out var claims), Is.True);
    Assert.That(claims.Subject, Is.EqualTo(license.Id));
    Assert.That(claims.Plan, Is.EqualTo("pro"));
    Assert.That(expiresAt, Is.GreaterThan(DateTimeOffset.UtcNow));
}

[Test]
public void TryValidate_FirmaDeOtraKey_EsFalse()
{
    var issuer = new JwtLicenseTokenService(Options.Create(new JwtOptions { SigningKey = Key }));
    var other = new JwtLicenseTokenService(Options.Create(new JwtOptions { SigningKey = OtherKey }));
    var (token, _) = issuer.Issue(new License { Id = Guid.NewGuid(), Plan = "pro", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1) });
    Assert.That(other.TryValidate(token, out _), Is.False);
}

[Test]
public void TryValidate_TokenVencido_EsFalse()
{
    var service = new JwtLicenseTokenService(Options.Create(new JwtOptions { SigningKey = Key, LifetimeHours = 0 }));
    var (token, _) = service.Issue(new License { Id = Guid.NewGuid(), Plan = "pro", ExpiresAt = DateTimeOffset.UtcNow });
    Assert.That(service.TryValidate(token, out _), Is.False);
}
```

- [ ] **Step 5: Implementar el servicio JWT**

`Issue`: claims `sub` = `license.Id`, `plan`, `exp` = `min(now + LifetimeHours, license.ExpiresAt)`; firma `SymmetricSecurityKey(UTF8(SigningKey))` HS256. `TryValidate`: `JwtSecurityTokenHandler.ValidateToken` con `ValidateIssuer=false`, `ValidateAudience=false`, `ValidateLifetime=true`, `ClockSkew = 30s`; devuelve `false` en cualquier excepción y exige `sub` parseable y `plan` no vacío.

- [ ] **Step 6: Verificar y commitear**

Run: `--filter "FullyQualifiedName~LicenseKeyHasherTests|FullyQualifiedName~JwtLicenseTokenServiceTests"` → PASS 5/5.

```bash
git add src/OpenScrape.Api/Licensing test/OpenScrape.App.Tests/Api
git commit -m "feat(api): emitir y validar licencias por key"
```

---

### Task 3: Activación anónima con rate limit por IP

**Files:**
- Create: `src/OpenScrape.Api/Licensing/MartenLicenseStore.cs`
- Create: `src/OpenScrape.Api/Endpoints/LicenseEndpoints.cs`
- Create: `src/OpenScrape.Api/RateLimiting/PlanRateLimiting.cs`
- Modify: `src/OpenScrape.Api/Program.cs`
- Create: `test/OpenScrape.App.Tests/Api/InMemoryLicenseStore.cs`
- Create: `test/OpenScrape.App.Tests/Api/LicenseTestData.cs`
- Test: `test/OpenScrape.App.Tests/Api/LicenseEndpointTests.cs`

**Interfaces:**
- Consumes: `ILicenseStore`, `ILicenseKeyHasher`, `ILicenseTokenService`, `EngineError`.
- Produces:
  - `static IEndpointRouteBuilder MapLicenseEndpoints(this IEndpointRouteBuilder app)`
  - `sealed class InMemoryLicenseStore : ILicenseStore` (tests)
  - `static class PlanRateLimiting { const string ActivationPolicy = "activation"; const string DecisionsPolicy = "decisions"; static void AddPlanRateLimiting(this IServiceCollection services, IConfiguration configuration); }`

- [ ] **Step 1: Test de activación (rojo)**

```csharp
[Test]
public async Task Activate_KeyValida_DevuelveTokenPlanYVencimiento()
{
    var response = await _client.PostAsJsonAsync("/v1/license/activate",
        new LicenseActivateRequest { LicenseKey = LicenseTestData.ValidKey }, JsonConfig.Options);

    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    var body = await response.Content.ReadFromJsonAsync<LicenseActivateResponse>(JsonConfig.Options);
    Assert.That(body!.Plan, Is.EqualTo("pro"));
    Assert.That(body.Token, Is.Not.Empty);
    Assert.That(body.ExpiresAt, Is.GreaterThan(DateTimeOffset.UtcNow));
}

[Test]
public async Task Activate_KeyInexistente_Devuelve401ConCodigo()
{
    var response = await _client.PostAsJsonAsync("/v1/license/activate",
        new LicenseActivateRequest { LicenseKey = "OSPK-0000-0000-0000" }, JsonConfig.Options);

    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    var error = await response.Content.ReadFromJsonAsync<EngineError>(JsonConfig.Options);
    Assert.That(error!.Code, Is.EqualTo("license_invalid"));
    Assert.That(error.Message, Does.Not.Contain("OSPK-"));
}

[Test]
public async Task Activate_KeyRevocada_Devuelve401()
{
    LicenseTestData.RevokeValidKey();
    var response = await _client.PostAsJsonAsync("/v1/license/activate",
        new LicenseActivateRequest { LicenseKey = LicenseTestData.ValidKey }, JsonConfig.Options);
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
}

[Test]
public async Task Activate_KeyVacia_Devuelve400()
{
    var response = await _client.PostAsJsonAsync("/v1/license/activate",
        new LicenseActivateRequest { LicenseKey = "  " }, JsonConfig.Options);
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
}
```

`LicenseTestData` expone `ValidKey`, `ProLicenseId`, `FreeKey`, y `RevokeValidKey()`; `ApiFactory` registra `ILicenseStore` → `InMemoryLicenseStore` con dos licencias (`pro` y `free`) ya sembradas.

- [ ] **Step 2: Correr y verificar rojo**

Run: `--filter "FullyQualifiedName~LicenseEndpointTests"` → FAIL `404` (no existe el endpoint).

- [ ] **Step 3: Implementar el store Marten**

`MartenLicenseStore(IDocumentStore store)`: `LightweightSession`; `Query<License>().FirstOrDefaultAsync(l => l.KeyHash == keyHash)`; `AddAsync` con `session.Store`; `RevokeAsync` con `LoadAsync<License>(id)` + `Revoked = true` + `SaveChangesAsync`, devuelve `false` si no existe; `ListAsync` con `Query<License>().ToListAsync()`. Registrar el esquema `license` con `StoreOptions.Schema.For<License>().DatabaseSchemaName("license")` en `AddDataBase` o al abrir la sesión.

- [ ] **Step 4: Implementar el endpoint**

```
POST /v1/license/activate  [AllowAnonymous] [EnableRateLimiting(ActivationPolicy)]
body vacío o LicenseKey en blanco            → 400 invalid_request
hash = hasher.Hash(key)
license = await store.FindByKeyHashAsync(hash)
license == null || license.Revoked || license.ExpiresAt <= now → 401 license_invalid
(token, expiresAt) = tokens.Issue(license)
200 → LicenseActivateResponse
```

- [ ] **Step 5: Rate limit de activación por IP**

`AddPlanRateLimiting`: `AddRateLimiter` con `AddPolicy(ActivationPolicy, context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }))`. En `UseRateLimiter()`, `OnRejected` responde 429 con `EngineError("rate_limited", ...)`.

- [ ] **Step 6: Verificar y commitear**

Run: `--filter "FullyQualifiedName~LicenseEndpointTests"` → PASS 4/4.
Run: `dotnet test OpenScrape.sln` → verde.

```bash
git add -A
git commit -m "feat(api): activar licencias con rate limit por ip"
```

---

### Task 4: Proteger decisiones con Bearer, revocación y cuota

**Files:**
- Create: `src/OpenScrape.Api/Licensing/LicenseRevocationMiddleware.cs`
- Modify: `src/OpenScrape.Api/Program.cs`
- Modify: `src/OpenScrape.Api/Endpoints/DecisionEndpoints.cs`
- Modify: `test/OpenScrape.App.Tests/Api/ApiFactory.cs`
- Modify: `test/OpenScrape.App.Tests/Api/DecisionEndpointTests.cs`
- Modify: `test/OpenScrape.App.Tests/Api/ParidadHttpTests.cs`
- Test: `test/OpenScrape.App.Tests/Api/DecisionAuthTests.cs`

**Interfaces:**
- Consumes: todo lo anterior.
- Produces:
  - `ApiFactory.CreateLicensedClient(string plan = "pro")` → `HttpClient` con `Authorization: Bearer` real emitido por `ILicenseTokenService`.
  - `LicenseTestData.FreeKey`, `LicenseTestData.ValidKey`, `LicenseTestData.ProLicenseId`.

- [ ] **Step 1: Tests de autenticación (rojo)**

```csharp
[Test] SinToken_Devuelve401();
[Test] TokenConFirmaInvalida_Devuelve401();
[Test] TokenVencido_Devuelve401();
[Test] LicenciaRevocadaConCheckPerRequest_Devuelve403();
[Test] LicenciaRevocadaSinCheckPerRequest_SigueEntrandoHastaVencer();
[Test] PlanFree_RequestNumeroOnceEnElMinuto_Devuelve429();
[Test] PlanPro_NoCortaEnLaOnce();
```

Cada uno: emitir token con `LicenseTestData.TokenFor(plan, expira: ...)` (usa el `ILicenseTokenService` real resuelto de `_factory.Services`), `PostAsJsonAsync("/v1/decisions", PreflopRequest(), ...)` y assertear código y `EngineError.Code` (`unauthorized` / `license_revoked` / `rate_limited`).

- [ ] **Step 2: Correr y verificar rojo**

Antes de tocar `Program.cs`, los tests de 401 fallan con `200`; los de 429 fallan con `200`. Correr y confirmar.

- [ ] **Step 3: Configurar autenticación**

`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => { o.TokenValidationParameters = new() { ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30), IssuerSigningKey = new SymmetricSecurityKey(UTF8(jwt.SigningKey)), NameClaimType = "sub" }; })`. `AddAuthorization()`. Fijar `MapInboundClaims = false`.

- [ ] **Step 4: Proteger el endpoint y mapear 403**

`app.MapPost("/v1/decisions", ...).RequireAuthorization();` En `UseStatusCodePages`, traducir `401` → `EngineError("unauthorized", ...)` y `403` → `EngineError("license_revoked", ...)` cuando la respuesta no tenga cuerpo JSON.

- [ ] **Step 5: Revocación opcional por request**

`LicenseRevocationMiddleware`: si `LicenseOptions.CheckRevocationPerRequest` y hay `sub`, busca `ILicenseStore.GetAsync`; si no existe o `Revoked`, responde 403 `license_revoked`. Se registra **después** de `UseAuthentication()` y antes de los endpoints.

- [ ] **Step 6: Rate limit por plan**

En `PlanRateLimiting`, `AddPolicy(DecisionsPolicy, context => RateLimitPartition.GetFixedWindowLimiter(sub, _ => new FixedWindowRateLimiterOptions { PermitLimit = configuration.GetValue($"RateLimit:Plans:{plan}:PermitsPerMinute", plan == "pro" ? 60 : 10), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }))`, con `plan` leído del claim y `sub` como partición. `.RequireRateLimiting(DecisionsPolicy)` en `/v1/decisions`.

- [ ] **Step 7: Adaptar los tests existentes al Bearer**

`ApiFactory` registra `ILicenseStore` en memoria, `JwtOptions` con una key de test de 32+ bytes y `LicenseOptions.Pepper`. `CreateLicensedClient` emite el token real. `DecisionEndpointTests` y `ParidadHttpTests` usan ese cliente; `HttpDecisionEngine` recibe un `LicenseTokenStore` de test con el token.

- [ ] **Step 8: Verificar y commitear**

Run: `--filter "FullyQualifiedName~DecisionAuthTests"` → PASS 7/7.
Run: `--filter "FullyQualifiedName~ParidadHttpTests"` → PASS 2/2 con los 15 escenarios.
Run: `dotnet test OpenScrape.sln` → verde.

```bash
git add -A
git commit -m "feat(api): exigir licencia y aplicar cuotas por plan"
```

---

### Task 5: CLI de licencias

**Files:**
- Create: `src/Tools/LicenseCli/LicenseCli.csproj`
- Create: `src/Tools/LicenseCli/Program.cs`
- Modify: `OpenScrape.sln`
- Test: `test/OpenScrape.App.Tests/Api/LicenseCliTests.cs`

**Interfaces:**
- Consumes: `ILicenseStore`, `ILicenseKeyHasher`, `LicenseKeyGenerator`, `License`.
- Produces: `static class LicenseCommands { Task<int> CreateAsync(ILicenseStore store, ILicenseKeyHasher hasher, string plan, int days, TextWriter output); Task<int> RevokeAsync(ILicenseStore store, Guid id, TextWriter output); Task<int> ListAsync(ILicenseStore store, TextWriter output); }`

- [ ] **Step 1: Tests de los comandos (rojo)**

```csharp
[Test]
public async Task Create_ImprimeLaKeyUnaSolaVezYGuardaElHash()
{
    var store = new InMemoryLicenseStore();
    var output = new StringWriter();
    var exit = await LicenseCommands.CreateAsync(store, Hasher, "pro", 30, output);
    var key = output.ToString().Trim();
    Assert.That(exit, Is.EqualTo(0));
    Assert.That(key, Does.Match("^OSPK(-[A-Z2-9]{4}){3}$"));
    Assert.That(await store.FindByKeyHashAsync(Hasher.Hash(key)), Is.Not.Null);
}

[Test]
public async Task Revoke_IdInexistente_Devuelve1()
{
    Assert.That(await LicenseCommands.RevokeAsync(new InMemoryLicenseStore(), Guid.NewGuid(), new StringWriter()),
        Is.EqualTo(1));
}

[Test]
public async Task List_SinLicencias_EscribeElEncabezado()
{
    var output = new StringWriter();
    Assert.That(await LicenseCommands.ListAsync(new InMemoryLicenseStore(), output), Is.EqualTo(0));
    Assert.That(output.ToString(), Does.Contain("PLAN"));
}
```

- [ ] **Step 2: Correr y verificar rojo** — `CS0246: LicenseCommands`.

- [ ] **Step 3: Implementar los comandos**

`CreateAsync`: genera key, `AddAsync(new License { Id = Guid.NewGuid(), KeyHash = hasher.Hash(key), Plan = plan, ExpiresAt = now.AddDays(days), CreatedAt = now })`, escribe **sólo** la key en claro. `ListAsync`: una línea por licencia con `Id`, `Plan`, `ExpiresAt`, `Revoked` y encabezado `ID  PLAN  VENCE  REVOCADA`. `RevokeAsync`: `1` si no existe.

- [ ] **Step 4: Implementar el `Program.cs` de la CLI**

Parseo manual de `args` (`create --plan --days`, `revoke --id`, `list`), `Host.CreateApplicationBuilder`, `AddDataBase(...)`, `MartenLicenseStore`, `LicenseKeyHasher` con `License__Pepper`; sale con el código del comando. `--help` imprime las tres formas.

- [ ] **Step 5: Verificar y commitear**

Run: `--filter "FullyQualifiedName~LicenseCliTests"` → PASS 3/3.
Run: `dotnet build OpenScrape.sln` → 0/0.

```bash
git add -A
git commit -m "feat(tools): administrar licencias por linea de comandos"
```

---

### Task 6: El cliente adjunta la licencia

**Files:**
- Create: `src/OpenScrape.App/Services/License/LicenseTokenStore.cs`
- Create: `src/OpenScrape.App/Services/License/LicenseRequiredException.cs`
- Create: `src/OpenScrape.App/Services/License/LicenseClient.cs`
- Modify: `src/OpenScrape.App/Services/Http/HttpDecisionEngine.cs`
- Modify: `src/OpenScrape.App/Program.cs`
- Test: `test/OpenScrape.App.Tests/Services/LicenseTokenStoreTests.cs`
- Test: `test/OpenScrape.App.Tests/Services/HttpDecisionEngineTests.cs` (agregar casos)

**Interfaces:**
- Consumes: `Encrypter`, `LicenseActivateRequest/Response`, `EngineError`.
- Produces:
  - `sealed record StoredLicense(string Key, string Token, DateTimeOffset ExpiresAt, string Plan);`
  - `sealed class LicenseTokenStore(string path)` con `StoredLicense? Load()`, `void Save(StoredLicense)`, `void Clear()`, `bool HasValidToken(DateTimeOffset now)`.
  - `sealed class LicenseRequiredException(string message) : Exception`
  - `sealed class LicenseClient(HttpClient client) { Task<StoredLicense?> ActivateAsync(string key, CancellationToken); }`

- [ ] **Step 1: Test del almacén (rojo)**

```csharp
[Test]
public void Save_YLoad_DevuelveLoMismoSinGuardarNadaEnClaro()
{
    var path = Path.Combine(Path.GetTempPath(), $"lic-{Guid.NewGuid():N}.json");
    var store = new LicenseTokenStore(path);
    store.Save(new StoredLicense("OSPK-1111-2222-3333", "tok", DateTimeOffset.UtcNow.AddHours(1), "pro"));

    var loaded = store.Load();
    Assert.That(loaded!.Plan, Is.EqualTo("pro"));
    Assert.That(File.ReadAllText(path), Does.Not.Contain("OSPK-1111-2222-3333"));
    Assert.That(File.ReadAllText(path), Does.Not.Contain("tok"));
}

[Test]
public void HasValidToken_Vencido_EsFalse()
```

El cifrado usa el mismo `Encrypter` que la config de la app; el test lo construye con una key de prueba.

- [ ] **Step 2: Test del cliente HTTP con 401 (rojo)**

```csharp
[Test]
public async Task DecideAsync_SinToken_LanzaLicenseRequiredYSinRequest()
{
    var handler = new CapturingHandler(HttpStatusCode.Unauthorized);
    var engine = new HttpDecisionEngine(new HttpClient(handler) { BaseAddress = ... }, NullLogger<...>.Instance, store);
    Assert.ThrowsAsync<LicenseRequiredException>(async () => await engine.DecideAsync(PreflopRequest()));
    Assert.That(handler.Requests, Is.Empty);   // no se manda sin Bearer
}

[Test]
public async Task DecideAsync_TokenVencidoEnLocal_NoMandaRequestYLimpiaElStore()
```

- [ ] **Step 3: Implementar**

`LicenseTokenStore` serializa `StoredLicense` y cifra el JSON completo antes de escribirlo (archivo `%AppData%/OpenScrape/license.dat`). `HttpDecisionEngine` recibe `LicenseTokenStore?`; si hay token válido agrega `Authorization: Bearer`; si no, lanza `LicenseRequiredException` **antes** de tocar la red. En `401`/`403` hace `Clear()` y lanza `LicenseRequiredException` con el `EngineError` del cuerpo.

- [ ] **Step 4: Verificar y commitear**

Run: `--filter "FullyQualifiedName~LicenseTokenStoreTests|FullyQualifiedName~HttpDecisionEngineTests"` → PASS.
Run: `dotnet test OpenScrape.sln` → verde.

```bash
git add -A
git commit -m "feat(app): adjuntar la licencia al motor remoto"
```

---

### Task 7: Pantalla de activación y arranque

**Files:**
- Create: `src/OpenScrape.App/Forms/FormLicense.cs`
- Modify: `src/OpenScrape.App/Forms/FrmMain.cs`
- Modify: `src/OpenScrape.App/Program.cs`
- Test: `test/OpenScrape.App.Tests/Services/LicenseGateTests.cs`

**Interfaces:**
- Consumes: `LicenseClient`, `LicenseTokenStore`, `LicenseRequiredException`.
- Produces: `internal static bool LicenseGate.NeedsActivation(LicenseTokenStore store, DateTimeOffset now)` y `FormLicense(LicenseClient client, LicenseTokenStore store)`.

- [ ] **Step 1: Test de la compuerta (rojo)**

```csharp
[Test] NeedsActivation_SinArchivo_EsTrue();
[Test] NeedsActivation_TokenVigente_EsFalse();
[Test] NeedsActivation_TokenVencido_EsTrue();
[Test] NeedsActivation_ConKeyPeroSinToken_EsTrue();
```

- [ ] **Step 2: Implementar `LicenseGate`** y verificar 4/4.

- [ ] **Step 3: Construir `FormLicense`**

TextBox de key (sin log), botón **Validar** con estado ocupado, etiquetas de plan y vencimiento, mensaje de error con el `code` (`license_invalid`, `rate_limited`, red). Al validar: `LicenseClient.ActivateAsync`, `store.Save(...)`, `DialogResult = OK`. Diseñado a mano (sin designer) para no depender del diseñador de VS.

- [ ] **Step 4: Enganchar en `FrmMain`**

Al arrancar, si `LicenseGate.NeedsActivation` y `RemoteEngine:Enabled`, abrir `FormLicense` como diálogo antes de iniciar el loop; si el usuario cancela, no arrancar y mostrar el motivo. En el menú, un ítem **Licencia**. Al capturar `LicenseRequiredException` en el loop: detener, mostrar el motivo y abrir `FormLicense` una sola vez.

- [ ] **Step 5: Verificar y commitear**

Run: `dotnet test OpenScrape.sln` → verde.
Smoke manual: arrancar con `RemoteEngine:Enabled=true` sin token → aparece `FormLicense`; validar con la key de la CLI → arranca.

```bash
git add -A
git commit -m "feat(app): activar la licencia desde el cliente"
```

---

### Task 8: Despliegue y cierre

**Files:**
- Create: `src/OpenScrape.Api/Dockerfile`
- Modify: `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md` (3b como completada)
- Modify: `src/OpenScrape.Api/appsettings.json` (secciones `Jwt`, `License`, `RateLimit` con placeholders vacíos)

- [ ] **Step 1: Dockerfile multi-stage** con contexto en la raíz:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/OpenScrape.Api/OpenScrape.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "OpenScrape.Api.dll"]
```

- [ ] **Step 2: `appsettings.json`** con `Jwt:Issuer`, `Jwt:SigningKey` vacío, `License:Pepper` vacío, `License:CheckRevocationPerRequest=false`, `RateLimit:Plans:pro:PermitsPerMinute=60`, `RateLimit:Plans:free:PermitsPerMinute=10`. El arranque falla con mensaje claro si `SigningKey` mide menos de 32 bytes o `Pepper` está vacío.
  Además: `app.UseHttpsRedirection()` cuando `!app.Environment.IsDevelopment()`, y los logs de activación y de rechazo registran sólo el `sub`/id de licencia —nunca la key ni el token—.

- [ ] **Step 3: Test de arranque sin configuración** (`StartupValidationTests`): `WebApplicationFactory` sin la key → el host lanza con mensaje que nombra `Jwt:SigningKey`.

- [ ] **Step 4: Gate y spec**

`.\scripts\verify-pre-merge.ps1 -SkipSmoke` → `ALL CHECKPOINTS PASSED`, `preflop: 8 | postflop: 7`.
Marcar `3b — Licencias` como `Estado: completada` en la spec con el commit de cierre y el total de tests.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore(api): empaquetar el servicio y cerrar la fase 3b"
```
