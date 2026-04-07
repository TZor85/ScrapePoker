## ADDED Requirements

### Requirement: Mostrar formulario de login antes de la app principal
El sistema SHALL mostrar `FrmLogin` como diálogo modal antes de abrir `FrmMain`. Si el usuario cierra `FrmLogin` sin autenticarse, la aplicación SHALL terminar.

#### Scenario: App arranca y muestra FrmLogin
- **WHEN** la aplicación se inicia
- **THEN** aparece `FrmLogin` centrado en pantalla antes de cualquier otra ventana

#### Scenario: Cancelar cierra la app
- **WHEN** el usuario cierra `FrmLogin` mediante el botón X o cancela
- **THEN** la aplicación termina sin abrir `FrmMain`

#### Scenario: Login exitoso abre FrmMain
- **WHEN** la validación de licencia devuelve éxito
- **THEN** `FrmLogin` se cierra con `DialogResult.OK` y se abre `FrmMain`

### Requirement: Campo de entrada de clave de licencia
El formulario SHALL contener un campo de texto para introducir la clave de licencia con formato `XXXX-XXXX-XXXX-XXXX`.

#### Scenario: Introducir clave y activar
- **WHEN** el usuario escribe una clave en el campo y pulsa el botón "Activar"
- **THEN** el sistema inicia la validación de la clave contra la BD

#### Scenario: Botón deshabilitado durante validación
- **WHEN** la validación está en curso
- **THEN** el botón "Activar" está deshabilitado para evitar envíos múltiples

### Requirement: Recordar clave de licencia entre sesiones
El formulario SHALL ofrecer una opción "Recordar licencia" que persista la clave en `Properties/Settings`. Si hay una clave guardada al arrancar, se SHALL autocompletar y validar automáticamente.

#### Scenario: Recordar clave marcado
- **WHEN** el usuario activa "Recordar licencia" y la validación es exitosa
- **THEN** la clave se guarda en `Properties/Settings`

#### Scenario: Clave guardada al arrancar
- **WHEN** existe una clave guardada en `Properties/Settings` al iniciar la app
- **THEN** el campo se autocompleta y se intenta validar automáticamente sin interacción del usuario

### Requirement: Mostrar mensajes de error de validación
El formulario SHALL mostrar el mensaje de error devuelto por la validación en un label visible cuando la licencia no es válida.

#### Scenario: Error de clave inválida
- **WHEN** la validación devuelve error
- **THEN** se muestra el mensaje de error en rojo bajo el campo de clave

#### Scenario: Limpiar error al reintentar
- **WHEN** el usuario modifica el campo de clave tras un error
- **THEN** el mensaje de error desaparece
