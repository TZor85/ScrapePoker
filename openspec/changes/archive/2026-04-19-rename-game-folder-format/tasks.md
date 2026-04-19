## 1. Modificar formato de carpeta en FrmMain.cs

- [x] 1.1 Cambiar línea ~1780: `Game_{new DateOnly(...).ToString().Replace("/", "_")}` → `{DateTime.Now:yyyyMMdd}_Game`
- [x] 1.2 Cambiar línea ~2487: mismo formato a `{DateTime.Now:yyyyMMdd}_Game`

## 2. Verificar

- [x] 2.1 Compilar el proyecto para verificar que no hay errores
- [x] 2.2 Verificar formato generado coincide con carpetas existentes (`AAAAMMDD_Game`)