using System.ComponentModel;
using System.Reflection;

namespace OpenScrape.Domain.Enums
{
    public enum Positions
    {
        None,
        OutOfPosition,
        InPosition
    }

    public enum TablePosition
    {
        None,
        Early,
        Middle,
        CutOff,
        Button,
        SmallBlind,
        BigBlind
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
        VsSqueeze,
        DonkBet,
        DonkBetVsOpenRaise,
        LimpRaise
    }

    public enum BoardPosition
    {
        None,
        Hand,
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

    public enum GameSituation
    {
        None,
        [Description("OpenRaise")]
        OpenRaise,
        [Description("BigBlindVsSmallBlind")]
        BigBlindVsSmallBlind,
        [Description("RaiseOverLimpers")]
        RaiseOverLimpers,
        [Description("ColdFourBet")]
        Cold4Bet,
        [Description("FourBet")]
        FourBet,
        [Description("Squeeze")]
        Squeeze,
        [Description("ThreeBet")]
        ThreeBet,
        [Description("VsSqueeze")]
        VsSqueeze,
        [Description("VsThreeBet")]
        VsThreeBet,
        [Description("VsThreeBetAndCall")]
        VsThreeBetAndCall
    }

    /// <summary>
    /// Clasificación de sub-tipo de par para decisiones postflop en turn/river.
    /// Ordinal refleja fuerza relativa: mayor valor = par más fuerte.
    /// </summary>
    public enum PairClassification : byte
    {
        None = 0,
        /// <summary>El par existe solo en el board; hero no contribuye con ninguna hole card.</summary>
        BoardPaired = 1,
        /// <summary>Hero empareja la carta más baja del board.</summary>
        BottomPair = 2,
        /// <summary>Pocket pair inferior a la carta más alta del board.</summary>
        PocketPairUnder = 3,
        /// <summary>Hero empareja una carta intermedia del board.</summary>
        MiddlePair = 4,
        /// <summary>Hero empareja la carta más alta del board.</summary>
        TopPair = 5,
        /// <summary>Pocket pair superior a todas las cartas del board.</summary>
        Overpair = 6
    }

    // Método para obtener la descripción
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }
    }
}
