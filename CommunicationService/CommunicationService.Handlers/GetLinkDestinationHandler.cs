using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Services;

namespace CommunicationService.Handlers
{
    public class GetLinkDestinationHandler : IRequestHandler<GetLinkDestinationRequest, GetLinkDestinationResponse>
    {
        private readonly ILinkRepository _linkRepository;

        public GetLinkDestinationHandler(ILinkRepository linkRepository)
        {
            _linkRepository = linkRepository;
        }

        public async Task<GetLinkDestinationResponse> Handle(GetLinkDestinationRequest request, CancellationToken cancellationToken)
        {
            return new GetLinkDestinationResponse()
            {
                Url = await _linkRepository.GetLinkDestination(request.Token)
            };
        }
    }
}
