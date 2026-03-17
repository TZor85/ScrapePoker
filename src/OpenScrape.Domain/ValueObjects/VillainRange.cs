using OpenScrape.Domain.Enums;

namespace OpenScrape.Domain.ValueObjects;

/// <summary>
/// Rango predefinido de manos del villano por HandSituation.
/// Cada entrada mapea "RankA_RankB_suited" → frecuencia (0.0 a 1.0).
/// El Monte Carlo selecciona manos ponderadas por frecuencia en vez de aleatorio puro.
/// </summary>
public class VillainRange
{
    /// <summary>
    /// Manos en el rango: clave = notación canónica (ej: "AA", "AKs", "AKo"), valor = frecuencia [0,1].
    /// </summary>
    public Dictionary<string, double> Hands { get; set; } = new();

    /// <summary>
    /// Nombre descriptivo del rango (ej: "Top 8% - vs 3Bet").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Porcentaje aproximado del total de manos que cubre este rango.
    /// </summary>
    public double RangePercentage { get; set; }

    /// <summary>
    /// Obtiene el rango predefinido del villano según la HandSituation.
    /// La lógica es: si hero hizo X, el villano que responde tiene un rango acorde.
    /// </summary>
    public static VillainRange? GetForSituation(HandSituation situation)
    {
        return situation switch
        {
            // Hero abrió raise → villano que paga/sube tiene rango medio-amplio
            HandSituation.OpenRaise => CreateRange("Caller vs OpenRaise", 25.0, _callerVsOpenRaise),

            // Hero hizo 3Bet → villano que paga tiene rango fuerte
            HandSituation.ThreeBet => CreateRange("Caller vs 3Bet", 10.0, _callerVs3Bet),

            // Hero enfrentó 3Bet (villano hizo 3Bet) → villano tiene rango fuerte
            HandSituation.OpenRaiseVs3Bet => CreateRange("3Bettor", 8.0, _threeBettor),

            // Hero enfrentó 3Bet y call (multiway) → rango medio
            HandSituation.OpenRaiseVs3BetAndCall => CreateRange("3Bet pot caller", 12.0, _threeBetPotCaller),

            // Hero hizo 4Bet → villano que paga tiene rango premium
            HandSituation.FourBet => CreateRange("Caller vs 4Bet", 5.0, _callerVs4Bet),

            // Hero hizo Cold 4Bet → villano tiene rango premium
            HandSituation.Cold4Bet => CreateRange("Cold 4Bet pot", 5.0, _callerVs4Bet),

            // Hero hizo call → villano que abrió tiene rango estándar de apertura
            HandSituation.Call => CreateRange("Open Raiser", 20.0, _openRaiser),

            // Hero hizo raise sobre limper → villano limper tiene rango amplio
            HandSituation.RaiseOverLimper => CreateRange("Limper", 40.0, _limper),

            // Hero hizo squeeze → villano tiene rango medio
            HandSituation.Squeeze => CreateRange("Caller vs Squeeze", 12.0, _callerVsSqueeze),

            // Hero enfrentó squeeze → villano squeezor tiene rango fuerte
            HandSituation.VsSqueeze => CreateRange("Squeezer", 8.0, _threeBettor),

            // Donk bet → villano tiene rango amplio (no continuó preflop)
            HandSituation.DonkBet => CreateRange("Donk Bettor", 30.0, _donkBettor),
            HandSituation.DonkBetVsOpenRaise => CreateRange("Donk vs OR", 25.0, _callerVsOpenRaise),

            // Sin situación → aleatorio puro (null = no filtrar)
            HandSituation.None => null,
            _ => null,
        };
    }

    private static VillainRange CreateRange(string name, double pct, Dictionary<string, double> hands)
    {
        return new VillainRange
        {
            Name = name,
            RangePercentage = pct,
            Hands = hands
        };
    }

    // ─── Rangos predefinidos ───────────────────────────────────────────

    /// <summary>
    /// Expande un rango de pares (ej: "TT+" → TT,JJ,QQ,KK,AA).
    /// </summary>
    private static readonly string[] AllRanks = ["2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A"];

