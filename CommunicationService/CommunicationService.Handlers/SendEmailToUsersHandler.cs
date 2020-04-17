using CommunicationService.Core.Domains.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.Handlers
{
    public class SendEmailToUsersHandler : IRequestHandler<SendEmailToUsersRequest>
    {
        public Task<Unit> Handle(SendEmailToUsersRequest request, CancellationToken cancellationToken)
        {            
            return Unit.Task;
        }
    }
}
