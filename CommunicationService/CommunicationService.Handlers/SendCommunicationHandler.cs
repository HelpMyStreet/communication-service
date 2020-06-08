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
    public class SendCommunicationHandler : IRequestHandler<SendCommunicationRequest,SendCommunicationResponse>
    {
        static IQueueClient _queueClient;
        private readonly IOptions<ServiceBusConfig> _serviceBusConfig;

        public SendCommunicationHandler(IQueueClient queueClient, IOptions<ServiceBusConfig> serviceBusConfig)
        {
            _queueClient = queueClient;
            _serviceBusConfig = serviceBusConfig;
            _queueClient = new QueueClient(_serviceBusConfig.Value.ConnectionString, _serviceBusConfig.Value.JobQueueName);

        }

        public async Task<SendCommunicationResponse> Handle(SendCommunicationRequest request, CancellationToken cancellationToken)
        {
            await SendMessagesAsync(request);
            return new SendCommunicationResponse()
            {
                Success = true
            };
        }

        private async Task SendMessagesAsync(SendCommunicationRequest sendCommunicationRequest)
        {
            string messageBody = JsonConvert.SerializeObject(sendCommunicationRequest);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Send the message to the queue
            await _queueClient.SendAsync(message);
        }
    }
}
