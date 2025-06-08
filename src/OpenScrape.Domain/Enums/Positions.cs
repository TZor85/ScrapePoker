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
