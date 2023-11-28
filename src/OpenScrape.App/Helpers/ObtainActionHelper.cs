using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Helpers
{
    public static class ObtainActionHelper
    {
        public static string ObtainAction(List<Hands> hands, string hand)
        {
            Random random = new Random();

            var porcen = new List<int>();
            var numeroAleatorio = random.Next(101);
            int acumulado = 0;
            int sumaTotal = 0;

            foreach (var item in hands)
            {
                if (item.Hand == hand)
                {
                    sumaTotal += item.Porcentajes;
                    porcen.Add(item.Porcentajes);
                }
            }

            foreach (var kvp in porcen)
            {
                acumulado += kvp;
                if (numeroAleatorio <= acumulado)
                {
                    return hands.FirstOrDefault(x => x.Porcentajes == kvp)?.Action ?? "Fold";
                }
            }

            return "Fold";


        }
    }

    public class Hands
    {
        public string Hand { get; set; } = default!;
        public string Action { get; set; } = default!;
        public int Porcentajes { get; set; }
    }
}
