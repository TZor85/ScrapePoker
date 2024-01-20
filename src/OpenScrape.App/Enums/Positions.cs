namespace OpenScrape.App.Enums
{
    public enum Positions
    {
        Default,
        OutOfPosition,
        InPosition
    }

    public enum HeroPosition
    {
        None,
        EarlyPosition,
        MiddlePosition,
        CutOff,
        Button,
        SmallBlind,
        BigBlind
    }

    public enum TypePreflop
    {
        HandClean,
        LimpedPot,
        RaisedPot,
        ThreeBetPot,
        FourBetPot
    }

    public enum HeroAction
    {
        None,
        OpenRaise,
        RaiseOverLimper,
        Call,
        ThreeBet,
        OpenRaiseVs3Bet,
        OpenRaiseVs3BetAndCall,
        FourBet,
        Cold4Bet,
        Squeeze,
        VsSqueeze
    }

    public enum BoardPosition
    {
        None,
        Flop,
        Turn,
        River
    }
    
}