    // Caller vs Open Raise (~25%): pares medianos+, broadways, suited connectors
    private static readonly Dictionary<string, double> _callerVsOpenRaise = new()
    {
        // Pares
        ["AA"] = 1.0, ["KK"] = 1.0, ["QQ"] = 1.0, ["JJ"] = 1.0, ["TT"] = 1.0,
        ["99"] = 1.0, ["88"] = 1.0, ["77"] = 1.0, ["66"] = 0.8, ["55"] = 0.7,
        ["44"] = 0.5, ["33"] = 0.3, ["22"] = 0.3,
        // Broadways suited
        ["AKs"] = 1.0, ["AQs"] = 1.0, ["AJs"] = 1.0, ["ATs"] = 1.0,
        ["KQs"] = 1.0, ["KJs"] = 1.0, ["KTs"] = 0.9,
        ["QJs"] = 1.0, ["QTs"] = 0.9, ["JTs"] = 1.0,
        // Broadways offsuit
        ["AKo"] = 1.0, ["AQo"] = 1.0, ["AJo"] = 0.8, ["ATo"] = 0.6,
        ["KQo"] = 0.9, ["KJo"] = 0.7, ["QJo"] = 0.6, ["JTo"] = 0.5,
        // Suited connectors
        ["T9s"] = 0.8, ["98s"] = 0.7, ["87s"] = 0.6, ["76s"] = 0.5, ["65s"] = 0.4,
        // Suited aces
        ["A9s"] = 0.7, ["A8s"] = 0.6, ["A7s"] = 0.5, ["A6s"] = 0.4, ["A5s"] = 0.6, ["A4s"] = 0.5, ["A3s"] = 0.4, ["A2s"] = 0.3,
    };

    // 3Bettor (~8%): rango fuerte polarizado
    private static readonly Dictionary<string, double> _threeBettor = new()
    {
        ["AA"] = 1.0, ["KK"] = 1.0, ["QQ"] = 1.0, ["JJ"] = 1.0, ["TT"] = 0.7,
        ["AKs"] = 1.0, ["AQs"] = 1.0, ["AJs"] = 0.8, ["ATs"] = 0.5,
        ["KQs"] = 0.8, ["KJs"] = 0.4,
        ["AKo"] = 1.0, ["AQo"] = 0.7,
        // Bluffs (suited)
        ["A5s"] = 0.6, ["A4s"] = 0.5, ["A3s"] = 0.4,
        ["76s"] = 0.3, ["65s"] = 0.3, ["54s"] = 0.3,
    };

    // Caller vs 3Bet (~10%): manos fuertes que no 4betean
    private static readonly Dictionary<string, double> _callerVs3Bet = new()
    {
        ["AA"] = 0.3, ["KK"] = 0.3, ["QQ"] = 0.8, ["JJ"] = 1.0, ["TT"] = 1.0,
        ["99"] = 0.8, ["88"] = 0.5,
        ["AKs"] = 0.5, ["AQs"] = 1.0, ["AJs"] = 1.0, ["ATs"] = 0.8,
        ["KQs"] = 1.0, ["KJs"] = 0.7, ["QJs"] = 0.8, ["JTs"] = 0.7,
        ["AKo"] = 0.5, ["AQo"] = 0.8, ["AJo"] = 0.5,
        ["KQo"] = 0.6,
        ["T9s"] = 0.5, ["98s"] = 0.4, ["87s"] = 0.3,
    };

    // 3Bet pot caller (~12%): rango que flatteó un 3bet en multiway
    private static readonly Dictionary<string, double> _threeBetPotCaller = new()
    {
        ["QQ"] = 0.5, ["JJ"] = 1.0, ["TT"] = 1.0, ["99"] = 1.0, ["88"] = 0.8, ["77"] = 0.5,
        ["AQs"] = 1.0, ["AJs"] = 1.0, ["ATs"] = 0.9,
        ["KQs"] = 1.0, ["KJs"] = 0.8, ["QJs"] = 0.9, ["JTs"] = 0.8,
        ["AQo"] = 0.7, ["AJo"] = 0.5,
        ["KQo"] = 0.5,
        ["T9s"] = 0.6, ["98s"] = 0.5, ["87s"] = 0.4,
    };

    // Caller vs 4Bet (~5%): solo premium
    private static readonly Dictionary<string, double> _callerVs4Bet = new()
    {
        ["AA"] = 1.0, ["KK"] = 1.0, ["QQ"] = 1.0, ["JJ"] = 0.5,
        ["AKs"] = 1.0, ["AKo"] = 0.8,
        ["AQs"] = 0.5,
    };

