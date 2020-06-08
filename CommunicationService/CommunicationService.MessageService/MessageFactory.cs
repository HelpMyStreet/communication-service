using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class MessageFactory : IMessageFactory
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IQueueClient _queueClient;

        public MessageFactory(IConnectUserService connectUserService, IConnectRequestService connectRequestService, IQueueClient queueClient)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _queueClient = queueClient;
        }
        public IMessage Create(SendCommunicationRequest sendCommunicationRequest)
        {
            switch (sendCommunicationRequest.CommunicationJob.CommunicationJobType)
            {
                case CommunicationJobTypes.SendWelcomeMessage:
                    return new WelcomeMessage(_connectUserService);
                case CommunicationJobTypes.SendNewTaskNotification:
                    return new NewTaskNotificationMessage(_connectUserService,_connectRequestService);
                default:
                    throw new Exception("Unknown Email Type");

            }
        }

        public IMessage Create(SendMessageRequest sendMessageRequest)
        {
            switch (sendMessageRequest.CommunicationJobType)
            {
                case CommunicationJobTypes.SendWelcomeMessage:
                    return new WelcomeMessage(_connectUserService);
                case CommunicationJobTypes.SendNewTaskNotification:
                    return new NewTaskNotificationMessage(_connectUserService, _connectRequestService);
                default:
                    throw new Exception("Unknown Email Type");

            }
        }

        public async Task AddToMessageQueueAsync(SendMessageRequest sendMessageRequest)
        {
            string messageBody = JsonConvert.SerializeObject(sendMessageRequest);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Send the message to the queue
            await _queueClient.SendAsync(message);
        }
    }
}
