## ADDED Requirements

### Requirement: Generar hardware ID determinista
El sistema SHALL generar un identificador único del dispositivo combinando nombre de máquina, número de serie del primer disco fijo y la primera dirección MAC activa, aplicando SHA256 a la concatenación. El resultado SHALL ser el mismo en ejecuciones sucesivas en el mismo hardware.

#### Scenario: Generación exitosa
- **WHEN** se llama a `HardwareIdHelper.GetHardwareId()`
- **THEN** devuelve una cadena hexadecimal SHA256 (64 caracteres) no vacía

#### Scenario: Componente WMI no disponible
- **WHEN** WMI no puede obtener el número de serie del disco (ej. máquina virtual sin disco físico)
- **THEN** ese componente se sustituye por cadena vacía, NO se lanza excepción, y el ID sigue siendo generado con los componentes disponibles

#### Scenario: Consistencia entre llamadas
- **WHEN** se llama a `HardwareIdHelper.GetHardwareId()` varias veces en el mismo equipo
- **THEN** el valor devuelto es siempre idéntico
