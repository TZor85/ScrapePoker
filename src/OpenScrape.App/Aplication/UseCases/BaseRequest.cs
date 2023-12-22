using OpenScrape.App.Enums;

namespace OpenScrape.App.Aplication.UseCases
{
    public class BaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
    }
}
