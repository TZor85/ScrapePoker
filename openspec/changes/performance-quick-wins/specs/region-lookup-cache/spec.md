## ADDED Requirements

### Requirement: Cache de regiones con lookup O(1)
SHALL existir un servicio `RegionLookupCache` que pre-compute un diccionario anidado `[mapId][regionName] → Region` a partir de `List<RegionTableMap>`, permitiendo lookups en O(1).

#### Scenario: Inicialización del cache
- **GIVEN** una lista de `RegionTableMap` con mapas "User", "Table", "Playing", "Empty", "Names", "Bets", "Board", "Dealer", "SitOut"
- **WHEN** se llama a `Initialize(maps)`
- **THEN** el cache contiene una entrada por cada combinación mapId/regionName presente en los mapas

#### Scenario: Lookup por mapId y regionName
- **GIVEN** un cache inicializado con un mapa "User" que tiene región "uAction"
- **WHEN** se llama a `GetRegion("User", "uAction")`
- **THEN** devuelve la misma instancia de `Region` que estaba en el mapa original

#### Scenario: Lookup de mapa inexistente
- **GIVEN** un cache inicializado
- **WHEN** se llama a `GetRegion("Inexistente", "uAction")`
- **THEN** devuelve `null` sin lanzar excepción

#### Scenario: Lookup de región inexistente
- **GIVEN** un cache inicializado con mapa "User" pero sin región "noExiste"
- **WHEN** se llama a `GetRegion("User", "noExiste")`
- **THEN** devuelve `null` sin lanzar excepción

#### Scenario: Obtener todas las regiones de un mapa
- **GIVEN** un cache inicializado con mapa "Playing" que tiene 9 regiones
- **WHEN** se llama a `GetRegions("Playing")`
- **THEN** devuelve una lista con las 9 regiones del mapa

### Requirement: Reemplazo de FirstOrDefault en FrmMain
Todas las llamadas a `_regionsTableMap?.FirstOrDefault(f => f.Id == "X")` en `FrmMain.cs` SHALL ser reemplazadas por llamadas a `RegionLookupCache`.

#### Scenario: Hot loop de detección usa cache
- **GIVEN** el game loop ejecutando `PerformEnhancedDetection`
- **WHEN** necesita las regiones "uAction" y "isFlop"
- **THEN** las obtiene via `_regionCache.GetRegion("User", "uAction")` y `_regionCache.GetRegion("Table", "isFlop")` en O(1)

#### Scenario: Métodos de scraping usan cache
- **GIVEN** métodos como `SetBetPlayer`, `SetHeroStack`, `SetPotValue`, `SetDealerPlayer`, etc.
- **WHEN** necesitan acceder a regiones de sus respectivos mapas
- **THEN** usan `_regionCache.GetRegion()` o `_regionCache.GetRegions()` en lugar de FirstOrDefault

#### Scenario: Cache se reinicializa al cambiar de mesa
- **GIVEN** que el usuario cambia de mesa y se carga un nuevo `_regionsTableMap`
- **WHEN** se asigna el nuevo mapa
- **THEN** se llama a `_regionCache.Initialize(nuevoMapa)` para reconstruir el diccionario
