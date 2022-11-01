using OpenScrape.App.Models;

namespace OpenScrape.App.Aplication
{
    public class SaveTableMapUseCaseRequest
    {
        public string Key { get; set; } = string.Empty;
        public List<Regions> Regions { get; set; } = new List<Regions>();
        public List<ImageRegion> Images { get; set; } = new List<ImageRegion>();
    }

    public interface ISaveTableMapUseCase
    {
        void Execute(SaveTableMapUseCaseRequest request);
    }
}
