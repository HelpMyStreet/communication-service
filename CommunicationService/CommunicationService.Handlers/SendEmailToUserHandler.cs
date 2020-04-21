using CommunicationService.Core.Domains.Entities;
using CommunicationService.Core.Interfaces.Services;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class SendEmailToUserHandler : IRequestHandler<SendEmailToUserRequest,SendEmailResponse>
    {
        private readonly ISendEmailService _sendEmailService;        

        public SendEmailToUserHandler(ISendEmailService sendEmailService)
        {
            _sendEmailService = sendEmailService;
        }

        public async Task<SendEmailResponse> Handle(SendEmailToUserRequest request, CancellationToken cancellationToken)
        {
            bool response = await _sendEmailService.SendEmailToUser(request);
            return new SendEmailResponse()
            {
                Success = response
            };
        }
    }
}
