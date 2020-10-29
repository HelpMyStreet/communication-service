using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CommunicationService.Core.Interfaces.Repositories;

namespace CommunicationService.Handlers
{
    public class CreateLinkHandler : IRequestHandler<CreateLinkRequest, CreateLinkResponse>
    {
        private readonly ILinkRepository _linkRepository;

        public CreateLinkHandler(ILinkRepository linkRepository)
        {
            _linkRepository = linkRepository;
        }

        public async Task<CreateLinkResponse> Handle(CreateLinkRequest request, CancellationToken cancellationToken)
        {
            return new CreateLinkResponse()
            {
                Token = await _linkRepository.CreateLink(request.LinkDestination, request.ExpiryDays)
            };
        }
    }
}
