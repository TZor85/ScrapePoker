using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication.UseCases
{
    public class Get3BetUseCase : IGet3BetUseCase
    {
        public Get3BetUseCaseResponse Execute(Get3BetUseCaseRequest request)
        {
            throw new NotImplementedException();
        }

        private static string GetRandomValue(Dictionary<string, double> values)
        {
            double totalPercentage = values.Sum(x => x.Value);
            double randomValue = new Random().NextDouble() * totalPercentage;

            double accumulatedPercentage = 0;
            foreach (var kvp in values)
            {
                accumulatedPercentage += kvp.Value;
                if (randomValue < accumulatedPercentage)
                {
                    return kvp.Key;
                }
            }

            return string.Empty;
        }
    }
}
