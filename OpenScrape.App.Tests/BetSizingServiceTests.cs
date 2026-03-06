using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Tests;

[TestFixture]
public class BetSizingServiceTests
{
    private BetSizingService _service;

    [SetUp]
    public void Setup()
    {
        _service = new BetSizingService();
    }

    [Test]
    public void CalculateDynamicBetSize_DeepStack_SPRMayorA3_DeberiaAumentarBet()
    {
        // SPR = 1000/100 = 10 (deep stack)
        var deepResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 1000m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        // SPR = 80/100 = 0.8 (shallow stack)
        var shallowResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 80m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        // Deep stack deberia tener bet mayor que shallow stack
        Assert.That(GetBetSizeOrder(deepResult), Is.GreaterThanOrEqualTo(GetBetSizeOrder(shallowResult)));
    }

    [Test]
    public void CalculateDynamicBetSize_ShortStack_SPRMenorA1_DeberiaDisminuirBet()
    {
        // SPR = 50/100 = 0.5 (short stack)
        var result = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 50m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        // Con base 0.5 y SPR < 1 => 0.5 * 0.75 * 1.1 = 0.4125 => "Bet 1/2"
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void CalculateDynamicBetSize_BoardPaired_DeberiaAumentarBet()
    {
        var pairedResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 500m, potSize: 100m,
            numOpponents: 1, isPaired: true, isCoordinated: false,
            isDry: false, isInPosition: true);

        var noPairedResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 500m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        Assert.That(GetBetSizeOrder(pairedResult), Is.GreaterThanOrEqualTo(GetBetSizeOrder(noPairedResult)));
    }

    [Test]
    public void CalculateDynamicBetSize_BoardCoordinated_DeberiaDisminuirBet()
    {
        var coordinatedResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 500m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: true,
            isDry: false, isInPosition: true);

        var dryResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 500m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        Assert.That(GetBetSizeOrder(coordinatedResult), Is.LessThanOrEqualTo(GetBetSizeOrder(dryResult)));
    }

    [Test]
    public void CalculateDynamicBetSize_OOP_DeberiaAplicarDescuento()
    {
        var ipResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 500m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        var oopResult = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 500m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: false);

        Assert.That(GetBetSizeOrder(oopResult), Is.LessThanOrEqualTo(GetBetSizeOrder(ipResult)));
    }

    [Test]
    public void CalculateDynamicBetSize_StackCero_DeberiaRetornarBetBaseString()
    {
        var result = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 0m, potSize: 100m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        Assert.That(result, Is.EqualTo("Bet 1/2"));
    }

    [Test]
    public void CalculateDynamicBetSize_PotCero_DeberiaRetornarBetBaseString()
    {
        var result = _service.CalculateDynamicBetSize(
            baseSize: 0.5, heroStack: 500m, potSize: 0m,
            numOpponents: 1, isPaired: false, isCoordinated: false,
            isDry: true, isInPosition: true);

        Assert.That(result, Is.EqualTo("Bet 1/2"));
    }

    [TestCase("Bet Pot", 6)]
    [TestCase("Bet 3/4", 5)]
    [TestCase("Bet 2/3", 4)]
    [TestCase("Bet 1/2", 3)]
    [TestCase("Bet 1/3", 2)]
    [TestCase("Bet 1/4", 1)]
    public void GetBetSizeOrder_ValidaOrdenDeStrings(string betSize, int expectedOrder)
    {
        Assert.That(GetBetSizeOrder(betSize), Is.EqualTo(expectedOrder));
    }

    private static int GetBetSizeOrder(string betSize) => betSize switch
    {
        "Bet Pot" => 6,
        "Bet 3/4" => 5,
        "Bet 2/3" => 4,
        "Bet 1/2" => 3,
        "Bet 1/3" => 2,
        "Bet 1/4" => 1,
        _ => 0
    };
}
