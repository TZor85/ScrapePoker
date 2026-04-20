---
name: git-commit
description: Commit de los cambios actuales en la rama en curso
disable-model-invocation: true
allowed-tools:
  - Bash(git:*)
argument-hint: "[mensaje opcional]"
---

## Hacer commit en la rama actual

Commitea los cambios pendientes en la rama actual con el formato estándar del proyecto.

### Pasos

1. Verificar que estamos en una rama válida:
   ```bash
   git branch --show-current
   ```
   - Si estamos en `develop` o `main`, **DETENER** y avisar al usuario.

2. Verificar que hay cambios para commitear:
   ```bash
   git status --porcelain
   ```
   - Si no hay cambios, avisar al usuario y no continuar.

3. Analizar los cambios para generar el mensaje de commit:
   ```bash
   git diff
   git diff --staged
   git status
   ```

4. Stagear los archivos relevantes:
   - No incluir archivos sensibles (`.env`, credenciales, `appsettings.Development.json`).
   - Stagear con `git add <archivos>` específicos, no `git add -A` salvo que sea seguro.

5. Crear el commit con este formato (todo en inglés):
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

   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
   ```
   - Si el usuario proporcionó `$ARGUMENTS`, usarlo como descripción del commit en lugar de generarla.
   - Types válidos: `feat`, `fix`, `chore`, `refactor`, `docs`, `test`, `perf`, `style`.

6. Confirmar al usuario:
   - Hash del commit creado.
   - Resumen de los archivos commiteados.
   - Recordar que NO se ha hecho push.
