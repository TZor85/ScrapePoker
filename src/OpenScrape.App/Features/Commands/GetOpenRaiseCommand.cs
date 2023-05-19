using MediatR;
using OpenScrape.App.Enums;
using OpenScrape.App.Tables;

namespace OpenScrape.App.Features.Commands
{

    public class GetOpenRaiseCommand : IRequest<GetOpenRaiseCommandResponse>
    {
        public string Hand { get; set; } = default!;
        public HeroPosition Position { get; set; }
    }

    public class GetOpenRaiseCommandResponse : BaseResponse { }

    public class GetOpenRaiseCommandHandler : IRequestHandler<GetOpenRaiseCommand, GetOpenRaiseCommandResponse>
    {
        public Task<GetOpenRaiseCommandResponse> Handle(GetOpenRaiseCommand request, CancellationToken cancellationToken)
        {
            var response = new GetOpenRaiseCommandResponse();

            response.Action = request.Position switch
            {
                HeroPosition.SmallBlind => OpenRaises.GetSmallBlindAction(request.Hand),
                HeroPosition.Button => OpenRaises.GetButtonAction(request.Hand),
                HeroPosition.CutOff => OpenRaises.GetCutOffAction(request.Hand),
                HeroPosition.MiddlePosition => OpenRaises.GetMiddleAction(request.Hand),
                HeroPosition.EarlyPosition => OpenRaises.GetEarlyAction(request.Hand),
                _ => "Fold"
            };

            return Task.FromResult(response);
        }
    }
}
