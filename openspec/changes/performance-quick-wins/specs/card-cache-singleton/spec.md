## ADDED Requirements

### Requirement: Servicio singleton de cache de cartas
SHALL existir un servicio `CardCacheService` registrado como singleton que carga las CardDTO desde Marten una única vez y las comparte con todos los consumidores.

#### Scenario: Primera carga desde base de datos
- **GIVEN** que no se han cargado cartas previamente
- **WHEN** se llama a `GetCardsAsync()` por primera vez
- **THEN** ejecuta `session.Query<Card>().ToListAsync()`, mapea a `List<CardDTO>`, y almacena en cache interno

#### Scenario: Cargas subsiguientes desde cache
- **GIVEN** que las cartas ya fueron cargadas en la primera llamada
- **WHEN** se llama a `GetCardsAsync()` una segunda vez (desde cualquier UseCase)
- **THEN** devuelve la lista cacheada sin hacer query a la base de datos

#### Scenario: Thread safety en carga concurrente
- **GIVEN** que múltiples threads llaman a `GetCardsAsync()` simultáneamente antes de que el cache se llene
- **WHEN** se resuelve la contención
- **THEN** solo se ejecuta una query a la BD (protegido por `SemaphoreSlim` o similar), los demás threads reciben el resultado cacheado

#### Scenario: Cache contiene las 52 cartas estándar
- **GIVEN** una BD con las 52 cartas de la baraja
- **WHEN** se completa la carga
- **THEN** el cache contiene exactamente 52 `CardDTO` con sus imágenes y metadatos

### Requirement: UseCases consumen CardCacheService
Los UseCases `GetCardsFlopUseCase`, `GetCardsTurnUseCase` y `GetCardsRiverUseCase` SHALL inyectar `CardCacheService` y usarlo en lugar de su cache interno y query directa.

#### Scenario: UseCase sin cache propio
- **GIVEN** un `GetCardsFlopUseCase` con `CardCacheService` inyectado
- **WHEN** necesita las cartas para comparar con la imagen capturada
- **THEN** llama a `_cardCache.GetCardsAsync()` y no mantiene campo `_cardsImages` propio

#### Scenario: Eliminación de query duplicada
- **GIVEN** que `GetCardsFlopUseCase` y `GetCardsTurnUseCase` se ejecutan en la misma mano
- **WHEN** ambos solicitan las cartas
- **THEN** solo existe una copia de las 52 CardDTO en memoria (la del singleton)

## REMOVED Requirements

### Requirement: Cache de cartas por instancia de UseCase
Se ELIMINA el patrón de cache `_cardsImages` interno en cada UseCase con lazy loading independiente vía `session.Query<Card>()`.
