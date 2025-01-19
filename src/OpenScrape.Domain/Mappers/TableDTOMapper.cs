using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.Domain.Mappers
{
    public static class TableDTOMapper
    {
        public static TableDTO ToDto(this Entities.Table action)
        {
            var actionDto = new TableDTO
            {
                Name = action.Id,
                Positions = new List<PlayerActionSequence>()
            };

            if (action.Positions != null)
            {
                foreach (var item in action.Positions)
                {
                    actionDto.Positions.Add(item);
                }
            }

            return actionDto;
        }
        public static Entities.Table ToEntity(this TableDTO action)
        {
            var actionEntity = new Entities.Table
            {
                Id = action.Name,
                Positions = new List<PlayerActionSequence>()
            };
            if (action.Positions != null)
            {
                foreach (var item in action.Positions)
                {
                    actionEntity.Positions.Add(item);
                }
            }
            return actionEntity;
        }
    }
}
