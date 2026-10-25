using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Tracking;

public interface IOpponentProfileStore
{
    OpponentProfile? Load(string playerId);

    void Save(OpponentProfile profile);
}
