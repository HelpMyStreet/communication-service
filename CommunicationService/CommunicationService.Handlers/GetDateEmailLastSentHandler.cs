using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CommunicationService.Core.Interfaces.Repositories;
using System;
using System.Linq;

namespace CommunicationService.Handlers
{
    public class GetDateEmailLastSentHandler : IRequestHandler<GetDateEmailLastSentRequest, GetDateEmailLastSentResponse>
    {
        private readonly ICosmosDbService _cosmosDbService;

        public GetDateEmailLastSentHandler(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        public async Task<GetDateEmailLastSentResponse> Handle(GetDateEmailLastSentRequest request, CancellationToken cancellationToken)
        {
            var emailHistory = await _cosmosDbService.GetEmailHistory(request.TemplateName, request.RecipientUserId.ToString());
            DateTime? maxDateEmailSent = null;

            if (emailHistory.Count > 0)
            {
               maxDateEmailSent = emailHistory.Max(x => x.LastSent);
            }

            return new GetDateEmailLastSentResponse()
            {
                DateEmailSent = maxDateEmailSent
            };
        }
    }
}
