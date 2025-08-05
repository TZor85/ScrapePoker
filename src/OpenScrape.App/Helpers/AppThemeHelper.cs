namespace OpenScrape.App.Helpers
{
    /// <summary>
    /// Configuración de colores del tema de la aplicación
    /// </summary>
    public static class AppThemeHelper
    {
        // Paleta principal (máx. 3 colores base)
        public static readonly Color PrimaryDark = Color.FromArgb(45, 52, 67);
        public static readonly Color PrimaryLight = Color.FromArgb(99, 110, 131);
        public static readonly Color Accent = Color.FromArgb(0, 123, 191);

        // Colores de estado
        public static readonly Color Success = Color.FromArgb(40, 167, 69);
        public static readonly Color Warning = Color.FromArgb(255, 193, 7);
        public static readonly Color Danger = Color.FromArgb(220, 53, 69);

        // Colores de fondo
        public static readonly Color BackgroundMain = Color.FromArgb(248, 249, 250);
        public static readonly Color BackgroundCard = Color.White;
        public static readonly Color BorderLight = Color.FromArgb(222, 226, 230);
    }
}
