using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication
{
    public class SetMovementRegionUseCaseRequest
    {
        public bool IsX { get; set; }
        public bool IsY { get; set; }
        public int Speed { get; set; }
        public int CoordX { get; set; }
        public int CoordY { get; set; }
    }

    public class SetMovementRegionUseCaseResponse
    {
        public int CoordX { get; set; }
        public int CoordY { get; set; }
    }

    public interface ISetMovementRegionUseCase
    {
        SetMovementRegionUseCaseResponse Execute(SetMovementRegionUseCaseRequest request);
    }
}
