using OpenScrape.App.Models;

namespace OpenScrape.App.Aplication
{
    public class LoadTableMapUseCaseResponse
    {
        public List<Regions> Regions { get; set; } = new List<Regions>();
        public List<ImageRegion> Images { get; set; } = new List<ImageRegion>();
        public List<ImageRegion> Board { get; set; } = new List<ImageRegion>();
        public List<FontRegion> Fonts { get; set; } = new List<FontRegion>();
        public List<KeyValuePair<string, string>> Tree { get; set; } = new List<KeyValuePair<string, string>>();
    }

    internal interface ILoadTableMapUseCase
    {
        LoadTableMapUseCaseResponse Execute(string secret);
    }
}
