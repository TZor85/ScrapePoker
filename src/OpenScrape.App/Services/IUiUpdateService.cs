using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services;

public sealed record UiUpdateControls(
    TextBox TbResume,
    PictureBox PbHeroCard0,
    PictureBox PbHeroCard1,
    PictureBox PbBoard1,
    PictureBox PbBoard2,
    PictureBox PbBoard3,
    PictureBox PbBoard4,
    PictureBox PbBoard5,
    Label LbNamePlayerOne,
    Label LbNamePlayerTwo,
    Label LbNamePlayerThree,
    Label LbNamePlayerFour,
    Label LbNamePlayerFive,
    Label LbBetPlayerOne,
    Label LbBetPlayerTwo,
    Label LbBetPlayerThree,
    Label LbBetPlayerFour,
    Label LbBetPlayerFive,
    Label LbBetHero,
    Label LbPot,
    PictureBox PbButtonPlayerOne,
    PictureBox PbButtonPlayerTwo,
    PictureBox PbButtonPlayerThree,
    PictureBox PbButtonPlayerFour,
    PictureBox PbButtonPlayerFive,
    PictureBox PbButtonHero,
    Label LbAction,
    Label LbPositionAction,
    Label LbTableHand,
    Label LbTableName
);

public interface IUiUpdateService
{
    void Attach(UiUpdateControls controls);
    void Detach();
    void SetCardList(List<Card> cards);
    void SetOverlay(IFrmOverlay overlay);
    void UpdateResults(PlayerGameState state, PokerCalculationResult result,
        ResponseAction responseAction, string tableHand, string tableName);
}
