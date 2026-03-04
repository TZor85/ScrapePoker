using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;
using System;

namespace OpenScrape.DecisionMaker.Services
{
    public class BetSizingService
    {
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

            // Adjust based on SPR
            if (spr > 3.0)
            {
                adjustedSize *= 1.25; // Increase 25% for deep stacks
            }
            else if (spr < 1.0)
            {
                adjustedSize *= 0.75; // Decrease 25% for shallow stacks
            }

            // Adjust based on number of opponents
            if (numOpponents >= 3)
            {
                adjustedSize *= 0.85; // Smaller bets with more opponents
            }
            else if (numOpponents == 1)
            {
                adjustedSize *= 1.1; // Slightly larger vs single opponent
            }

            // Adjust based on board texture
            if (isPaired)
            {
                adjustedSize *= 1.15; // Larger bets on paired boards
            }
            else if (isCoordinated && !isDry)
            {
                adjustedSize *= 0.9; // Smaller bets on wet boards
            }

            // Position adjustment
            if (!isInPosition)
            {
                adjustedSize *= 0.9; // Smaller bets OOP
            }

            // Cap the size
            adjustedSize = Math.Min(adjustedSize, 1.0); // Don't exceed pot
            adjustedSize = Math.Max(adjustedSize, 0.1); // Minimum 10%

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