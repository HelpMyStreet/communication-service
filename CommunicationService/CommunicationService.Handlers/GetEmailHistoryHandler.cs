using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CommunicationService.Core.Interfaces.Repositories;

namespace CommunicationService.Handlers
{
    public class GetEmailHistoryHandler : IRequestHandler<GetEmailHistoryRequest, GetEmailHistoryResponse>
    {
        private readonly ICosmosDbService _cosmosDbService;

        public GetEmailHistoryHandler(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        public async Task<GetEmailHistoryResponse> Handle(GetEmailHistoryRequest request, CancellationToken cancellationToken)
        {
            return new GetEmailHistoryResponse()
            {
                EmailHistoryDetails = await _cosmosDbService.GetEmailHistoryDetails(request.RequestId)
            };
        }
    }
}
