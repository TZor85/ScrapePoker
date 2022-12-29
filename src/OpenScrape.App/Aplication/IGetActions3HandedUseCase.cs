using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{
    public interface IGetActions3HandedUseCase
    {
        GetActions3HandedResponse ExecuteButtonAction(GetActions3HandedRequest request);
        GetActions3HandedResponse ExecuteBigBlindAction(GetActions3HandedRequest request);
        GetActions3HandedResponse ExecuteSmallBlindAction(GetActions3HandedRequest request);
    }

    public class GetActions3HandedResponse
    {
        public GetActionsResponse Data { get; set; } = new GetActionsResponse();        
    }

    public class GetActions3HandedRequest
    {
        public string Card0 { get; set; } = string.Empty;
        public string Card1 { get; set; } = string.Empty;
        public double BetP1 { get; set; }
        public double BetP2 { get; set; }
        public double ChipsP1 { get; set; }
        public double ChipsP2 { get; set; }
        public bool P1Active { get; set; }
        public bool P2Active { get; set; }
        public double EffectiveStack { get; set; }
    }

    public class GetActionsResponse
    {
        public string Action { get; set; } = "FOLD";
        public Styles Style { get; set; } = default!;
        public Positions Position { get; set; } = default!;
        public PreflopAction PreflopAction { get; set; } = default!;
    }
}
