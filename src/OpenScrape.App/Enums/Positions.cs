using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    
}
