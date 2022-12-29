using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Models
{
    //public class Regions
    //{
    //    public string Type { get; set; } = string.Empty;
    //    public RectangleRegion Region { get; set; } = new RectangleRegion();
    //    public ImageRegion Image { get; set; } = new ImageRegion();
    //    public HashRegion Hash { get; set; } = new HashRegion();
    //}

    public class Regions
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int Width { get; set; } = 20;
        public int Height { get; set; } = 20;
        public string Value { get; set; } = string.Empty;
        public bool IsHash { get; set; }
        public bool IsColor { get; set; }
        public bool IsBoard { get; set; }
        public string Color { get; set; } = string.Empty;

    }

    public class ImageRegion
    {
        public string Name { get; set; } = string.Empty;
        public Bitmap? Image { get; set; }
        public bool IsBoard { get; set; }
        public string Value { get; set; } = string.Empty;
        public int Force { get; set; } = 0;
        public int Suit { get; set; } = 0;
        //public HashRegion Hash { get; set; } = new HashRegion();
    }

    public class HashRegion
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class FontRegion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 6);
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

}
