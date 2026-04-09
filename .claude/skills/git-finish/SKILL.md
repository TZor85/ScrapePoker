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

2. Verificar si hay cambios sin commitear:
   ```bash
   git status --porcelain
   ```
   - Si hay cambios pendientes, **commitearlos automáticamente** antes de continuar.
   - Analizar los cambios con `git diff` y `git status` para generar el mensaje de commit.
   - El mensaje de commit debe seguir este formato (todo en inglés):
     ```
     [type/scope]: Brief description (max 50 chars)

     ### Changes Made
     - Change 1: Specific modification
     - Change 2: Specific modification
     - ...

     ### Implemented Solution
     Technical explanation of the fix (1-3 lines)

     ### Impact
     Problem solved and system effects

     Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
     ```
   - Types válidos: `feat`, `fix`, `chore`, `refactor`, `docs`, `test`, `perf`, `style`.
   - Stagear los archivos relevantes (no incluir archivos sensibles como .env).

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
