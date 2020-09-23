using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class PutNewMarketingContactHandler : IRequestHandler<PutNewMarketingContactRequest, bool>
    {
        private readonly IConnectSendGridService _connectSendGridService;

        public PutNewMarketingContactHandler(IConnectSendGridService connectSendGridService)
        {
            _connectSendGridService = connectSendGridService;
        }

        public async Task<bool> Handle(PutNewMarketingContactRequest request, CancellationToken cancellationToken)
        {
            return await _connectSendGridService.AddNewMarketingContact(request.MarketingContact);
        }
    }
}
