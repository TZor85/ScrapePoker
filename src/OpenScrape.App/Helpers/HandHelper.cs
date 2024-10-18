namespace OpenScrape.App.Helpers
{
    public static class HandHelper
    {

        public static int GetSuitHand(string texto)
        {
            var suit = texto[1] switch
            {
                'c' => 1,
                'h' => 2,
                'd' => 3,
                's' => 4,
                _ => 0
            };

            return suit;
        }

        public static int GetForceHand(string texto)
        {
            var force = texto[0] switch
            {
                'A' => 14,
                'K' => 13,
                'Q' => 12,
                'J' => 11,
                'T' => 10,
                '9' => 9,
                '8' => 8,
                '7' => 7,
                '6' => 6,
                '5' => 5,
                '4' => 4,
                '3' => 3,
                '2' => 2,
                _ => 0
            };

            return force;
        }

    }
}
