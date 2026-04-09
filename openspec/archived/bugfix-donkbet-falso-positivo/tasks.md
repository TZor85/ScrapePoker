# Tasks: Bugfix DonkBet Falso Positivo

## BUG 1+2: DetectDonkBet requiere agresor real

- [ ] Modificar `GameCoordinator.DetectDonkBet`: excluir hero (ValuePosition=0) del check de `villainWasPreflopAggressor`
- [ ] Añadir check `heroIsAggressor`: hero.WasPreflopAggressor OR heroWasPreviousStreetAggressor
- [ ] Early return `(false, currentSituation)` si `!heroIsAggressor`
- [ ] Escribir 6 tests unitarios (ver spec)
- [ ] Verificar build y tests existentes pasan

## Verificacion Final

- [ ] `dotnet build OpenScrape.sln`
- [ ] `dotnet test OpenScrape.sln`
