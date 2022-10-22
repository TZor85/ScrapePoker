using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.UseCases
{
    public interface IGetActionsUseCase
    {
        GetActionsResponse Execute(GetActionsRequest request);
    }

    public class GetActionsResponse
    {
        public string Data { get; set; } = string.Empty;
    }

    public class GetActionsRequest
    {
        public string Card0 { get; set; } = string.Empty;
        public string Card1 { get; set; } = string.Empty;
        public double EffectiveStack { get; set; }
    }
}
