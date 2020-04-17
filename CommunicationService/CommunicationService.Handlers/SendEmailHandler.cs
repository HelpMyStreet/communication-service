using CommunicationService.Core.Domains.Entities;
using CommunicationService.Core.Interfaces.Repositories;
using MediatR;
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

        public SendEmailHandler(IRepository repository)
        {
            _repository = repository;
        }

        public Task<Unit> Handle(SendEmailRequest request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }
    }
}
