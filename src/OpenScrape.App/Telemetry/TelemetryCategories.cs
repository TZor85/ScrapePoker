namespace OpenScrape.App.Telemetry;

/// <summary>
/// Contrato estable de categorías de telemetría. Las cadenas forman parte del esquema
/// persistido en <c>HandRecord.Telemetry</c> y se consumen en la UI, no se renombran
/// sin migración.
/// </summary>
public static class TelemetryCategories
{
    // Ciclo completo
    public const string CycleTotal = "Cycle.Total";

    // Captura
    public const string CaptureScreenshot = "Capture.Screenshot";

    // OCR por tipo de lectura
    public const string OcrCards = "OCR.Cards";
    public const string OcrBets = "OCR.Bets";
    public const string OcrStacks = "OCR.Stacks";
    public const string OcrHandNumber = "OCR.HandNumber";
    public const string OcrPlayerNames = "OCR.PlayerNames";

    // Detección de layout
    public const string LayoutDealer = "Layout.Dealer";
    public const string LayoutPositions = "Layout.Positions";

    // Pipeline de decisión
    public const string DecisionTotal = "Decision.Total";
    public const string DecisionEquity = "Decision.Equity";
    public const string DecisionTexture = "Decision.Texture";
    public const string DecisionProfile = "Decision.Profile";
    public const string DecisionDecisionService = "Decision.DecisionService";
    public const string DecisionSizing = "Decision.Sizing";

    // Render overlay
    public const string OverlayRender = "Overlay.Render";

    // Persistencia (solo acumula en sesión, no en última mano)
    public const string PersistenceSaveHand = "Persistence.SaveHand";

    /// <summary>
    /// Orden de presentación en la pestaña "Métricas" de <c>FrmMain</c>.
    /// Categorías no listadas se muestran al final del grid.
    /// </summary>
    public static readonly IReadOnlyList<string> DisplayOrder = new[]
    {
        CycleTotal,
        CaptureScreenshot,
        OcrCards, OcrBets, OcrStacks, OcrHandNumber, OcrPlayerNames,
        LayoutDealer, LayoutPositions,
        DecisionTotal, DecisionEquity, DecisionTexture, DecisionProfile, DecisionDecisionService, DecisionSizing,
        OverlayRender,
        PersistenceSaveHand,
    };

    /// <summary>
    /// Categorías que <b>no</b> se persisten en <c>HandRecord.Telemetry</c>.
    /// <c>Persistence.SaveHand</c> se mide después del snapshot de la mano,
    /// así que solo tiene sentido en el acumulado de sesión.
    /// </summary>
    public static readonly IReadOnlySet<string> SessionOnly = new HashSet<string>
    {
        PersistenceSaveHand,
    };
}
