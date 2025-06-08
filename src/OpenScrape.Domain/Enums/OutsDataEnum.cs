namespace OpenScrape.Domain.Enums;

public enum Rank : byte
{
    Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    Jack = 11, Queen = 12, King = 13, Ace = 14
}

public enum Suit : byte { Clubs, Hearts, Diamonds, Spades }

public enum HandRanking : byte
{
    HighCard = 1,
    OnePair = 2,
    TwoPair = 3,
    ThreeOfAKind = 4,
    Straight = 5,
    Flush = 6,
    FullHouse = 7,
    FourOfAKind = 8,
    StraightFlush = 9,
    RoyalFlush = 10
}
