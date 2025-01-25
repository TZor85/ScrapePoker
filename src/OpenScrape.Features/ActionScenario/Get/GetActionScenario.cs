using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.Features.Table;

namespace OpenScrape.Features.ActionScenario.Get;

public class GetActionScenario
{
    private TableUseCases tableUseCases;

    public GetActionScenario(TableUseCases tableUseCases)
    {
        this.tableUseCases = tableUseCases;
    }

    public async Task<string> ExecuteAsync(GameSituation situation, ActionScenarioRequest request)
    {
        try
        {
            var table = await tableUseCases.GetTable.ExecuteAsync(situation.GetDescription());
            if (table?.Value == null)
                throw new Exception("Table not found");

            var hands = table.Value.Positions?
                        .Where(w => w.HeroPosition == request.HeroPosition?.GetDescription()
                                && (request.OpenRaiser == null || w.OpenRaiser == request.OpenRaiser.GetDescription())
                                && (request.ThreeBetPosition == null || w.ThreeBetPosition == request.ThreeBetPosition.GetDescription())
                                && (request.Limper == null || w.Limper == request.Limper.GetDescription())
                                && (request.Caller == null || w.Caller == request.Caller.GetDescription())
                                && (request.Squeezer == null || w.Squeezer == request.Squeezer.GetDescription())
                                //&& (request.BetSize == null || w.BetSize == request.BetSize)
                                && (request.IsGreater == null || w.IsGreater == request.IsGreater)
                                && (request.RaiserFolds == null || w.RaiserFolds == request.RaiserFolds))
                        .FirstOrDefault()?.Hands
                        .Where(f => f.Name == request.HandName && f.Suited == request.Suited);

            if (hands == null)
                return "Fold";
            else
            {
                var action = GetRandomAction(hands?.ToList() ?? new List<Hand>());
                return string.IsNullOrEmpty(action) ? "Fold" : action;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error executing {situation.GetDescription()} scenario: {ex.Message}");
        }
    }

    private string GetRandomAction(List<Hand> actions)
    {
        if (actions.Count != 0)
        {
            // Verificamos que la lista no esté vacía
            if (!actions.Any())
                throw new ArgumentException("La lista de acciones está vacía");

            // Verificamos que los porcentajes sumen 100
            if (actions.Sum(a => a.Percentage) != 100)
                throw new ArgumentException("Los porcentajes deben sumar 100");

            // Generamos un número aleatorio entre 1 y 100
            Random random = new Random();
            int randomNumber = random.Next(1, 101);

            // Acumulamos los porcentajes para crear rangos
            int accumulatedPercentage = 0;

            foreach (var action in actions)
            {
                accumulatedPercentage += action.Percentage;
                if (randomNumber <= accumulatedPercentage)
                {
                    return action.Action;
                }
            }

            // En caso de algún error de redondeo, devolvemos la última acción
            return actions.Last()?.Action ?? string.Empty;
        }

        return string.Empty;
    }
}
