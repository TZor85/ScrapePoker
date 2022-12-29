using OpenScrape.App.Enums;

namespace OpenScrape.App.Entities
{
    public class BoardPlayerData
    {
        public bool Aggressor { get; set; } = false;
        public bool InPosition { get; set; } = false;
        public JugadasEnum Jugada { get; set; } = JugadasEnum.Nothing;
        public PreflopAction PreflopAction { get; set; } = default!;
        public bool BoardCoordinado { get; set; } = false;
        public int KickerValue { get; set; } = 0;

    }
}
