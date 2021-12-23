using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.Core.Services;
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
        private readonly IConnectGroupService _connectGroupService;
        private readonly IQueueClient _queueClient;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IJobFilteringService _jobFilteringService;
        private readonly IConnectAddressService _connectAddressService;
        private readonly IOptions<EmailConfig> _emailConfig;
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private readonly IOptions<LinkConfig> _linkConfig;
        private readonly ILinkRepository _linkRepository;

        public MessageFactory(IConnectUserService connectUserService,
            IConnectRequestService connectRequestService,
            IConnectGroupService connectGroupService,
            IQueueClient queueClient,
            ICosmosDbService cosmosDbService,
            IOptions<EmailConfig> emailConfig,
            IJobFilteringService jobFilteringService,
            IConnectAddressService connectAddressService,
            IOptions<SendGridConfig> sendGridConfig,
            IOptions<LinkConfig> linkConfig,
            ILinkRepository linkRepository)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _connectGroupService = connectGroupService;
            _queueClient = queueClient;
            _cosmosDbService = cosmosDbService;
            _emailConfig = emailConfig;
            _jobFilteringService = jobFilteringService;
            _connectAddressService = connectAddressService;
            _sendGridConfig = sendGridConfig;
            _linkConfig = linkConfig;
            _linkRepository = linkRepository;
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
                case CommunicationJobTypes.SendRegistrationChasers:
                    return new RegistrationChaserMessage(_connectUserService, _cosmosDbService,_emailConfig);
                case CommunicationJobTypes.SendNewTaskNotification:
                    return new TaskNotificationMessage(_connectUserService, _connectRequestService, _connectGroupService);
                case CommunicationJobTypes.RequestorTaskConfirmation:
                    return new RequestorTaskConfirmation(_connectRequestService, _connectGroupService, _connectAddressService, _linkRepository, _linkConfig, _sendGridConfig);
                case CommunicationJobTypes.SendTaskStateChangeUpdate:
                    return new TaskUpdateSimplifiedMessage(_connectRequestService, _connectUserService, _connectGroupService, _linkRepository, _linkConfig, _sendGridConfig, _connectAddressService);
                case CommunicationJobTypes.SendOpenTaskDigest:
                    return new DailyDigestMessage(_connectGroupService, _connectUserService, _connectRequestService, _emailConfig,_connectAddressService, _cosmosDbService);
                case CommunicationJobTypes.SendTaskReminder:
                    return new TaskReminderMessage(_connectRequestService, _connectUserService, _cosmosDbService);
                case CommunicationJobTypes.SendShiftReminder:
                    return new ShiftReminderMessage(_connectRequestService, _connectUserService, _connectAddressService, _linkRepository, _linkConfig);
                case CommunicationJobTypes.InterUserMessage:
                    return new InterUserMessage(_connectRequestService, _connectUserService);
                case CommunicationJobTypes.NewCredentials:
                    return new NewCredentialsMessage(_connectUserService, _connectGroupService);
                case CommunicationJobTypes.TaskDetail:
                    return new TaskDetailMessage(_connectGroupService, _connectUserService, _connectRequestService, _emailConfig, _cosmosDbService);
                case CommunicationJobTypes.SendNewRequestNotification:
                    return new NewRequestNotificationMessage(_connectRequestService, _connectAddressService, _connectUserService, _cosmosDbService, _emailConfig, _connectGroupService);
                case CommunicationJobTypes.NewTaskPendingApprovalNotification:
                    return new NewTaskPendingApprovalNotification(_connectRequestService, _connectGroupService, _connectUserService, _linkRepository, _linkConfig);
                case CommunicationJobTypes.GroupWelcome:
                    return new GroupWelcomeMessage(_connectGroupService, _connectUserService, _sendGridConfig);
                case CommunicationJobTypes.NewUserNotification:
                    return new NewUserNotificationMessage(_connectGroupService, _connectUserService, _sendGridConfig);
                case CommunicationJobTypes.InProgressReminder:
                    return new InProgressReminderMessage(_connectRequestService, _connectUserService, _cosmosDbService);
                case CommunicationJobTypes.JobsDueTomorrow:
                    return new NextDayReminderMessage(_connectRequestService, _connectUserService, _connectGroupService);
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
