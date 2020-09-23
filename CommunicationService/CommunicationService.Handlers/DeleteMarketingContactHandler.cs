using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class DeleteMarketingContactHandler : IRequestHandler<DeleteMarketingContactRequest, bool>
    {
        private readonly IConnectSendGridService _connectSendGridService;

        public DeleteMarketingContactHandler(IConnectSendGridService connectSendGridService)
        {
            _connectSendGridService = connectSendGridService;
        }

        public async Task<bool> Handle(DeleteMarketingContactRequest request, CancellationToken cancellationToken)
        {
            return await _connectSendGridService.DeleteMarketingContact(request.MarketingContact);
        }
    }
}
