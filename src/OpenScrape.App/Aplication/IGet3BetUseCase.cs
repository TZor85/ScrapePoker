using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Aplication
{
    public class Get3BetUseCaseRequest
    {

    }

    public class Get3BetUseCaseResponse
    {

    }

    public interface IGet3BetUseCase
    {
        Get3BetUseCaseResponse Execute(Get3BetUseCaseRequest request);
    }
}
