using OpenScrape.App.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication.UseCases
{
    public class BaseRequest
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
    }
}
