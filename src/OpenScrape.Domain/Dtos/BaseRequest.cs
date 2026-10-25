using OpenScrape.Domain.Enums;

namespace OpenScrape.Domain.Dtos
{
    public class BaseRequest
    {
        public string Hand { get; set; } = default!;
        public TablePosition Position { get; set; }
    }
}
