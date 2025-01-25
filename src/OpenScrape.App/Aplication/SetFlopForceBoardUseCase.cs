using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication;

public class SetFlopForceBoardUseCase : ISetFlopForceBoardUseCase
{
    public SetFlopForceBoardUseCaseResponse Execute(SetFlopForceBoardUseCaseRequest request)
    {
        var cartasMismoPalo = request.TableScrapeResult.DataBoard
            .GroupBy(g => g.Suit)
            .Select(grupo => new { Suit = grupo.Key, Count = grupo.Count() })
            .ToList();

        var cartasIguales = request.TableScrapeResult.DataBoard
            .GroupBy(g => g.Force)
            .Select(grupo => new { Force = grupo.Key, Count = grupo.Count() })
            .ToList();

        var maxCardForce = request.TableScrapeResult.DataBoard.Max(m => m.Force);
        var middleCardForce = request.TableScrapeResult.DataBoard.OrderBy(o => o.Force).ElementAt(1).Force;
        var bottomCardForce = request.TableScrapeResult.DataBoard.Min(m => m.Force);
        var maxHandCardForce = Math.Max(request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);

        request.TableScrapeFlopResult.HighCardInFlop = request.TableScrapeResult.DataBoard
            .Any(a => a.Force == 13 || a.Force == 14);

        request.TableScrapeFlopResult.HavePairOnHand = request.TableScrapeResult.U0CardForce0 == request.TableScrapeResult.U0CardForce1;
        request.TableScrapeFlopResult.HaveHandSuited = request.TableScrapeResult.U0CardSuit0 == request.TableScrapeResult.U0CardSuit1;
        request.TableScrapeFlopResult.HaveHandConnected = Math.Abs(request.TableScrapeResult.U0CardForce0 - request.TableScrapeResult.U0CardForce1) == 1;
        request.TableScrapeFlopResult.HasAce = request.TableScrapeResult.U0CardForce0 == 14 || request.TableScrapeResult.U0CardForce1 == 14;
        request.TableScrapeFlopResult.HasKing = request.TableScrapeResult.U0CardForce0 == 13 || request.TableScrapeResult.U0CardForce1 == 13;
        request.TableScrapeFlopResult.GetHighestRank = Math.Max(request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);
        request.TableScrapeFlopResult.GetLowestRank = Math.Min(request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);
        request.TableScrapeFlopResult.HasOverCards = Math.Max(request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1) > maxCardForce;
        request.TableScrapeFlopResult.IsRainbow = cartasMismoPalo.Count == 3;

        var ranks = request.TableScrapeResult.DataBoard.Select(s => s.Force).OrderBy(r => r).ToList();
        request.TableScrapeFlopResult.IsFlopConnected = (ranks[1] - ranks[0] <= 2) || (ranks[2] - ranks[1] <= 2);

        request.TableScrapeFlopResult.IsFlopPaired = cartasIguales.Any(a => a.Count == 2);
        request.TableScrapeFlopResult.IsDryBoard = request.TableScrapeFlopResult.IsRainbow && !request.TableScrapeFlopResult.IsFlopConnected && !request.TableScrapeFlopResult.IsFlopPaired;

        request.TableScrapeFlopResult.NoOverCardsOnFlop = !request.TableScrapeResult.DataBoard.All(a => a.Force > maxHandCardForce);
        request.TableScrapeFlopResult.HasFlushDraw = HasflushDraw(request.TableScrapeResult.DataBoard, request.TableScrapeResult.U0CardSuit0, request.TableScrapeResult.U0CardSuit1);
        request.TableScrapeFlopResult.HasStraightDraw = HasStraightDraw(request.TableScrapeResult.DataBoard, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);
        request.TableScrapeFlopResult.HasDrawingHand = request.TableScrapeFlopResult.HasFlushDraw || request.TableScrapeFlopResult.HasStraightDraw;

        //HasShowdownValue, HasPair


        request.TableScrapeFlopResult.FlushDrawInFlop = request.TableScrapeResult.DataBoard
            .GroupBy(g => g.Suit)
            .Any(a => a.Count() == 1);

        var handSameSuit = request.TableScrapeResult.U0CardSuit0 == request.TableScrapeResult.U0CardSuit1;

        if (request.TableScrapeFlopResult.HavePairOnHand)
        {
            request.TableScrapeFlopResult.HaveOverPairOnFlop = request.TableScrapeResult.DataBoard
                .Any(a => a.Force > request.TableScrapeResult.U0CardForce0);
        }

        if (!request.TableScrapeFlopResult.HavePairOnHand)
        {
            request.TableScrapeFlopResult.HaveTwoPairOnFlop = request.TableScrapeResult.DataBoard
                .Any(a => a.Force == request.TableScrapeResult.U0CardForce0 && a.Force == request.TableScrapeResult.U0CardForce1);
        }

        if (handSameSuit)
        {
            request.TableScrapeFlopResult.HaveBackdoorFlushDraw = cartasMismoPalo.Count <= 2;
        }

        if (!request.TableScrapeFlopResult.HavePairOnHand)
        {
            if (request.TableScrapeResult.U0CardForce0 == maxCardForce || request.TableScrapeResult.U0CardForce1 == maxCardForce)
            {
                request.TableScrapeFlopResult.HaveTopPairOnFlop = true;
            }

            if (request.TableScrapeResult.U0CardForce0 == middleCardForce || request.TableScrapeResult.U0CardForce1 == middleCardForce)
            {
                request.TableScrapeFlopResult.HaveMiddlePairOnFlop = true;
            }

            if (request.TableScrapeResult.U0CardForce0 == bottomCardForce || request.TableScrapeResult.U0CardForce1 == bottomCardForce)
            {
                request.TableScrapeFlopResult.HaveBottomPairOnFlop = true;
            }
        }

        if (request.TableScrapeResult.U0CardForce0 > maxCardForce && request.TableScrapeResult.U0CardForce1 > maxCardForce)
        {
            request.TableScrapeFlopResult.HaveHighCardsOnHand = true;
        }

        switch (cartasIguales.Count)
        {
            case 1:
                if (request.TableScrapeFlopResult.HavePairOnHand)
                {
                    request.TableScrapeFlopResult.Hand = HeroHand.Full;
                }
                else
                {
                    if (request.TableScrapeResult.DataBoard
                        .Any(a => a.Force == request.TableScrapeResult.U0CardForce0 || a.Force == request.TableScrapeResult.U0CardForce1))
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Poker;
                    }
                    else
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Trio;
                    }
                }

