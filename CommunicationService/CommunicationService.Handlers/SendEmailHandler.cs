using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains.Entities;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class SendEmailHandler : IRequestHandler<SendEmailRequest>
    {
        private readonly IRepository _repository;
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private readonly ISendEmailService _sendEmailService;

        public SendEmailHandler(IRepository repository, IOptions<SendGridConfig> sendGridConfig, ISendEmailService sendEmailService)
        {
            _repository = repository;
            _sendGridConfig = sendGridConfig;
            _sendEmailService = sendEmailService;
            string a =_sendGridConfig.Value.ApiKey;
        }

        public async Task<Unit> Handle(SendEmailRequest request, CancellationToken cancellationToken)
        {
            await _sendEmailService.SendEmail(request);
            return Unit.Value;
        }
    }
}
