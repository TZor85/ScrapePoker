using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using OpenScrape.App.Entities;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio de detección y layout de jugadores en la mesa.
/// Gestiona dealer, posiciones, estados de jugadores y aliases.
/// </summary>
public class TableLayoutService : ITableLayoutService
{
    private readonly RegionLookupCache _regionLookupCache;
    private readonly ICoordinateScaler _coordinateScaler;
    private readonly IScreenReaderService _screenReader;
    private readonly IOpponentTracker _opponentTracker;

    // Constantes de color para detección por canal Blue
    private readonly List<int> _colorEmpty = new() { 14, 15, 53, 59, 74 };
    private readonly List<int> _colorPlaying = new() { 17 };

    // Estado del dealer (persiste entre llamadas dentro del scope)
    public int DealerValuePosition { get; private set; } = -1;
    public string DealerPosition { get; private set; } = string.Empty;
    public string PreviousDealerPlayerName { get; private set; } = string.Empty;

    public TableLayoutService(
        RegionLookupCache regionLookupCache,
        ICoordinateScaler coordinateScaler,
        IScreenReaderService screenReader,
        IOpponentTracker opponentTracker)
    {
        _regionLookupCache = regionLookupCache ?? throw new ArgumentNullException(nameof(regionLookupCache));
        _coordinateScaler = coordinateScaler ?? throw new ArgumentNullException(nameof(coordinateScaler));
        _screenReader = screenReader ?? throw new ArgumentNullException(nameof(screenReader));
        _opponentTracker = opponentTracker ?? throw new ArgumentNullException(nameof(opponentTracker));
    }

    #region [Estado dealer]

    public void ResetDealerState()
    {
        DealerValuePosition = -1;
        DealerPosition = string.Empty;
    }

    public void SavePreviousDealer()
    {
        PreviousDealerPlayerName = DealerPosition;
    }

    #endregion

    #region [Inicialización]

