using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class SendEmailToUsersHandler : IRequestHandler<SendEmailToUsersRequest, SendEmailResponse>
    {
        private readonly ISendEmailService _sendEmailService;

        public SendEmailToUsersHandler(ISendEmailService sendEmailService)
        {
            _sendEmailService = sendEmailService;
        }

        public async Task<SendEmailResponse> Handle(SendEmailToUsersRequest request, CancellationToken cancellationToken)
        {
            bool response = await _sendEmailService.SendEmailToUsers(request);
            return new SendEmailResponse()
            {
                Success = response
            };
        }
    }
}
