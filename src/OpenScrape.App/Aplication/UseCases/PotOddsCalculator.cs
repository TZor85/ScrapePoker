using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Aplication.UseCases;

public class PotOddsCalculator : IPotOddsCalculator
{
    private readonly IOutsCalculatorUseCase _outsCalculator;

    public PotOddsCalculator(IOutsCalculatorUseCase outsCalculator)
    {
        _outsCalculator = outsCalculator;
    }

    public PotOddsResult Calculate(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards, decimal currentPotSize, decimal betToCall, List<CardDataOuts> blockedCards = null)
    {
        // 1. Calcular los outs y la equity usando el código existente
        var handStrength = _outsCalculator.CalculateAllOuts(playerHand, communityCards);
        decimal equity = CalculateTotalEquity(handStrength, communityCards.Count);

        // 2. Calcular las pot odds
        decimal potAfterCall = currentPotSize + betToCall + betToCall; // Bote actual + apuesta rival + tu call
        decimal potOdds = 0;
        
        if (betToCall > 0) 
            potOdds = betToCall / potAfterCall * 100;

        // 3. Determinar si es rentable pagar
        bool shouldCall = equity > potOdds;

        // 4. Obtener la calle actual
        string street = GetStreetName(communityCards.Count);

        return new PotOddsResult
        {
            PotOddsPercentage = potOdds,
            EquityPercentage = equity,
            ShouldCall = shouldCall,
            Street = street
        };
    }

    // Método auxiliar: Calcular equity total (usando regla del 4 o 2)
    private decimal CalculateTotalEquity(HandStrength handStrength, int communityCardsCount)
    {
        int cardsToCome = communityCardsCount switch
        {
            3 => 2, // Flop: 2 cartas por venir
            4 => 1, // Turn: 1 carta por venir
            _ => 0  // Pre-flop o River: no aplica
        };

        // Sumar todos los outs únicos (sin duplicados)
        var allOuts = handStrength.FlushDraw.Outs
            .Concat(handStrength.StraightDraw.Outs)
            .Concat(handStrength.SetDraw.Outs)
            .DistinctBy(c => c.Id)
            .ToList();

        return cardsToCome switch
        {
            2 => allOuts.Count * 4,   // Regla del 4%
            1 => allOuts.Count * 2,   // Regla del 2%
            _ => 0                    // Pre-flop o River
        };
    }

    // Método auxiliar: Nombre de la calle
    private string GetStreetName(int communityCardsCount)
    {
        return communityCardsCount switch
        {
            0 => "Pre-Flop",
            3 => "Flop",
            4 => "Turn",
            5 => "River",
            _ => "Unknown"
        };
    }
}