    public void InitializePlayers(Image screenshot, PlayerGameState state)
    {
        try
        {
            SetDealerPlayer(screenshot, state);

            LogDebug($"Dealer result: P{DealerValuePosition}, Position: {state.Position}, IsDealer: {state.IsDealer}, Players: {state.Players.Count}");

            if (DealerValuePosition >= 0)
                SetVillainPosition(state, state.Position, DealerValuePosition);

            SetAliasVillain(screenshot, state);
        }
        catch (Exception ex)
        {
            LogDebug($"Error en InitializePlayers: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region [Detección de dealer]

    public void SetDealerPlayer(Image screenshot, PlayerGameState state)
    {
        if (state.Players.Count == 0)
            return;

        var dealerRegionsList = _regionLookupCache.GetRegions("Dealer");
        if (dealerRegionsList == null || screenshot == null)
            return;

        // Limpiar flags de dealer previos
        state.Players.ForEach(p => p.Dealer = false);

        using var bitmap = new Bitmap(screenshot);

        var emptyPositions = state.Players
            .Where(w => w.Empty || w.SitOut)
            .Select(s => s.ValuePosition)
            .ToList();

        var allColorsLog = new StringBuilder();
        int? detectedDealerPosition = null;

        foreach (var region in dealerRegionsList.Where(x => x.IsColor.GetValueOrDefault()))
        {
            var scaled = GetScaledRegion(region, screenshot);
            var centerColor = bitmap.GetPixel(scaled.X, scaled.Y);
            allColorsLog.Append($"{region.Name}=RGB({centerColor.R},{centerColor.G},{centerColor.B}) ");

            bool isDealerColor = IsDealerButtonColor(bitmap, scaled.X, scaled.Y, searchRadius: 3);

            if (isDealerColor)
            {
                var playerNumber = GetPlayerNumber(region.Name, "dealer");
                if (playerNumber != null && detectedDealerPosition == null)
                {
                    detectedDealerPosition = playerNumber.Value;
                }
            }
        }

        LogDebug($"Dealer scan: {allColorsLog}| Detectado: {(detectedDealerPosition.HasValue ? $"P{detectedDealerPosition}" : "NINGUNO")} | Imagen: {bitmap.Width}x{bitmap.Height}");

        if (detectedDealerPosition.HasValue)
        {
            SetDealerForPlayer(state, detectedDealerPosition.Value, emptyPositions);
        }
    }

    private void SetDealerForPlayer(PlayerGameState state, int playerNumber, List<int> emptyPositions)
    {
        if (emptyPositions == null)
            throw new ArgumentNullException(nameof(emptyPositions));

        // P0: héroe es dealer
        if (playerNumber == 0)
        {
            state.IsDealer = true;
            state.Position = TablePosition.Button;
            DealerValuePosition = 0;

            var heroP0 = state.Players.FirstOrDefault(p => p.ValuePosition == 0);
            if (heroP0 != null)
            {
                heroP0.Position = TablePosition.Button;
                heroP0.Dealer = true;
            }

            SetVillainPosition(state, TablePosition.Button, 0);
            return;
        }

        var player = state.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");

        if (player != null)
        {
            if ((player.Empty || player.SitOut) && !player.Active)
                return;

            player.Dealer = true;
        }

        LogDebug($"Dealer assigned to player P{playerNumber}");

        var p0Pos = DetermineP0Position(state, playerNumber);
        LogDebug($"DetermineP0Position resultado: {p0Pos}, dealer: {playerNumber}, emptyPositions: [{string.Join(",", emptyPositions)}]");
        state.Position = p0Pos;

        var heroPlayer = state.Players.FirstOrDefault(p => p.ValuePosition == 0);
        if (heroPlayer != null)
        {
            heroPlayer.Position = p0Pos;
            LogDebug($"Héroe P0 position establecida: {p0Pos}");
        }

        PreviousDealerPlayerName = DealerPosition;
        DealerPosition = player?.Name ?? $"P{playerNumber}";
        DealerValuePosition = playerNumber;

        SetVillainPosition(state, p0Pos, playerNumber);
    }

    private static TablePosition DetermineP0Position(PlayerGameState state, int dealerPosition)
    {
        return PositionCalculator.DetermineP0Position(dealerPosition, state.Players);
    }

    private static bool IsDealerButtonColor(Bitmap bitmap, int centerX, int centerY, int searchRadius)
    {
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                int px = centerX + dx;
                int py = centerY + dy;

                if (px < 0 || py < 0 || px >= bitmap.Width || py >= bitmap.Height)
                    continue;

                var c = bitmap.GetPixel(px, py);

                // Dealer button dorado: R alto (>=200), G medio-alto (>=140), B bajo (<=80)
                if (c.R >= 200 && c.G >= 140 && c.B <= 80)
                    return true;
            }
        }

        return false;
    }

    #endregion

    #region [Detección de jugadores]

    public void SetEmptyPlayer(Image screenshot, PlayerGameState state)
    {
        var emptyRegionsList = _regionLookupCache.GetRegions("Empty");
        if (emptyRegionsList == null || screenshot == null)
            return;

        // P0 (héroe) siempre está activo
        if (!state.Players.Any(p => p.ValuePosition == 0))
        {
            var heroPlayer = CreatePlayerData(0);
            heroPlayer.Active = true;
            state.Players.Add(heroPlayer);
        }
        else
        {
            var existingHero = state.Players.FirstOrDefault(p => p.ValuePosition == 0);
            if (existingHero != null)
            {
                existingHero.Active = true;
            }
        }

        using var bitmap = new Bitmap(screenshot);

        foreach (var region in emptyRegionsList)
        {
            var playerNumber = GetPlayerNumber(region.Name, "empty");
            if (playerNumber == null)
                continue;

            var scaled = GetScaledRegion(region, screenshot);
            var color = bitmap.GetPixel(scaled.X, scaled.Y);
            var colorMatch = IsColorMatch(color.B, _colorEmpty);

            

            state.Players.Add(CreatePlayerData(playerNumber.Value));

            if (region.Name.Contains("empty") && colorMatch)
            {
                var player = state.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");
                if (player != null)
                {
                    player.Empty = true;
                }
            }
        }

        
    }

    public void SetActivePlayer(Image screenshot, PlayerGameState state)
    {
        var playingRegionsList = _regionLookupCache.GetRegions("Playing");
        if (playingRegionsList == null || screenshot == null)
            return;

        using var bitmap = new Bitmap(screenshot);

        foreach (var region in playingRegionsList)
        {
            var playerNumber = GetPlayerNumber(region.Name, "playing");
            if (playerNumber == null)
                continue;

            var scaled = GetScaledRegion(region, screenshot);
            var color = bitmap.GetPixel(scaled.X, scaled.Y);

            var player = state.Players.FirstOrDefault(n => n.Name == $"P{playerNumber}");
            bool isActive = region.Name.Contains("playing") && IsColorMatch(color.B, _colorPlaying);

            if (isActive)
            {
                if (player != null)
                {
                    player.Active = true;
                }
            }
            else
            {
                if (player != null)
                {
                    player.Active = false;
                }
            }
        }

        
    }

    public void SetSitOutPlayer(Image screenshot, PlayerGameState state)
    {
        var sitOutRegionsList = _regionLookupCache.GetRegions("SitOut");
        if (sitOutRegionsList == null || screenshot == null)
            return;

        var colorSitOutMap = new Dictionary<string, int>
        {
            {"p1sitout", 0},
            {"p2sitout", 0},
            {"p3sitout", 2},
            {"p4sitout", 1},
            {"p5sitout", 1}
        };

        foreach (var region in sitOutRegionsList)
        {
            var playerNumber = GetPlayerNumber(region.Name, "sitout");
            if (playerNumber == null)
                continue;

            var player = state.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
            if (player == null)
                continue;

            var scaled = GetScaledRegion(region, screenshot);

            bool isSittingOut = !player.Empty && !player.Active &&
                _screenReader.ReadText(screenshot,
                           scaled.X, scaled.Y, scaled.Width, scaled.Height,
                           region.Umbral, region.InactiveUmbral, region.IsOnlyNumber)
                .Contains("SIT");

            if (isSittingOut)
            {
                player.SitOut = true;
            }
        }
    }

    public void DetectFoldedPlayers(Image screenshot, PlayerGameState state, GameState currentGameState)
    {
        if (currentGameState == GameState.WaitingForHand ||
            currentGameState == GameState.HandComplete)
            return;

        var playingRegionsList = _regionLookupCache.GetRegions("Playing");
        if (playingRegionsList == null || screenshot == null) return;

        using var bitmap = new Bitmap(screenshot);

        foreach (var player in state.Players.Where(p => p.Active && !p.HasFolded && p.Name != "P0"))
        {
            var regionName = $"p{player.ValuePosition}playing";
            var region = _regionLookupCache.GetRegion("Playing", regionName);
            if (region == null) continue;

            var scaled = GetScaledRegion(region, screenshot);
            var color = bitmap.GetPixel(scaled.X, scaled.Y);

            if (!IsColorMatch(color.B, _colorPlaying))
            {
                player.HasFolded = true;
                player.Active = false;
                LogDebug($"[FOLD] {player.Name} ({player.Alias ?? "?"}) foldeó mid-hand");
            }
        }
    }

    public void RefreshPlayerStates(Image screenshot, PlayerGameState state)
    {
        var emptyRegionsList = _regionLookupCache.GetRegions("Empty");
        if (emptyRegionsList == null || screenshot == null) return;

        using var bitmap = new Bitmap(screenshot);

        foreach (var region in emptyRegionsList)
        {
            var playerNumber = GetPlayerNumber(region.Name, "empty");
            if (playerNumber == null || playerNumber == 0) continue;

            var player = state.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
            if (player == null) continue;

            var scaled = GetScaledRegion(region, screenshot);
            var color = bitmap.GetPixel(scaled.X, scaled.Y);

            bool wasEmpty = player.Empty;
            bool isNowEmpty = IsColorMatch(color.B, _colorEmpty);

            if (!wasEmpty && isNowEmpty)
            {
                player.Empty = true;
                player.Active = false;
                player.SitOut = false;
                LogDebug($"[LEFT] {player.Name} ({player.Alias ?? "?"}) dejó la mesa mid-session");
            }
        }
    }

    public void ValidatePlayerStates(PlayerGameState state)
    {
        foreach (var player in state.Players.Where(p => p.Name != "P0"))
        {
            if (!player.Active && !player.Empty && !player.SitOut &&
                string.IsNullOrEmpty(player.Alias) &&
                player.Stack == 0 && player.Bet == 0)
            {
                player.Empty = true;
            }

            if (player.Active && player.Stack == 0 && player.Bet == 0 && !player.HasFolded)
            {
                LogDebug($"[WARNING] {player.Name} activo pero stack=0, bet=0 — posible detección incorrecta");
            }
        }
    }

    #endregion

    #region [Posiciones]

    public void SetVillainPosition(PlayerGameState state, TablePosition heroPosition, int dealerPosition)
    {
        var allPlayers = state.Players.ToList();
        if (allPlayers.Count == 0)
            return;

        var activePlayers = allPlayers
            .Where(p => p != null && !p.Empty && !p.SitOut)
            .OrderBy(p => p.ValuePosition)
            .ToList();

        if (!activePlayers.Any())
            return;

        LogDebug($"SetVillainPosition - Jugadores activos: {string.Join(", ", activePlayers.Select(p => $"{p.Name}(VP:{p.ValuePosition},Empty:{p.Empty},SitOut:{p.SitOut})"))}, Posición héroe: {heroPosition}, Dealer: {dealerPosition}");

        // Limpiar posiciones previas en todos los asientos físicos (SitOut incluido) excepto héroe
        foreach (var p in allPlayers.Where(p => p.ValuePosition != 0))
        {
            p.Position = TablePosition.None;
        }

        var villainPositions = PositionCalculator.AssignVillainPositions(dealerPosition, allPlayers);

        foreach (var kvp in villainPositions)
        {
            var player = allPlayers.FirstOrDefault(p => p.ValuePosition == kvp.Key);
            if (player != null)
            {
                player.Position = kvp.Value;
            }
        }

        var positionLog = string.Join(", ", allPlayers.Where(p => !p.Empty).Select(p => $"{p.Name}:{p.Position}{(p.SitOut ? "(SitOut)" : "")}"));
        LogDebug($"Posiciones asignadas: {positionLog}");

        ValidatePositionAssignments(activePlayers);
    }

    public void SetIsInPosition(PlayerGameState state)
    {
        var activePlayers = state.Players.Where(w => w.Active &&
                                                     w.ValuePosition != 5 &&
                                                     w.ValuePosition != 6);

        state.IsInPosition = true;

        if (activePlayers.Any(item => (int)state.Position > item.ValuePosition) ||
            activePlayers.Any(item => item.Position == TablePosition.Button && item.Active))
        {
            state.IsInPosition = false;
        }

        if (state.Position == TablePosition.BigBlind &&
            state.Players.Any(a => a.Active && a.Position != TablePosition.SmallBlind))
        {
            state.IsInPosition = false;
        }

        if (state.Position == TablePosition.SmallBlind)
        {
            state.IsInPosition = false;
        }

        if (state.Position == TablePosition.BigBlind &&
            state.Players.Count(w => w.Active) == 1 &&
            state.Players.FirstOrDefault(w => w.Active)?.Position == TablePosition.SmallBlind)
        {
            state.IsInPosition = true;
        }
    }

    #endregion

    #region [Aliases]

    public void SetAliasVillain(Image screenshot, PlayerGameState state)
    {
        var namesRegionsList = _regionLookupCache.GetRegions("Names");
        if (namesRegionsList == null || screenshot == null)
            return;

        foreach (var region in namesRegionsList)
        {
            var playerNumber = GetPlayerNumber(region.Name, "Name");
            if (playerNumber == null) continue;

            var player = state.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
            if (player != null)
            {
                var scaled = GetScaledRegion(region, screenshot);
                double nameUmbral = Math.Min(region.Umbral ?? 0.80, 0.80);
                var cleanName = _screenReader.ReadPlayerName(screenshot,
                    scaled.X, scaled.Y, scaled.Width, scaled.Height,
                    nameUmbral, region.InactiveUmbral ?? 0.30);
                player.Alias = cleanName;

                if (!string.IsNullOrEmpty(cleanName) && !string.IsNullOrEmpty(player.Name))
                    _opponentTracker.RegisterSeatAlias(player.Name, cleanName);
            }
        }
    }

    public void RetryEmptyAliases(Image screenshot, PlayerGameState state)
    {
        var nameRegionsList = _regionLookupCache.GetRegions("Names");
        if (nameRegionsList == null || screenshot == null) return;

        foreach (var player in state.Players.Where(p => p.Active && string.IsNullOrEmpty(p.Alias)))
        {
            var regionName = $"p{player.ValuePosition}Name";
            var region = _regionLookupCache.GetRegion("Names", regionName);
            if (region == null) continue;

            var scaled = GetScaledRegion(region, screenshot);
            double nameUmbral = Math.Min(region.Umbral ?? 0.80, 0.80);
            var cleanName = _screenReader.ReadPlayerName(screenshot,
                scaled.X, scaled.Y, scaled.Width, scaled.Height,
                nameUmbral, region.InactiveUmbral ?? 0.30);

            if (!string.IsNullOrEmpty(cleanName))
            {
                player.Alias = cleanName;
                if (!string.IsNullOrEmpty(player.Name))
                    _opponentTracker.RegisterSeatAlias(player.Name, cleanName);
            }
        }
    }

    #endregion

    #region [Helpers privados]

    private (int X, int Y, int Width, int Height) GetScaledRegion(
        OpenScrape.Domain.ValueObjects.Region region, Image screenshot)
    {
        if (screenshot == null)
            return (region.PosX, region.PosY, region.Width, region.Height);

        return _coordinateScaler.ScaleRegion(
            region.PosX, region.PosY, region.Width, region.Height,
            screenshot.Width, screenshot.Height);
    }

    private static Player CreatePlayerData(int playerNumber) =>
        new Player
        {
            Name = $"P{playerNumber}",
            Active = false,
            Empty = false,
            SitOut = false,
            ValuePosition = playerNumber
        };

    private static int? GetPlayerNumber(string regionName, string extraText = "")
    {
        if (string.IsNullOrEmpty(regionName))
            return null;

        var match = Regex.Match(regionName, @$"p(\d+){extraText}");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private static bool IsColorMatch(int actualB, IEnumerable<int> expectedValues, int tolerance = 5)
    {
        return expectedValues.Any(expected => Math.Abs(actualB - expected) <= tolerance);
    }

    private void ValidatePositionAssignments(List<Player> players)
    {
        var dealers = players.Where(p => p.Dealer).ToList();
        if (dealers.Count != 1)
        {
            LogDebug($"Advertencia: Se encontraron {dealers.Count} dealers. Debe haber exactamente 1.");
        }

        var assignedPositions = players.Where(p => p.Position != TablePosition.None)
                                       .GroupBy(p => p.Position)
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key)
                                       .ToList();
        if (assignedPositions.Any())
        {
            LogDebug($"Advertencia: Posiciones duplicadas: {string.Join(", ", assignedPositions)}");
        }

        if (players.Count >= 2)
        {
            var hasSmallBlind = players.Any(p => p.Position == TablePosition.SmallBlind);
            var hasBigBlind = players.Any(p => p.Position == TablePosition.BigBlind);
            if (!hasSmallBlind || !hasBigBlind)
            {
                LogDebug("Advertencia: Faltan asignar SmallBlind o BigBlind.");
            }
        }

        if (players.Count >= 3)
        {
            var hasButton = players.Any(p => p.Position == TablePosition.Button);
            if (!hasButton)
            {
                LogDebug("Advertencia: Falta asignar Button.");
            }
        }
    }

    private static void LogDebug(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] {message}");
    }

    #endregion
}
