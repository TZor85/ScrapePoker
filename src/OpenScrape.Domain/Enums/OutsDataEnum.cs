namespace OpenScrape.Domain.Enums;

public enum Rank : byte
{
    Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10,
    Jack = 11, Queen = 12, King = 13, Ace = 14
}

public enum Suit : byte { Clubs = 1, Hearts = 2, Diamonds = 3, Spades = 4 }

public enum HandRank : byte
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