    // Open Raiser (~20%): rango estándar de apertura
    private static readonly Dictionary<string, double> _openRaiser = new()
    {
        ["AA"] = 1.0, ["KK"] = 1.0, ["QQ"] = 1.0, ["JJ"] = 1.0, ["TT"] = 1.0,
        ["99"] = 1.0, ["88"] = 1.0, ["77"] = 1.0, ["66"] = 0.8, ["55"] = 0.6,
        ["AKs"] = 1.0, ["AQs"] = 1.0, ["AJs"] = 1.0, ["ATs"] = 1.0, ["A9s"] = 0.8,
        ["A8s"] = 0.6, ["A7s"] = 0.5, ["A6s"] = 0.4, ["A5s"] = 0.7, ["A4s"] = 0.5,
        ["KQs"] = 1.0, ["KJs"] = 1.0, ["KTs"] = 0.9,
        ["QJs"] = 1.0, ["QTs"] = 0.8, ["JTs"] = 1.0,
        ["AKo"] = 1.0, ["AQo"] = 1.0, ["AJo"] = 0.9, ["ATo"] = 0.7,
        ["KQo"] = 1.0, ["KJo"] = 0.7, ["QJo"] = 0.5,
        ["T9s"] = 0.7, ["98s"] = 0.6, ["87s"] = 0.5, ["76s"] = 0.4,
    };

    // Limper (~40%): rango amplio y débil
    private static readonly Dictionary<string, double> _limper = new()
    {
        // Pares (limp-call con pares pequeños)
        ["AA"] = 0.2, ["KK"] = 0.2, ["QQ"] = 0.3, ["JJ"] = 0.4, ["TT"] = 0.5,
        ["99"] = 0.8, ["88"] = 0.9, ["77"] = 1.0, ["66"] = 1.0, ["55"] = 1.0,
        ["44"] = 1.0, ["33"] = 1.0, ["22"] = 1.0,
        // Suited broadways
        ["AKs"] = 0.2, ["AQs"] = 0.3, ["AJs"] = 0.5, ["ATs"] = 0.6,
        ["KQs"] = 0.5, ["KJs"] = 0.6, ["KTs"] = 0.7,
        ["QJs"] = 0.7, ["QTs"] = 0.8, ["JTs"] = 0.8,
        // Suited connectors y gappers
        ["T9s"] = 0.9, ["98s"] = 0.9, ["87s"] = 0.9, ["76s"] = 0.9, ["65s"] = 0.8, ["54s"] = 0.7,
        // Suited aces
        ["A9s"] = 0.7, ["A8s"] = 0.8, ["A7s"] = 0.8, ["A6s"] = 0.8, ["A5s"] = 0.7, ["A4s"] = 0.7,
        ["A3s"] = 0.7, ["A2s"] = 0.6,
        // Offsuit broadways
        ["AKo"] = 0.3, ["AQo"] = 0.4, ["AJo"] = 0.6, ["ATo"] = 0.7,
        ["KQo"] = 0.6, ["KJo"] = 0.7, ["KTo"] = 0.7,
        ["QJo"] = 0.8, ["QTo"] = 0.7, ["JTo"] = 0.8, ["T9o"] = 0.6, ["98o"] = 0.5,
        // Suited kings bajos
        ["K9s"] = 0.7, ["K8s"] = 0.5, ["K7s"] = 0.4, ["K6s"] = 0.4,
        // Suited queens bajas
        ["Q9s"] = 0.6, ["Q8s"] = 0.4, ["J9s"] = 0.6, ["J8s"] = 0.4, ["T8s"] = 0.5,
    };

    // Caller vs Squeeze (~12%): rango medio
    private static readonly Dictionary<string, double> _callerVsSqueeze = new()
    {
        ["QQ"] = 0.8, ["JJ"] = 1.0, ["TT"] = 1.0, ["99"] = 0.9, ["88"] = 0.6,
        ["AQs"] = 1.0, ["AJs"] = 1.0, ["ATs"] = 0.8,
        ["KQs"] = 1.0, ["KJs"] = 0.7, ["QJs"] = 0.8, ["JTs"] = 0.7,
        ["AQo"] = 0.7, ["AJo"] = 0.5,
        ["KQo"] = 0.5,
        ["T9s"] = 0.5, ["98s"] = 0.4,
    };

