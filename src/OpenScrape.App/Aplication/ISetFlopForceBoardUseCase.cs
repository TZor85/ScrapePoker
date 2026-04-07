using OpenScrape.App.Entities;

namespace OpenScrape.App.Aplication
{
    public class SetFlopForceBoardUseCaseRequest
    {
        public PlayerGameState PlayerState { get; set; } = new PlayerGameState();
        public TableScrapeFlopResult TableScrapeFlopResult { get; set; } = new TableScrapeFlopResult();
    }

    public class SetFlopForceBoardUseCaseResponse
    {
        public PlayerGameState PlayerState { get; set; } = new PlayerGameState();
        public TableScrapeFlopResult TableScrapeFlopResult { get; set; } = new TableScrapeFlopResult();
    }

    public interface ISetFlopForceBoardUseCase
    {
        SetFlopForceBoardUseCaseResponse Execute(SetFlopForceBoardUseCaseRequest request);
    }
}
