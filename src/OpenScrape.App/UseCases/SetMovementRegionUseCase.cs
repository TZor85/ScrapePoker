using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.UseCases
{
    public class SetMovementRegionUseCase : ISetMovementRegionUseCase
    {
        public SetMovementRegionUseCaseResponse Execute(SetMovementRegionUseCaseRequest request)
        {
            var response = new SetMovementRegionUseCaseResponse();

            if(request.IsX && request.IsY)
            {
                response.CoordX = request.CoordX + request.Speed;
                response.CoordY = request.CoordY + request.Speed;
            }
            else if(request.IsX)
                response.CoordX = request.CoordX + request.Speed;
            else if(request.IsY)
                response.CoordY = request.CoordY + request.Speed;


            return response;
        }
    }
}
