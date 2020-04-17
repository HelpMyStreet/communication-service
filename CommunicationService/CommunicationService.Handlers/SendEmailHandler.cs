using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains.Entities;
using CommunicationService.Core.Interfaces.Repositories;
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

        public SendEmailHandler(IRepository repository, IOptions<SendGridConfig> sendGridConfig)
        {
            _repository = repository;
            _sendGridConfig = sendGridConfig;
            string a =_sendGridConfig.Value.ApiKey;
        }

        public Task<Unit> Handle(SendEmailRequest request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }
    }
}
