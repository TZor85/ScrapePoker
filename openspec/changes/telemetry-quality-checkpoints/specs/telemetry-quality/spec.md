# telemetry-quality

## Descripción

Capacidad para validar la calidad del código de telemetría antes del merge a develop.

## Verificaciones requeridas

1. **Build**: Debug y Release sin errores
2. **Format**: dotnet format limpio
3. **Tests**: 1173 tests pasando
4. **Warnings**: <= 75
5. **Dependencies**: Sin cambios en .csproj
6. **Smoke Test**: 8 pasos manuales

## Smoke Test Pasos

1. Ejecutar aplicación
2. Ir a pestaña "Métricas"
3. Verificar timer inicia (label cambia)
4. Click "Reset sesión"
5. Ejecutar ciclo de captura
6. Ver métricas en grid
7. Cambiar a otra pestaña
8. Volver a Métricas - verificar timer