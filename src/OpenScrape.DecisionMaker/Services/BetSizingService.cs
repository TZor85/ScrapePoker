using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace OpenScrape.DecisionMaker.Services
{
    public enum BetSizingType
    {
        Value,
        ThinValue,
        Bluff,
        Overbet
    }

    public record BetSizingOption(double Size, string Label, BetSizingType Type);

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

        /// <summary>
        /// Obtiene opciones de bet sizes para value bets (manos fuertes).
        /// </summary>
        public List<BetSizingOption> GetValueBetSizes(
            double equity,
            double spr,
            BoardTextureCategory texture,
            bool isInPosition,
            bool isMultiway)
        {
            var options = new List<BetSizingOption>();

            if (equity > 80)
            {
                options.Add(new BetSizingOption(0.75, "Overbet", BetSizingType.Overbet));
                options.Add(new BetSizingOption(1.0, "Pot", BetSizingType.Value));
            }

            if (equity > 65)
            {
                options.Add(new BetSizingOption(0.75, "3/4 Pot", BetSizingType.Value));
                options.Add(new BetSizingOption(1.0, "Pot", BetSizingType.Value));
            }
            else
            {
                options.Add(new BetSizingOption(0.5, "1/2 Pot", BetSizingType.ThinValue));
                options.Add(new BetSizingOption(0.66, "2/3 Pot", BetSizingType.Value));
            }

            if (isMultiway)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    options[i] = new BetSizingOption(options[i].Size * 0.8, options[i].Label, options[i].Type);
                }
            }

            if (!isInPosition)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    options[i] = new BetSizingOption(options[i].Size * 1.1, options[i].Label, options[i].Type);
                }
            }

            return options;
        }

        /// <summary>
        /// Obtiene opciones de bet sizes para bluffs.
        /// </summary>
        public List<BetSizingOption> GetBluffSizes(
            double foldEquity,
            double spr,
            BoardTextureCategory texture,
            bool isInPosition)
        {
            var options = new List<BetSizingOption>();

            bool isDry = texture == BoardTextureCategory.Dry || texture == BoardTextureCategory.Paired;
            bool isWet = texture == BoardTextureCategory.Wet || texture == BoardTextureCategory.SemiWet;

            if (isDry && foldEquity > 50)
            {
                options.Add(new BetSizingOption(0.66, "2/3 Pot", BetSizingType.Bluff));
                options.Add(new BetSizingOption(0.75, "3/4 Pot", BetSizingType.Bluff));
            }
            else if (isWet)
            {
                options.Add(new BetSizingOption(0.33, "1/3 Pot", BetSizingType.Bluff));
                options.Add(new BetSizingOption(0.5, "1/2 Pot", BetSizingType.Bluff));
            }
            else
            {
                options.Add(new BetSizingOption(0.5, "1/2 Pot", BetSizingType.Bluff));
            }

            if (!isInPosition)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    options[i] = new BetSizingOption(options[i].Size * 1.2, options[i].Label, options[i].Type);
                }
            }

            return options;
        }

        /// <summary>
        /// Obtiene el threshold de thin value según la situación.
        /// </summary>
        public double GetThinValueThreshold(BoardTextureCategory texture, double spr, bool isInPosition)
        {
            double baseThreshold = 55;

            if (texture == BoardTextureCategory.Dry || texture == BoardTextureCategory.Paired)
                baseThreshold -= 3;
            else if (texture == BoardTextureCategory.Wet)
                baseThreshold += 3;

            if (!isInPosition)
                baseThreshold += 3;

            if (spr < 3)
                baseThreshold += 5;
            else if (spr > 10)
                baseThreshold -= 2;

            return baseThreshold;
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
