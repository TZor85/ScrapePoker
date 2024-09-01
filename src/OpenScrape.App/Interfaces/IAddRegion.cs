using OpenScrape.App.Models;

namespace OpenScrape.App.Interfaces
{
    public interface IAddRegion
    {
        void Execute(string texto, string nodo);
        void Execute(List<FontRegion> fonts);
    }
}
