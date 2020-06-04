using CommunicationService.Core.Domains.Entities.Request;
using CommunicationService.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class MessageFactory : IMessageFactory
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        const string ServiceBusConnectionString = "Endpoint=sb://helpmystreet-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=8+WjZcHI5bCgKfYeyZoLsr6+KxbGad90k/wdT7bb5vw=";
        const string QueueName = "recipient";
        private readonly IQueueClient _queueClient;

        public MessageFactory(IConnectUserService connectUserService, IConnectRequestService connectRequestService, IQueueClient queueClient)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _queueClient = queueClient;
        }
        public IMessage Create(SendCommunicationRequest sendCommunicationRequest)
        {
            switch (sendCommunicationRequest.EmailTemplate.EmailTypes)
            {
                case EmailTypes.Welcome:
                    return new WelcomeMessage(_connectUserService);
                case EmailTypes.TaskNotification:
                    return new NewTaskNotificationMessage(_connectUserService,_connectRequestService);
                default:
                    throw new Exception("Unknown Email Type");

            }
        }
        public async Task AddToRecipientQueueAsync(SendCommunicationRequest sendCommunicationRequest)
        {
            string messageBody = JsonConvert.SerializeObject(sendCommunicationRequest);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Send the message to the queue
            await _queueClient.SendAsync(message);
        }
    }
}
