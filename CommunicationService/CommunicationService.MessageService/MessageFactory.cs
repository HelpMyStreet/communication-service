using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class MessageFactory : IMessageFactory
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IQueueClient _queueClient;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IOptions<EmailConfig> _emailConfig;

        public MessageFactory(IConnectUserService connectUserService, IConnectRequestService connectRequestService, IQueueClient queueClient, ICosmosDbService cosmosDbService, IOptions<EmailConfig> emailConfig)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _queueClient = queueClient;
            _cosmosDbService = cosmosDbService;
            _emailConfig = emailConfig;
        }
        public IMessage Create(RequestCommunicationRequest sendCommunicationRequest)
        {
            return GetMessage(sendCommunicationRequest.CommunicationJob.CommunicationJobType);
        }

        public IMessage Create(SendMessageRequest sendMessageRequest)
        {
            return GetMessage(sendMessageRequest.CommunicationJobType);
        }

        private IMessage GetMessage(CommunicationJobTypes communicationJobTypes)
        {
            switch (communicationJobTypes)
            {
                case CommunicationJobTypes.PostYotiCommunication:
                    return new PostYotiCommunicationMessage(_connectUserService, _cosmosDbService);
                case CommunicationJobTypes.SendRegistrationChasers:
                    return new RegistrationChaserMessage(_connectUserService, _cosmosDbService,_emailConfig);
                case CommunicationJobTypes.SendNewTaskNotification:
                    return new TaskNotificationMessage(_connectUserService, _connectRequestService);
                case CommunicationJobTypes.SendTaskStateChangeUpdate:
                    return new TaskUpdateMessage(_connectUserService, _connectRequestService);
                default:
                    throw new Exception("Unknown Email Type");
            }
        }

        public async Task AddToMessageQueueAsync(SendMessageRequest sendMessageRequest)
        {
            string messageBody = JsonConvert.SerializeObject(sendMessageRequest);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            // Send the message to the queue
            await _queueClient.SendAsync(message).ConfigureAwait(false);
        }
    }
}
