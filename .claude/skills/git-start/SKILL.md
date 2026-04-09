---
name: git-start
description: Crea una nueva rama desde develop siguiendo git flow
disable-model-invocation: true
allowed-tools:
  - Bash(git:*)
argument-hint: <tipo/nombre-rama> (ej: feature/bankroll, bugfix/ocr-leak, refactor/cleanup)
---

## Crear rama desde develop (git flow)

Crea una nueva rama `$ARGUMENTS` desde develop.

### Pasos

1. Verificar que no hay cambios sin commitear:
   ```bash
   git status --porcelain
   ```
   - Si hay cambios pendientes, **DETENER** y avisar al usuario. No continuar.

2. Cambiar a develop y actualizar:
   ```bash
   git checkout develop
   git pull origin develop
   ```

3. Crear y cambiar a la nueva rama:
   ```bash
   git checkout -b $ARGUMENTS
   ```

4. Confirmar al usuario que la rama fue creada y está lista para trabajar.

### Validaciones
- El argumento es obligatorio. Si no se proporciona, pedir el nombre de la rama.
- Si la rama ya existe, avisar al usuario y no sobreescribirla.
- El nombre debe seguir el formato `tipo/nombre` (feature/, bugfix/, refactor/, hotfix/, docs/).
