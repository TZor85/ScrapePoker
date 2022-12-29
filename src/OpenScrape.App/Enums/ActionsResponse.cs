using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Enums
{
    public class ActionsResponse
    {
        public string Action { get; set; } = default!;
        public List<string> Hands { get; set; } = default!;
        public Styles Style { get; set; } = default!;
        public Positions Position { get; set; } = default!;
        public PreflopAction PreflopAction { get; set; } = default!;
    }
}
