## 1. Verificar Build

- [ ] 1.1 dotnet build OpenScrape.sln --configuration Debug (sin errores)
- [ ] 1.2 dotnet build OpenScrape.sln --configuration Release (sin errores)

## 2. Verificar Format

- [ ] 2.1 dotnet format --verify-no-changes OpenScrape.sln (limpio)

## 3. Verificar Tests

- [ ] 3.1 dotnet test OpenScrape.sln --no-build (1173 tests pasando)

## 4. Verificar Warnings

- [ ] 4.1 Compilar y contar warnings (debe ser <= 75)

## 5. Verificar Dependencias

- [ ] 5.1 git diff main --stat -- '**/*.csproj' (sin cambios)

## 6. Smoke Test Manual

- [ ] 6.1 Ejecutar aplicación
- [ ] 6.2 Ir a pestaña "Métricas"
- [ ] 6.3 Verificar que timer inicia (label "Actualizado" cambia)
- [ ] 6.4 Click "Reset sesión"
- [ ] 6.5 Ejecutar ciclo de captura (btnCapture)
- [ ] 6.6 Ver métricas en pestaña (P50/P95/Max/Count)
- [ ] 6.7 Cambiar a otra pestaña, volver a Métricas
- [ ] 6.8 Verificar timer detenido/reiniciado correctamente