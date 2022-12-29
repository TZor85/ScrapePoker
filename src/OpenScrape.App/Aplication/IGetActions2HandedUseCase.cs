
namespace OpenScrape.App.Aplication
{
    public interface IGetActions2HandedUseCase
    {
        GetActions2HandedResponse ExecuteOpenRaise(GetActions2HandedRequest request);
        GetActions2HandedResponse ExecuteVsPlayer(GetActions2HandedRequest request);
    }

    public class GetActions2HandedResponse
    {
        public GetActionsResponse Data { get; set; } = new GetActionsResponse();
    }

    public class GetActions2HandedRequest
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

    
}
