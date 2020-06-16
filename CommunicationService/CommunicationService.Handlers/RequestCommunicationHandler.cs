using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using CommunicationService.Core.Interfaces.Services;

namespace CommunicationService.Handlers
{
    public class RequestCommunicationHandler : IRequestHandler<RequestCommunicationRequest,RequestCommunicationResponse>
    {
        static IQueueClient _queueClient;
        private readonly IOptions<ServiceBusConfig> _serviceBusConfig;

        public RequestCommunicationHandler(IQueueClient queueClient, IOptions<ServiceBusConfig> serviceBusConfig)
        {
            _queueClient = queueClient;
            _serviceBusConfig = serviceBusConfig;
            _queueClient = new QueueClient(_serviceBusConfig.Value.ConnectionString, _serviceBusConfig.Value.JobQueueName);
        }

        public async Task<RequestCommunicationResponse> Handle(RequestCommunicationRequest request, CancellationToken cancellationToken)
        {
            await SendMessagesAsync(request);
            return new RequestCommunicationResponse()
            {
                Success = true
            };
        }

        private async Task SendMessagesAsync(RequestCommunicationRequest sendCommunicationRequest)
        {
            string messageBody = JsonConvert.SerializeObject(sendCommunicationRequest);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Send the message to the queue
            await _queueClient.SendAsync(message);
        }
    }
}
