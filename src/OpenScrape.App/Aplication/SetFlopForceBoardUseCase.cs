using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication
{
    public class SetFlopForceBoardUseCase : ISetFlopForceBoardUseCase
    {
        public SetFlopForceBoardUseCaseResponse Execute(SetFlopForceBoardUseCaseRequest request)
        {
            request.TableScrapeResult.HighCardInFlop = request.TableScrapeResult.DataBoard.Any(a => a.Force == 13 || a.Force == 14);
            request.TableScrapeResult.HavePairOnHand = request.TableScrapeResult.U0CardForce0 == request.TableScrapeResult.U0CardForce1;

            //Si cartasMismoPalo.Count() = 1 -> Flop mismo palo
            //Si cartasMismoPalo.Count() = 2 -> Flop con dos cartas del mismo palo
            //Si cartasMismoPalo.Count() = 3 -> Flop multicolor
            var cartasMismoPalo = request.TableScrapeResult.DataBoard.GroupBy(g => g.Suit)
                                     .Select(grupo => new { Valor = grupo.Key, Cantidad = grupo.Count() }).ToList();

            //Si cartasIguales.Count() = 1 -> Trio en Flop
            //Si cartasIguales.Count() = 2 -> Pareja en Flop
            //Si cartasIguales.Count() = 3 -> No hay pareja en Flop
            var cartasIguales = request.TableScrapeResult.DataBoard.GroupBy(g => g.Force)
                                     .Select(grupo => new { Valor = grupo.Key, Cantidad = grupo.Count() }).ToList();

            switch (cartasIguales.Count())
            {
                case 1:
                    if (request.TableScrapeResult.HavePairOnHand)
                        request.TableScrapeResult.Hand = HeroHand.Full;
                    else
                    {
                        if (request.TableScrapeResult.DataBoard.Any(a => a.Force == request.TableScrapeResult.U0CardForce0 || a.Force == request.TableScrapeResult.U0CardForce1))
                            request.TableScrapeResult.Hand = HeroHand.Poker;
                        else
                            request.TableScrapeResult.Hand = HeroHand.Trio;
                    }

                    request.TableScrapeResult.FlopIsCoordinate = true;
                    break;
                case 2:
                    var forcePairFlop = cartasIguales.First(f => f.Cantidad == 2).Valor;
                    var forceCardFlop = cartasIguales.First(f => f.Cantidad == 1).Valor;

                    var hayProyectoEscalera = ProyectoEscalera(cartasIguales[0].Valor, cartasIguales[1].Valor, cartasIguales[2].Valor, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);

                    if (request.TableScrapeResult.HavePairOnHand)
                    {
                        request.TableScrapeResult.Hand = HeroHand.DoblePareja;

                        if (forceCardFlop == request.TableScrapeResult.U0CardForce0)
                            request.TableScrapeResult.Hand = HeroHand.Full;

                        if (forcePairFlop == request.TableScrapeResult.U0CardForce0)
                            request.TableScrapeResult.Hand = HeroHand.Poker;

                        request.TableScrapeResult.FlopIsCoordinate = true;
                    }
                    else
                    {
                        request.TableScrapeResult.Hand = HeroHand.Pareja;

                        if (forcePairFlop == request.TableScrapeResult.U0CardForce0 || forcePairFlop == request.TableScrapeResult.U0CardForce1)
                            request.TableScrapeResult.Hand = HeroHand.Trio;

                        if (hayProyectoEscalera)
                        {
                            if (request.TableScrapeResult.Hand == HeroHand.Nada)
                                request.TableScrapeResult.Hand = HeroHand.ProyectoEscalera;
                        }

                        if (cartasMismoPalo.Count == 2)
                        {
                            var suitCartasMismoPalo = cartasMismoPalo.First(f => f.Cantidad == 2).Valor;

                            if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo && request.TableScrapeResult.U0CardSuit1 == suitCartasMismoPalo)
                            {
                                if (request.TableScrapeResult.Hand == HeroHand.Nada || request.TableScrapeResult.Hand == HeroHand.ProyectoEscalera)
                                    request.TableScrapeResult.Hand = HeroHand.ProyectoColor;
                            }
                        }
                    }

                    request.TableScrapeResult.FlopIsCoordinate = true;

                    break;
                case 3:

                    if (request.TableScrapeResult.HavePairOnHand)
                    {
                        hayProyectoEscalera = ProyectoEscalera(cartasIguales[0].Valor, cartasIguales[1].Valor, cartasIguales[2].Valor, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1);

                        request.TableScrapeResult.Hand = HeroHand.Pareja;

                        if (cartasIguales.Any(a => a.Valor == request.TableScrapeResult.U0CardForce0 && a.Valor == request.TableScrapeResult.U0CardForce1))
                            request.TableScrapeResult.Hand = HeroHand.Trio;

                        if (hayProyectoEscalera)
                        {
                            if (request.TableScrapeResult.Hand == HeroHand.Nada)
                                request.TableScrapeResult.Hand = HeroHand.ProyectoEscalera;

                            request.TableScrapeResult.FlopIsCoordinate = true;
                        }

                        if (cartasMismoPalo.Count == 1)
                        {
                            var suitCartasMismoPalo = cartasMismoPalo.First().Valor;

                            if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo)
                            {
                                if (request.TableScrapeResult.Hand == HeroHand.Nada || request.TableScrapeResult.Hand == HeroHand.ProyectoEscalera)
                                    request.TableScrapeResult.Hand = HeroHand.ProyectoColor;
                            }

                            request.TableScrapeResult.FlopIsCoordinate = true;
                        }

                        if (request.TableScrapeResult.Hand == HeroHand.ProyectoColor && hayProyectoEscalera)
                        {
                            request.TableScrapeResult.Hand = HeroHand.ProyectoEscaleraColor;
                            request.TableScrapeResult.FlopIsCoordinate = true;
                        }

                    }
                    else
                    {
                        if ((cartasIguales.Any(a => a.Valor == request.TableScrapeResult.U0CardForce0) && !cartasIguales.Any(a => a.Valor == request.TableScrapeResult.U0CardForce1)) ||
                            (!cartasIguales.Any(a => a.Valor == request.TableScrapeResult.U0CardForce0) && cartasIguales.Any(a => a.Valor == request.TableScrapeResult.U0CardForce1)))
                        {
                            request.TableScrapeResult.Hand = HeroHand.Pareja;
                        }
                        else
                            request.TableScrapeResult.Hand = HeroHand.DoblePareja;

                        if (ExisteEscalera(cartasIguales[0].Valor, cartasIguales[1].Valor, cartasIguales[2].Valor, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1))
                            request.TableScrapeResult.Hand = HeroHand.Escalera;
                        else if (ProyectoEscalera(cartasIguales[0].Valor, cartasIguales[1].Valor, cartasIguales[2].Valor, request.TableScrapeResult.U0CardForce0, request.TableScrapeResult.U0CardForce1))
                        {
                            if (request.TableScrapeResult.Hand == HeroHand.Nada)
                                request.TableScrapeResult.Hand = HeroHand.ProyectoEscalera;

                            request.TableScrapeResult.FlopIsCoordinate = true;
                        }

                        if (cartasMismoPalo.Count == 1)
                        {
                            var suitCartasMismoPalo = cartasMismoPalo.First().Valor;

                            if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo && request.TableScrapeResult.U0CardSuit1 == suitCartasMismoPalo)
                                request.TableScrapeResult.Hand = HeroHand.Color;

                            request.TableScrapeResult.FlopIsCoordinate = true;
                        }
                        else if (cartasMismoPalo.Count == 2)
                        {
                            var suitCartasMismoPalo = cartasMismoPalo.First(f => f.Cantidad == 2).Valor;

                            if (request.TableScrapeResult.U0CardSuit0 == suitCartasMismoPalo && request.TableScrapeResult.U0CardSuit1 == suitCartasMismoPalo)
                            {
                                if (request.TableScrapeResult.Hand == HeroHand.Nada || request.TableScrapeResult.Hand == HeroHand.ProyectoEscalera)
                                    request.TableScrapeResult.Hand = HeroHand.ProyectoColor;
                            }

                            request.TableScrapeResult.FlopIsCoordinate = true;
                        }
                    }

                    break;
                default:
                    break;
            }

            return new SetFlopForceBoardUseCaseResponse { TableScrapeResult = request.TableScrapeResult };
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

        private bool ExisteEscalera(int card1, int card2, int card3, int card4, int card5)
        {
            var listaForceCards = new List<int> { card1, card2, card3, card4, card5 };

            listaForceCards.Sort();

            int[] diferencias = new int[listaForceCards.ToArray().Length - 1];
            for (int i = 0; i < diferencias.Length; i++)
            {
                diferencias[i] = listaForceCards[i + 1] - listaForceCards[i];
            }

            // Verificar si todas las diferencias son iguales a 1 (correlativos)
            if (diferencias.All(d => d == 1))
                return true;

            return false;
        }
    }
}
