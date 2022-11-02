using OpenScrape.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication
{
    public class LoadTableMapUseCaseResponse
    {
        public List<Regions> Regions { get; set; } = new List<Regions>();
        public List<ImageRegion> Images { get; set; } = new List<ImageRegion>();
        public List<ImageRegion> Board { get; set; } = new List<ImageRegion>();
        public List<KeyValuePair<string, string>> Tree { get; set; } = new List<KeyValuePair<string, string>>();
    }

    internal interface ILoadTableMapUseCase
    {
        LoadTableMapUseCaseResponse Execute(string secret);
    }
}
