---
name: git-finish
description: Cierra la rama actual con merge a develop (git flow)
disable-model-invocation: true
allowed-tools:
  - Bash(git:*)
  - Bash(dotnet build:*)
  - Bash(dotnet test:*)
  - Bash(dotnet format:*)
---

## Cerrar rama y merge a develop (git flow)

Finaliza la rama actual mergeando a develop.

### Pasos

1. Identificar la rama actual:
   ```bash
   git branch --show-current
   ```
   - Si estamos en `develop` o `main`, **DETENER**: no hay rama feature para cerrar.

2. Verificar que no hay cambios sin commitear:
   ```bash
   git status --porcelain
   ```
   - Si hay cambios pendientes, **DETENER** y avisar al usuario que debe commitear o descartar antes de cerrar.

3. Validar build y tests:
   ```bash
   dotnet build OpenScrape.sln
   dotnet test OpenScrape.sln
   ```
   - Si el build falla o hay tests rotos, **DETENER** y reportar los errores. No mergear código roto.

4. Verificar formato:
   ```bash
   dotnet format --verify-no-changes OpenScrape.sln
   ```
   - Si hay problemas de formato, corregirlos con `dotnet format OpenScrape.sln` y commitear el fix antes de continuar.

5. Guardar el nombre de la rama actual y cambiar a develop:
   ```bash
   git checkout develop
   git pull origin develop
   ```

6. Merge con no-fast-forward (preserva historial de la rama):
   ```bash
   git merge <rama> --no-ff -m "merge: <rama> — <descripcion-breve>"
   ```
   - El mensaje de merge debe estar en castellano.
   - Usar formato: `merge: <rama> — <resumen de los cambios>`.
   - Generar la descripcion breve a partir de los commits de la rama.

7. Eliminar la rama local:
   ```bash
   git branch -d <rama>
   ```

8. Confirmar al usuario:
   - Rama mergeada y eliminada.
   - Resumen de los commits incluidos en el merge.
   - Recordar que NO se ha hecho push (el usuario decide cuándo).

### Manejo de conflictos
- Si el merge tiene conflictos, **DETENER** y listar los archivos en conflicto.
- No resolver conflictos automáticamente sin aprobacion del usuario.
