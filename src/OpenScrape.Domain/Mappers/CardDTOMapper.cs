using OpenScrape.Domain.Dtos;

namespace OpenScrape.Domain.Mappers;

public static class CardDTOMapper
{
    public static CardDTO ToDto(this Entities.Card card)
    {
        return new CardDTO
        {
            Name = card.Id,
            BinaryValue = card.BinaryValue,
            ImageBase64 = card.ImageBase64,
            Hall = card.Hall,
            Force = card.Force,
            Suit = card.Suit
        };
    }

    public static Entities.Card ToEntity(this CardDTO card)
    {
        return new Entities.Card
        {
            Id = card.Name,
            BinaryValue = card.BinaryValue,
            ImageBase64 = card.ImageBase64,
            Hall = card.Hall,
            Force = card.Force,
            Suit = card.Suit
        };
    }
}
