using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OpenScrape.App.Helpers
{
    public class GetRGBColorResponse
    {
        public string RColor { get; set; } = string.Empty;
        public string GColor { get; set; } = string.Empty;
        public string BColor { get; set; } = string.Empty;
    }

    public class GetRGBColorRequest
    {
        public Bitmap Image { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public static class ColorHelper
    {
        public static GetRGBColorResponse GetRGBColor(GetRGBColorRequest request)
        {
            var response = new GetRGBColorResponse();

            Color color = request.Image.GetPixel(request.X, request.Y);
            var rgbColor = color.Name.Substring(2, 6);

            response.RColor = rgbColor.Substring(0, 2);
            response.GColor = rgbColor.Substring(2, 2);
            response.BColor = rgbColor.Substring(4, 2);

            return response;

        }

    }
}
