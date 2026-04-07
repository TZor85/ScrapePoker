using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

public static class PositionCalculator
{
    public static TablePosition DetermineP0Position(int dealerPosition, List<Player> players)
    {
        var activeSeats = players
            .Where(p => !p.Empty && !p.SitOut)
            .Select(p => p.ValuePosition)
            .OrderBy(s => s)
            .ToList();

        int dealerSeat = dealerPosition;
        int heroSeat = 0;

        if (!activeSeats.Contains(dealerSeat) || !activeSeats.Contains(heroSeat))
            return TablePosition.None;

        int dealerIndex = activeSeats.IndexOf(dealerSeat);
        int heroIndex = activeSeats.IndexOf(heroSeat);

        int distance = (heroIndex - dealerIndex + activeSeats.Count) % activeSeats.Count;

        return activeSeats.Count switch
        {
            2 => distance switch { 0 => TablePosition.SmallBlind, _ => TablePosition.BigBlind },
            3 => distance switch { 0 => TablePosition.Button, 1 => TablePosition.SmallBlind, _ => TablePosition.BigBlind },
            4 => distance switch { 0 => TablePosition.Button, 1 => TablePosition.SmallBlind, 2 => TablePosition.BigBlind, _ => TablePosition.CutOff },
            5 => distance switch { 0 => TablePosition.Button, 1 => TablePosition.SmallBlind, 2 => TablePosition.BigBlind, 3 => TablePosition.Middle, _ => TablePosition.CutOff },
            6 => distance switch { 0 => TablePosition.Button, 1 => TablePosition.SmallBlind, 2 => TablePosition.BigBlind, 3 => TablePosition.Early, 4 => TablePosition.Middle, _ => TablePosition.CutOff },
            _ => distance switch { 0 => TablePosition.Button, 1 => TablePosition.SmallBlind, 2 => TablePosition.BigBlind, 3 => TablePosition.Early, 4 => TablePosition.Middle, _ => TablePosition.CutOff }
        };
    }

    public static Dictionary<int, TablePosition> AssignVillainPositions(TablePosition heroPosition, List<Player> players)
    {
        var result = new Dictionary<int, TablePosition>();

        var activePlayers = players
            .Where(p => p != null && !p.Empty && !p.SitOut && p.ValuePosition != 0)
            .OrderBy(p => p.ValuePosition)
            .ToList();

        if (!activePlayers.Any())
            return result;

        var positionsOrder = GetPositionsOrder(heroPosition, activePlayers.Count);

        int positionIndex = 0;
        foreach (var player in activePlayers)
        {
            if (positionIndex < positionsOrder.Count)
            {
                result[player.ValuePosition] = positionsOrder[positionIndex];
                positionIndex++;
            }
        }

        return result;
    }

    public static Dictionary<int, TablePosition> AssignVillainPositionsWithDealer(int dealerPosition, List<Player> players)
    {
        var result = new Dictionary<int, TablePosition>();

        var activePlayers = players
            .Where(p => p != null && !p.Empty && !p.SitOut && p.ValuePosition != 0)
            .OrderBy(p => p.ValuePosition)
            .ToList();

        if (!activePlayers.Any())
            return result;

        var activeSeats = activePlayers.Select(p => p.ValuePosition).ToList();
        
        int dealerIndex;
        if (!activeSeats.Contains(dealerPosition))
        {
            if (activeSeats.Count == 0)
                return result;
            
            dealerIndex = 0;
            dealerPosition = activeSeats[0];
        }
        else
        {
            dealerIndex = activeSeats.IndexOf(dealerPosition);
        }

        int playerCount = activePlayers.Count;

        for (int i = 0; i < activePlayers.Count; i++)
        {
            var player = activePlayers[i];
            int positionFromDealer = (i - dealerIndex + playerCount) % playerCount;

            TablePosition position = positionFromDealer switch
            {
                1 => TablePosition.SmallBlind,
                2 => TablePosition.BigBlind,
                _ => GetPositionFromOrder(positionFromDealer, playerCount)
            };

            result[player.ValuePosition] = position;
        }

        return result;
    }

    private static TablePosition GetPositionFromOrder(int positionFromDealer, int playerCount)
    {
        if (playerCount <= 3)
            return TablePosition.Button;

        return positionFromDealer switch
        {
            0 => TablePosition.Button,
            3 when playerCount >= 5 => TablePosition.CutOff,
            3 => TablePosition.Button,
            4 when playerCount >= 6 => TablePosition.Middle,
            4 when playerCount == 5 => TablePosition.CutOff,
            5 when playerCount >= 6 => TablePosition.Early,
            _ => TablePosition.CutOff
        };
    }

    private static List<TablePosition> GetPositionsOrder(TablePosition heroPosition, int playerCount)
    {
        var baseOrder = heroPosition switch
        {
            TablePosition.BigBlind => new List<TablePosition>
            {
                TablePosition.SmallBlind, TablePosition.Button, TablePosition.CutOff,
                TablePosition.Middle, TablePosition.Early
            },
            TablePosition.SmallBlind => new List<TablePosition>
            {
                TablePosition.BigBlind, TablePosition.Early, TablePosition.Middle,
                TablePosition.CutOff, TablePosition.Button
            },
            TablePosition.Button => new List<TablePosition>
            {
                TablePosition.SmallBlind, TablePosition.BigBlind, TablePosition.Early,
                TablePosition.Middle, TablePosition.CutOff
            },
            TablePosition.CutOff => new List<TablePosition>
            {
                TablePosition.Button, TablePosition.SmallBlind, TablePosition.BigBlind,
                TablePosition.Early, TablePosition.Middle
            },
            TablePosition.Early => new List<TablePosition>
            {
                TablePosition.Middle, TablePosition.CutOff, TablePosition.Button,
                TablePosition.SmallBlind, TablePosition.BigBlind
            },
            TablePosition.Middle => new List<TablePosition>
            {
                TablePosition.CutOff, TablePosition.Button, TablePosition.SmallBlind,
                TablePosition.BigBlind, TablePosition.Early
            },
            _ => new List<TablePosition>()
        };

        if (playerCount == 2)
        {
            baseOrder.Remove(TablePosition.Middle);
            baseOrder.Remove(TablePosition.Early);
            baseOrder.Remove(TablePosition.CutOff);
        }
        else if (playerCount == 3)
        {
            baseOrder.Remove(TablePosition.Middle);
            baseOrder.Remove(TablePosition.Early);
        }
        else if (playerCount == 4)
        {
            baseOrder.Remove(TablePosition.Middle);
        }

        return baseOrder;
    }
}
