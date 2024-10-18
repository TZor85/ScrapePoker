namespace OpenScrape.App.Enums
{
    public enum Positions
    {
        None,
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

    public enum HandSituation
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

    public enum HeroHand
    {
        Nada,
        ProyectoColor,
        ProyectoEscalera,
        ProyectoEscaleraColor,
        CartaAlta,
        Pareja,
        DoblePareja,
        Trio,
        Escalera,
        Color,
        Full,
        Poker,
        EscaleraDeColor,
        EscaleraReal
    }
    
}