                request.TableScrapeFlopResult.FlopIsCoordinate = true;
                break;
            case 2:
                var forcePairFlop = cartasIguales.First(f => f.Count == 2).Force;
                var forceCardFlop = cartasIguales.First(f => f.Count == 1).Force;

                var hayProyectoEscalera = ProyectoEscalera(request.TableScrapeResult.DataBoard[0].Force, request.TableScrapeResult.DataBoard[1].Force, request.TableScrapeResult.DataBoard[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);

                if (request.TableScrapeFlopResult.HavePairOnHand)
                {
                    request.TableScrapeFlopResult.Hand = HeroHand.DoblePareja;

                    if (forceCardFlop == request.TableScrapeResult.U0CardForce0)
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Full;
                    }

                    if (forcePairFlop == request.TableScrapeResult.U0CardForce0)
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Poker;
                    }

                    request.TableScrapeFlopResult.FlopIsCoordinate = true;
                }
                else
                {
                    request.TableScrapeFlopResult.Hand = HeroHand.Pareja;

                    if (forcePairFlop == request.TableScrapeResult.U0CardForce0 || forcePairFlop == request.TableScrapeResult.U0CardForce1)
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Trio;
                    }

                    if (hayProyectoEscalera)
                    {
                        if (request.TableScrapeFlopResult.Hand == HeroHand.Nada)
                        {
                            request.TableScrapeFlopResult.Hand = HeroHand.ProyectoEscalera;
                        }
                    }

                    if (cartasMismoPalo.Count == 2)
                    {
                        var suitCartasMismoPalo = cartasMismoPalo.First(f => f.Count == 2).Suit;

                        if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo && request.TableScrapeResult.U0CardSuit1 == suitCartasMismoPalo)
                        {
                            if (request.TableScrapeFlopResult.Hand == HeroHand.Nada || request.TableScrapeFlopResult.Hand == HeroHand.ProyectoEscalera)
                            {
                                request.TableScrapeFlopResult.Hand = HeroHand.ProyectoColor;
                            }
                        }
                    }
                }

                request.TableScrapeFlopResult.FlopIsCoordinate = true;

                break;
            case 3:
                if (request.TableScrapeFlopResult.HavePairOnHand)
                {
                    hayProyectoEscalera = ProyectoEscalera(request.TableScrapeResult.DataBoard[0].Force, request.TableScrapeResult.DataBoard[1].Force, request.TableScrapeResult.DataBoard[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);

                    request.TableScrapeFlopResult.Hand = HeroHand.Pareja;

                    if (cartasIguales.Any(a => a.Force == request.TableScrapeResult.U0CardForce0 && a.Force == request.TableScrapeResult.U0CardForce1))
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Trio;
                    }

                    if (hayProyectoEscalera)
                    {
                        if (request.TableScrapeFlopResult.Hand == HeroHand.Nada)
                        {
                            request.TableScrapeFlopResult.Hand = HeroHand.ProyectoEscalera;
                        }

                        request.TableScrapeFlopResult.FlopIsCoordinate = true;
                    }

                    if (cartasMismoPalo.Count == 1)
                    {
                        var suitCartasMismoPalo = cartasMismoPalo.First().Suit;

                        if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo)
                        {
                            if (request.TableScrapeFlopResult.Hand == HeroHand.Nada || request.TableScrapeFlopResult.Hand == HeroHand.ProyectoEscalera)
                            {
                                request.TableScrapeFlopResult.Hand = HeroHand.ProyectoColor;
                            }
                        }

                        request.TableScrapeFlopResult.FlopIsCoordinate = true;
                    }

                    if (request.TableScrapeFlopResult.Hand == HeroHand.ProyectoColor && hayProyectoEscalera)
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.ProyectoEscaleraColor;
                        request.TableScrapeFlopResult.FlopIsCoordinate = true;
                    }
                }
                else
                {
                    if ((cartasIguales.Any(a => a.Force == request.TableScrapeResult.U0CardForce0) && !cartasIguales.Any(a => a.Force == request.TableScrapeResult.U0CardForce1)) ||
                        (!cartasIguales.Any(a => a.Force == request.TableScrapeResult.U0CardForce0) && cartasIguales.Any(a => a.Force == request.TableScrapeResult.U0CardForce1)))
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Pareja;
                    }
                    else
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.DoblePareja;
                    }


                    var listaEscalera = new List<int> { cartasIguales[0].Force, cartasIguales[1].Force, cartasIguales[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1 };
                    if (ExisteEscalera(listaEscalera))
                    {
                        request.TableScrapeFlopResult.Hand = HeroHand.Escalera;
                    }
                    else if (ProyectoEscalera(request.TableScrapeResult.DataBoard[0].Force, request.TableScrapeResult.DataBoard[1].Force, request.TableScrapeResult.DataBoard[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1))
                    {
                        if (request.TableScrapeFlopResult.Hand == HeroHand.Nada)
                        {
                            request.TableScrapeFlopResult.Hand = HeroHand.ProyectoEscalera;
                        }

                        request.TableScrapeFlopResult.FlopIsCoordinate = true;
                    }

                    if (cartasMismoPalo.Count == 1)
                    {
                        var suitCartasMismoPalo = cartasMismoPalo.First().Suit;

                        if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo && request.TableScrapeResult.U0CardSuit1 == suitCartasMismoPalo)
                        {
                            request.TableScrapeFlopResult.Hand = HeroHand.Color;
                        }

                        request.TableScrapeFlopResult.FlopIsCoordinate = true;
                    }
                    else if (cartasMismoPalo.Count == 2)
                    {
                        var suitCartasMismoPalo = cartasMismoPalo.First(f => f.Count == 2).Suit;

                        if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo && request.TableScrapeResult.U0CardSuit1 == suitCartasMismoPalo)
                        {
                            request.TableScrapeFlopResult.Hand = HeroHand.ProyectoColor;
                        }
                    }
                }

                request.TableScrapeFlopResult.FlopIsCoordinate = true;

                break;
        }

        return new SetFlopForceBoardUseCaseResponse { TableScrapeResult = request.TableScrapeResult, TableScrapeFlopResult = request.TableScrapeFlopResult };
    }

    private bool ProyectoEscalera(int card1, int card2, int card3, int card4, int card5)
    {
        var listaForceCards = new List<int> { card1, card2, card3, card4, card5 };

        listaForceCards.Sort();

        int[] diferencias = new int[listaForceCards.ToArray().Length - 1];
        for (int i = 0; i < diferencias.Length; i++)
        {
            diferencias[i] = listaForceCards[i + 1] - listaForceCards[i];
        }

        if (!diferencias.Any(d => d > 2))
            return true;

        return false;
    }

    private bool ExisteEscalera(List<int> cards)
    {
        cards.Sort();

        int[] diferencias = new int[cards.ToArray().Length - 1];
        for (int i = 0; i < diferencias.Length; i++)
        {
            diferencias[i] = cards[i + 1] - cards[i];
        }

        // Verificar si todas las diferencias son iguales a 1 (correlativos)
        if (diferencias.All(d => d == 1))
            return true;

        return false;
    }

    private bool HasflushDraw(List<BoardData> boardData, int cardSuit0, int cardSuit1)
    {
        var allCards = new List<int> { cardSuit0, cardSuit1 };
        allCards.AddRange(boardData.Select(s => s.Suit));

        return allCards
            .GroupBy(g => g)
            .Any(a => a.Count() >= 4);
    }

    private bool HasStraightDraw(List<BoardData> boardData, int cardForce0, int cardForce1)
    {
        var allCards = new List<int> { cardForce0, cardForce1 };
        allCards.AddRange(boardData.Select(s => s.Force));

        var distinctRanks = allCards.Select(s => s).Distinct().OrderBy(o => o).ToList();

        for (int i = 0; i < distinctRanks.Count - 3; i++)
        {
            if (distinctRanks[i + 3] - distinctRanks[i] <= 4)
                return true;
        }

        return false;
    }
}
