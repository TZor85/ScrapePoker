namespace OpenScrape.App.Services;

/// <summary>
/// Servicio que encapsula la configuración de estilos visuales del formulario
/// principal. Todos los métodos aceptan los controles WinForms por parámetro
/// y configuran sus propiedades de estilo (colores, fuentes, bordes, etc.)
/// sin depender del estado de FrmMain.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Inicializa los estilos visuales globales del formulario: BackColor
    /// y Font. NOTA: SetStyle (DoubleBuffer) es protected y debe llamarse
    /// desde FrmMain directamente, no desde este servicio.
    /// </summary>
    void InitializeVisualStyles(Form form);

    /// <summary>
    /// Aplica estilo moderno al TabControl completo: Appearance FlatButtons,
    /// colores de fondo, fuente, y recorre cada TabPage aplicando el estilo
    /// específico según su nombre.
    /// </summary>
    void ApplyModernTabStyle(TabControl tabControl);

    /// <summary>
    /// Aplica estilo moderno a la pestaña de juego (tbJuego): cartas,
    /// paneles de información, botones de acción y label de acción.
    /// </summary>
    void ApplyGameTabStyle(TabPage gameTab);

    /// <summary>
    /// Aplica estilo moderno a la pestaña de configuración (tbConfig):
    /// TreeView de regiones, GroupBox, botones de movimiento/tamaño,
    /// TextBoxes y CheckBoxes.
    /// </summary>
    void ApplyConfigTabStyle(TabPage configTab);

    /// <summary>
    /// Aplica estilo moderno a la pestaña de tablas (tbTables):
    /// TreeView de tablas y DataGridView de manos.
    /// </summary>
    void ApplyTablesTabStyle(TabPage tablesTab);

    /// <summary>
    /// Aplica estilo moderno a la pestaña de logs (tbLogs):
    /// TextBox de logs con fuente Consolas y colores oscuros.
    /// </summary>
    void ApplyLogsTabStyle(TabPage logsTab);

    /// <summary>
    /// Aplica estilo moderno a la pestaña de historial (tpHistorial):
    /// labels de título, DataGridViews de sesiones y manos, label de stats.
    /// </summary>
    void ApplyHistorialTabStyle(TabPage historialTab);

    /// <summary>
    /// Aplica estilo moderno a la pestaña de bankroll (tpBankroll):
    /// paneles de información con bordes pintados y DataGridView de historial.
    /// </summary>
    void ApplyBankrollTabStyle(TabPage bankrollTab);
}
