using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication.UseCases
{
    public class BaseRequest
    {
        public string Hand { get; set; } = default!;
        public TablePosition Position { get; set; }
    }
}