    // Donk Bettor (~30%): rango amplio, a menudo manos medianas
    private static readonly Dictionary<string, double> _donkBettor = new()
    {
        ["AA"] = 0.3, ["KK"] = 0.3, ["QQ"] = 0.5, ["JJ"] = 0.7, ["TT"] = 0.8,
        ["99"] = 1.0, ["88"] = 1.0, ["77"] = 1.0, ["66"] = 0.9, ["55"] = 0.8,
        ["44"] = 0.7, ["33"] = 0.6, ["22"] = 0.5,
        ["AKs"] = 0.5, ["AQs"] = 0.6, ["AJs"] = 0.7, ["ATs"] = 0.8,
        ["KQs"] = 0.7, ["KJs"] = 0.8, ["KTs"] = 0.8,
        ["QJs"] = 0.8, ["QTs"] = 0.8, ["JTs"] = 0.9,
        ["AKo"] = 0.4, ["AQo"] = 0.5, ["AJo"] = 0.7, ["ATo"] = 0.7,
        ["KQo"] = 0.6, ["KJo"] = 0.7, ["QJo"] = 0.7, ["JTo"] = 0.8,
        ["T9s"] = 0.9, ["98s"] = 0.9, ["87s"] = 0.8, ["76s"] = 0.7, ["65s"] = 0.6,
        ["A9s"] = 0.7, ["A8s"] = 0.7, ["A7s"] = 0.6, ["A6s"] = 0.6, ["A5s"] = 0.6,
        ["A4s"] = 0.5, ["A3s"] = 0.5, ["A2s"] = 0.4,
        ["T9o"] = 0.5, ["98o"] = 0.4, ["87o"] = 0.3,
    };

    // ─── Conversión notación → cartas concretas ──────────────────────

    private static readonly Dictionary<char, Rank> CharToRank = new()
    {
        ['2'] = Rank.Two, ['3'] = Rank.Three, ['4'] = Rank.Four, ['5'] = Rank.Five,
        ['6'] = Rank.Six, ['7'] = Rank.Seven, ['8'] = Rank.Eight, ['9'] = Rank.Nine,
        ['T'] = Rank.Ten, ['J'] = Rank.Jack, ['Q'] = Rank.Queen, ['K'] = Rank.King, ['A'] = Rank.Ace,
    };

    private static readonly Suit[] AllSuits = [Suit.Hearts, Suit.Diamonds, Suit.Clubs, Suit.Spades];

    /// <summary>
    /// Expande una mano en notación (ej: "AKs", "QQ", "JTo") a todas las combinaciones
    /// concretas de 2 cartas posibles (suit combos).
    /// </summary>
    public static List<(CardDataOuts Card1, CardDataOuts Card2)> ExpandHandNotation(string notation)
    {
        var combos = new List<(CardDataOuts, CardDataOuts)>();

        if (notation.Length < 2) return combos;

        var rank1 = CharToRank.GetValueOrDefault(notation[0]);
        var rank2 = CharToRank.GetValueOrDefault(notation[1]);

        if (rank1 == default || rank2 == default) return combos;

        bool isPair = notation.Length == 2 || (rank1 == rank2);
        bool isSuited = notation.Length == 3 && notation[2] == 's';
        // offsuit: notation[2] == 'o' o notation.Length == 2 con ranks diferentes

        if (isPair)
        {
            // 6 combos: cada par de suits distintos
            for (int i = 0; i < AllSuits.Length; i++)
            {
                for (int j = i + 1; j < AllSuits.Length; j++)
                {
                    combos.Add((new CardDataOuts(AllSuits[i], rank1), new CardDataOuts(AllSuits[j], rank2)));
                }
            }
        }
        else if (isSuited)
        {
            // 4 combos: mismo suit
            foreach (var suit in AllSuits)
            {
                combos.Add((new CardDataOuts(suit, rank1), new CardDataOuts(suit, rank2)));
            }
        }
        else
        {
            // 12 combos: suits diferentes
            for (int i = 0; i < AllSuits.Length; i++)
            {
                for (int j = 0; j < AllSuits.Length; j++)
                {
                    if (i != j)
                    {
                        combos.Add((new CardDataOuts(AllSuits[i], rank1), new CardDataOuts(AllSuits[j], rank2)));
                    }
                }
            }
        }

        return combos;
    }
}
