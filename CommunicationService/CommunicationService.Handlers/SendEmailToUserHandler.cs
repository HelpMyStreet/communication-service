using CommunicationService.Core.Domains.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class SendEmailToUserHandler : IRequestHandler<SendEmailToUserRequest>
    {
        public Task<Unit> Handle(SendEmailToUserRequest request, CancellationToken cancellationToken)
        {            
            return Unit.Task;
        }
    }
}
