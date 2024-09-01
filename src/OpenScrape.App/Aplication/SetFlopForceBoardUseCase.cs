using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{
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

            request.TableScrapeFlopResult.HighCardInFlop = request.TableScrapeResult.DataBoard
                .Any(a => a.Force == 13 || a.Force == 14);

            request.TableScrapeFlopResult.HavePairOnHand = request.TableScrapeResult.U0CardForce0 == request.TableScrapeResult.U0CardForce1;

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

                    var hayProyectoEscalera = ProyectoEscalera(cartasIguales[0].Force, cartasIguales[1].Force, cartasIguales[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);

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
                        hayProyectoEscalera = ProyectoEscalera(cartasIguales[0].Force, cartasIguales[1].Force, cartasIguales[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);

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

                        if (ExisteEscalera(cartasIguales[0].Force, cartasIguales[1].Force, cartasIguales[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1))
                        {
                            request.TableScrapeFlopResult.Hand = HeroHand.Escalera;
                        }
                        else if (ProyectoEscalera(cartasIguales[0].Force, cartasIguales[1].Force, cartasIguales[2].Force, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1))
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

            return new SetFlopForceBoardUseCaseResponse();
        }

        private bool ProyectoEscalera(int card1, int card2, int card3, int card4, int card5)
        {
            // Implementación del método ProyectoEscalera
            // ...
        }

        private bool ExisteEscalera(int card1, int card2, int card3, int card4, int card5)
        {
            // Implementación del método ExisteEscalera
            // ...
        }
    }
}
