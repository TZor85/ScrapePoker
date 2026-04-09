---
name: git-start
description: Crea una rama feature/ desde develop
disable-model-invocation: true
allowed-tools:
  - Bash(git:*)
argument-hint: <nombre> (ej: bankroll-dashboard, ocr-improvements)
---

## Crear rama feature desde develop (git flow)

Crea una nueva rama `feature/$ARGUMENTS` desde develop.

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
   git checkout -b feature/$ARGUMENTS
   ```

4. Confirmar al usuario que la rama fue creada y está lista para trabajar.

### Validaciones
- El argumento es obligatorio. Si no se proporciona, pedir el nombre de la rama.
- Si la rama ya existe, avisar al usuario y no sobreescribirla.
- Si el argumento ya incluye el prefijo `feature/`, no duplicarlo.
