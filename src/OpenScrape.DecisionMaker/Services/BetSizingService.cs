using Microsoft.Extensions.Options;

using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;

namespace OpenScrape.DecisionMaker.Services
{
    public class BetSizingService
    {
        private readonly StrategyProfile _profile;

        // Factor de street: flop más pequeño (inducir/proteger), river más grande (extraer valor)
        private const double FlopStreetFactor = 0.90;
        private const double TurnStreetFactor = 1.0;
        private const double RiverStreetFactor = 1.10;

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
            bool isInPosition,
            BoardPosition street = BoardPosition.None)
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

            // Ajustar por número de oponentes (multiway → sizing más pequeño para proteger)
            if (numOpponents >= 3)
            {
                adjustedSize *= _profile.BetSizingMultiOpponentMultiplier;
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

            // Ajustar por street: flop más pequeño, river más grande
            adjustedSize *= street switch
            {
                BoardPosition.Flop => FlopStreetFactor,
                BoardPosition.Turn => TurnStreetFactor,
                BoardPosition.River => RiverStreetFactor,
                _ => 1.0
            };

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
