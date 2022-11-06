using OpenScrape.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Interfaces
{
    public interface IAddRegion
    {
        void Execute(string texto, string nodo);
        void Execute(List<FontRegion> fonts);
    }
}
