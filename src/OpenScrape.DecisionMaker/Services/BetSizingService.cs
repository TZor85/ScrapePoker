using Microsoft.Extensions.Options;

using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;
using System;

namespace OpenScrape.DecisionMaker.Services
{
    public class BetSizingService
    {
        private readonly StrategyProfile _profile;

        public BetSizingService(IOptions<StrategyProfile> profileOptions)
        {
            _profile = profileOptions.Value;
        }

        public string CalculateDynamicBetSize(
            double baseSize,
            decimal heroStack,
            decimal potSize,
            int numOpponents,
            bool isPaired,
            bool isCoordinated,
            bool isDry,
            bool isInPosition)
        {
            if (heroStack == 0 || potSize == 0)
                return GetBetSizeString(baseSize);

            double spr = (double)heroStack / (double)potSize;
            double adjustedSize = baseSize;

            // Ajustar por SPR
            if (spr > _profile.BetSizingSPRDeepThreshold)
            {
                adjustedSize *= _profile.BetSizingSPRDeepMultiplier;
            }
            else if (spr < _profile.BetSizingSPRShallowThreshold)
            {
                adjustedSize *= _profile.BetSizingSPRShallowMultiplier;
            }

            // Ajustar por número de oponentes
            if (numOpponents >= 3)
            {
                adjustedSize *= _profile.BetSizingMultiOpponentMultiplier;
            }
            else if (numOpponents == 1)
            {
                adjustedSize *= 1.1;
            }

            // Ajustar por textura de board
            if (isPaired)
            {
                adjustedSize *= _profile.BetSizingPairedMultiplier;
            }
            else if (isCoordinated && !isDry)
            {
                adjustedSize *= _profile.BetSizingCoordinatedMultiplier;
            }

            // Ajustar por posición
            if (!isInPosition)
            {
                adjustedSize *= _profile.BetSizingOOPMultiplier;
            }

            // Limitar tamaño
            adjustedSize = Math.Min(adjustedSize, 1.0);
            adjustedSize = Math.Max(adjustedSize, 0.1);

            return GetBetSizeString(adjustedSize);
        }

        private string GetBetSizeString(double size)
        {
            if (size >= 0.9) return "Bet Pot";
            if (size >= 0.75) return "Bet 3/4";
            if (size >= 0.6) return "Bet 2/3";
            if (size >= 0.4) return "Bet 1/2";
            if (size >= 0.25) return "Bet 1/3";
            return "Bet 1/4";
        }
    }
}
