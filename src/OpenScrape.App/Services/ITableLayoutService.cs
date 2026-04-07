using System.Drawing;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio de detección y layout de jugadores en la mesa.
/// Encapsula detección de dealer, posiciones, estados de jugadores y aliases.
/// </summary>
public interface ITableLayoutService
{
    /// <summary>Estado del dealer: posición numérica del asiento (-1 si no detectado).</summary>
    int DealerValuePosition { get; }

    /// <summary>Estado del dealer: nombre del jugador dealer (ej: "P3").</summary>
    string DealerPosition { get; }

    /// <summary>Estado del dealer: nombre del dealer de la mano anterior (para detección de nueva mano).</summary>
    string PreviousDealerPlayerName { get; }

    /// <summary>Resetea el estado del dealer entre manos.</summary>
    void ResetDealerState();

    /// <summary>Actualiza PreviousDealerPlayerName con el valor actual de DealerPosition.</summary>
    void SavePreviousDealer();

    /// <summary>Detecta el dealer, asigna posiciones hero/villanos y lee aliases.</summary>
    void InitializePlayers(Image screenshot, PlayerGameState state);

    /// <summary>Detecta el botón de dealer escaneando regiones de color.</summary>
    void SetDealerPlayer(Image screenshot, PlayerGameState state);

    /// <summary>Detecta asientos vacíos por color y crea jugadores.</summary>
    void SetEmptyPlayer(Image screenshot, PlayerGameState state);

    /// <summary>Detecta jugadores activos (playing) por color.</summary>
    void SetActivePlayer(Image screenshot, PlayerGameState state);

    /// <summary>Detecta jugadores en sit-out por OCR.</summary>
    void SetSitOutPlayer(Image screenshot, PlayerGameState state);

    /// <summary>Detecta jugadores que han foldeado mid-hand.</summary>
    void DetectFoldedPlayers(Image screenshot, PlayerGameState state, GameState currentGameState);

    /// <summary>Refresca estados de jugadores (detecta quién dejó la mesa).</summary>
    void RefreshPlayerStates(Image screenshot, PlayerGameState state);

    /// <summary>Valida consistencia de estados de jugadores.</summary>
    void ValidatePlayerStates(PlayerGameState state);

    /// <summary>Asigna posiciones a villanos basándose en la posición del hero y el dealer.</summary>
    void SetVillainPosition(PlayerGameState state, TablePosition heroPosition, int dealerPosition);

    /// <summary>Determina si el hero está en posición (IP/OOP).</summary>
    void SetIsInPosition(PlayerGameState state);

    /// <summary>Lee aliases de todos los villanos por OCR.</summary>
    void SetAliasVillain(Image screenshot, PlayerGameState state);

    /// <summary>Re-lee aliases de jugadores activos que aún no tienen alias.</summary>
    void RetryEmptyAliases(Image screenshot, PlayerGameState state);
}
