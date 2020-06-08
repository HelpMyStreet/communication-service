using CommunicationService.Core.Interfaces.Repositories;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.CommunicationService.Response;
using MediatR;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediatR.Pipeline;

namespace CommunicationService.Handlers
{
    public class SendCommunicationHandler : IRequestHandler<SendCommunicationRequest,SendCommunicationResponse>
    {
        const string ServiceBusConnectionString = "Endpoint=sb://helpmystreet-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=8+WjZcHI5bCgKfYeyZoLsr6+KxbGad90k/wdT7bb5vw=";
        const string QueueName = "job";
        static IQueueClient _queueClient;
        private readonly ICosmosDbService _cosmosDbService;

        public SendCommunicationHandler(IQueueClient queueClient, ICosmosDbService cosmosDbService)
        {
            _queueClient = queueClient;
            _cosmosDbService = cosmosDbService;
        }

        public async Task<SendCommunicationResponse> Handle(SendCommunicationRequest request, CancellationToken cancellationToken)
        {
            _queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
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
