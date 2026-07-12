using System.ComponentModel;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio que actualiza los controles WinForms con los resultados del pipeline
/// de decisión. Recibe referencias de controles vía Attach/Detach (mismo patrón
/// que IUiSyncService) y actualiza sin lógica de negocio.
/// </summary>
public interface IUiUpdateService : IDisposable
{
    /// <summary>
    /// Conecta el servicio a los controles WinForms que actualizará.
    /// Llámalo desde FrmMain_Load o desde Attach del UiSyncService.
    /// </summary>
    void Attach(ISynchronizeInvoke syncTarget, IFrmOverlay overlay, UiUpdateControls controls);

    /// <summary>
    /// Desconecta el servicio de los controles. Idempotente.
    /// </summary>
    void Detach();

    /// <summary>
    /// Actualiza toda la UI con los resultados de una iteración del pipeline.
    /// </summary>
    void UpdateResults(PlayerGameState state, PokerCalculationResult result, string? responseAction, string tableHand, string tableName);

    /// <summary>
    /// Actualiza la UI desde un <see cref="GameLoopResult"/> (ruta del coordinator).
    /// Construye internamente los objetos necesarios a partir de los campos disponibles.
    /// </summary>
    void UpdateResults(GameLoopResult result);

    /// <summary>
    /// Actualiza el overlay (proyección de decisión). Puede llamarse independientemente
    /// de UpdateResults cuando solo se necesita refrescar el overlay.
    /// </summary>
    void UpdateOverlay(PokerCalculationResult result);

    /// <summary>
    /// Establece la lista de cartas disponibles para GetCardImage.
    /// Se llama desde FrmMain.Load o cuando se recargan cartas.
    /// </summary>
    void SetCardList(List<Card> cards);
}

/// <summary>
/// Agrupa las referencias a controles WinForms que UiUpdateService necesita actualizar.
/// </summary>
public sealed record UiUpdateControls(
    Label LbAction,
    Label LbPositionAction,
    TextBox TbResume,
    // Player indicators (PictureBox con BackColor=Red para marcar dealer)
    PictureBox PbButtonPlayerOne,
    PictureBox PbButtonPlayerTwo,
    PictureBox PbButtonPlayerThree,
    PictureBox PbButtonPlayerFour,
    PictureBox PbButtonPlayerFive,
    PictureBox PbButtonHero,
    Label LbNamePlayerOne,
    Label LbNamePlayerTwo,
    Label LbNamePlayerThree,
    Label LbNamePlayerFour,
    Label LbNamePlayerFive,
    Label LbBetPlayerOne,
    Label LbBetPlayerTwo,
    Label LbBetPlayerThree,
    Label LbBetPlayerFour,
    Label LbBetPlayerFive,
    Label LbBetHero,
    Label LbPot,
    Label LbUserStack,
    Label LbTableHand,
    Label LbTableName,
    // Card images
    PictureBox PbHeroCard0,
    PictureBox PbHeroCard1,
    PictureBox PbBoard1,
    PictureBox PbBoard2,
    PictureBox PbBoard3,
    PictureBox PbBoard4,
    PictureBox PbBoard5);
