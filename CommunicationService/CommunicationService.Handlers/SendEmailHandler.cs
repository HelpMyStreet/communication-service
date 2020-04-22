using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class SendEmailHandler : IRequestHandler<SendEmailRequest,SendEmailResponse>
    {
        private readonly IRepository _repository;        
        private readonly ISendEmailService _sendEmailService;

        public SendEmailHandler(IRepository repository, ISendEmailService sendEmailService)
        {
            _repository = repository;            
            _sendEmailService = sendEmailService;            
        }

        public async Task<SendEmailResponse> Handle(SendEmailRequest request, CancellationToken cancellationToken)
        {
            bool response = await _sendEmailService.SendEmail(request);
            return new SendEmailResponse()
            {
                Success = response
            };
        }
    }
}
