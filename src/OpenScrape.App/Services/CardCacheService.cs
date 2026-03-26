using Marten;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Mappers;

namespace OpenScrape.App.Services;

/// <summary>
/// Singleton que carga las 52 CardDTO una vez desde Marten y las comparte con todos los consumidores.
/// Elimina queries redundantes en GetCardsFlop/Turn/RiverUseCase.
/// </summary>
public class CardCacheService(IDocumentStore documentStore)
{
    private List<CardDTO>? _cards;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Obtiene las cartas cacheadas. La primera llamada ejecuta la query; las siguientes devuelven el cache.
    /// Thread-safe mediante SemaphoreSlim.
    /// </summary>
    public async Task<List<CardDTO>> GetCardsAsync()
    {
        if (_cards != null)
            return _cards;

        await _semaphore.WaitAsync();
        try
        {
            // Double-check después de adquirir el semáforo
            if (_cards != null)
                return _cards;

            await using var session = documentStore.LightweightSession();
            var cards = await session.Query<Card>().ToListAsync();
            _cards = cards.Select(c => c.ToDto()).ToList();

            return _cards;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
