using OpenScrape.Domain.Dtos;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato mínimo para CardCacheService — solo expone lo que TableReaderService necesita.
/// </summary>
public interface ICardCacheService
{
    Task<List<CardDTO>> GetCardsAsync();
}
