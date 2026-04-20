using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

public static class PositionCalculator
{
    public static TablePosition DetermineP0Position(int dealerPosition, List<Player> players)
    {
        var all = AssignAllPositions(dealerPosition, players);
        return all.TryGetValue(0, out var pos) ? pos : TablePosition.None;
    }

    public static Dictionary<int, TablePosition> AssignVillainPositions(int dealerPosition, List<Player> players)
    {
        var all = AssignAllPositions(dealerPosition, players);
        all.Remove(0);
        return all;
    }

    // Asigna posición a cada asiento !Empty. Los SitOut ocupan asiento físico, pero
    // tanto SB como BB saltan el tramo consecutivo de SitOut a su izquierda para
    // encontrar al siguiente jugador activo (moving blinds). Los SitOut "consumidos"
    // por el salto de ciegas quedan sin posición (asiento muerto esa mano).
    public static Dictionary<int, TablePosition> AssignAllPositions(int dealerPosition, List<Player> players)
    {
        var result = new Dictionary<int, TablePosition>();

        var ring = players
            .Where(p => p != null && !p.Empty)
            .OrderBy(p => p.ValuePosition)
            .ToList();

        if (ring.Count < 2)
            return result;

        int dealerIdx = ring.FindIndex(p => p.ValuePosition == dealerPosition);
        if (dealerIdx < 0)
            return result;

        int count = ring.Count;

        // Heads-up: el dealer es la SB, el otro es la BB
        if (count == 2)
        {
            result[ring[dealerIdx].ValuePosition] = TablePosition.SmallBlind;
            int otherIdx = (dealerIdx + 1) % 2;
            result[ring[otherIdx].ValuePosition] = TablePosition.BigBlind;
            return result;
        }

        // SB: primer asiento !SitOut a la izquierda del dealer
        int sbIdx = -1;
        for (int step = 1; step < count; step++)
        {
            int idx = (dealerIdx + step) % count;
            if (!ring[idx].SitOut)
            {
                sbIdx = idx;
                break;
            }
        }
        if (sbIdx < 0)
            return result;

        // BB: primer asiento !SitOut a la izquierda de la SB, sin rebasar al dealer
        int bbIdx = -1;
        for (int step = 1; step < count; step++)
        {
            int idx = (sbIdx + step) % count;
            if (idx == dealerIdx)
                break;
            if (!ring[idx].SitOut)
            {
                bbIdx = idx;
                break;
            }
        }
        if (bbIdx < 0)
            return result;

        result[ring[dealerIdx].ValuePosition] = TablePosition.Button;
        result[ring[sbIdx].ValuePosition] = TablePosition.SmallBlind;
        result[ring[bbIdx].ValuePosition] = TablePosition.BigBlind;

        // Los SitOut saltados (tramo dealer→SB y SB→BB) no reciben etiqueta.
        // El "effectiveCount" determina el set de labels del resto (Early/Middle/CutOff).
        int skippedForSB = (sbIdx - dealerIdx - 1 + count) % count;
        int skippedForBB = (bbIdx - sbIdx - 1 + count) % count;
        int effectiveCount = count - skippedForSB - skippedForBB;

        var labels = GetRemainingLabels(effectiveCount);
        int cursor = (bbIdx + 1) % count;
        int labelIndex = 0;
        while (cursor != dealerIdx && labelIndex < labels.Count)
        {
            result[ring[cursor].ValuePosition] = labels[labelIndex];
            cursor = (cursor + 1) % count;
            labelIndex++;
        }

        return result;
    }

    // Etiquetas para los asientos entre la BB y el dealer (sin incluirlos), yendo a la izquierda.
    private static List<TablePosition> GetRemainingLabels(int ringCount) =>
        ringCount switch
        {
            3 => new List<TablePosition>(),
            4 => new List<TablePosition> { TablePosition.CutOff },
            5 => new List<TablePosition> { TablePosition.Middle, TablePosition.CutOff },
            _ => new List<TablePosition> { TablePosition.Early, TablePosition.Middle, TablePosition.CutOff }
        };
}
