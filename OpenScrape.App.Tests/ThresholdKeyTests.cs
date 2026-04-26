using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class ThresholdKeyTests
{
    [Test]
    public void Constructor_CombinacionPostflopValida_CreaInstancia()
    {
        var key = new ThresholdKey(BoardPosition.Turn, HandSituation.OpenRaise);

        Assert.That(key.Street, Is.EqualTo(BoardPosition.Turn));
        Assert.That(key.Situation, Is.EqualTo(HandSituation.OpenRaise));
    }

    [Test]
    public void Constructor_StreetNone_LanzaArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new ThresholdKey(BoardPosition.None, HandSituation.OpenRaise));

        Assert.That(ex!.ParamName, Is.EqualTo("street"));
        Assert.That(ex.Message, Does.Contain("None"));
    }

    [Test]
    public void Constructor_StreetHand_LanzaArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new ThresholdKey(BoardPosition.Hand, HandSituation.OpenRaise));

        Assert.That(ex!.ParamName, Is.EqualTo("street"));
        Assert.That(ex.Message, Does.Contain("Hand"));
    }

    [Test]
    public void Constructor_SituationNone_LanzaArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new ThresholdKey(BoardPosition.Flop, HandSituation.None));

        Assert.That(ex!.ParamName, Is.EqualTo("situation"));
        Assert.That(ex.Message, Does.Contain("None"));
    }

    [Test]
    public void Constructor_SituationCall_EsValido()
    {
        var key = new ThresholdKey(BoardPosition.Flop, HandSituation.Call);

        Assert.That(key.Street, Is.EqualTo(BoardPosition.Flop));
        Assert.That(key.Situation, Is.EqualTo(HandSituation.Call));
        Assert.That(key.ToString(), Is.EqualTo("Flop_Call"));
    }

    [Test]
    public void Equality_MismasCombinaciones_SonIguales()
    {
        var a = new ThresholdKey(BoardPosition.River, HandSituation.Squeeze);
        var b = new ThresholdKey(BoardPosition.River, HandSituation.Squeeze);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void Equality_CombinacionesDistintas_NoSonIguales()
    {
        var a = new ThresholdKey(BoardPosition.Turn, HandSituation.OpenRaise);
        var b = new ThresholdKey(BoardPosition.Turn, HandSituation.ThreeBet);

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void ToString_DevuelveFormatoStreetGuionSituation()
    {
        var key = new ThresholdKey(BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall);

        Assert.That(key.ToString(), Is.EqualTo("Turn_OpenRaiseVs3BetAndCall"));
    }
}
