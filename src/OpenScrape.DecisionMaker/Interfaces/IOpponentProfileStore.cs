using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Interfaces;

public interface IOpponentProfileStore
{
    OpponentProfile? Load(string playerId);

    void Save(OpponentProfile profile);
}
