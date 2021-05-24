using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Exceptions;
using System.Linq;
using HelpMyStreet.Contracts.RequestService.Response;

namespace CommunicationService.MessageService
{
    public class GroupWelcomeMessage : IMessage
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? receipientId)
        {
                return UnsubscribeGroupName.GroupWelcome;
        }

        public GroupWelcomeMessage(IConnectGroupService connectGroupService, IConnectUserService connectUserService)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            if (recipientUserId == null)
            {
                throw new BadRequestException("recipientUserId is null");
            }

            if (!groupId.HasValue)
            {
                throw new BadRequestException("GroupId is null");
            }

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;

            if (user == null)
            {
                throw new BadRequestException($"unable to retrieve user object for {recipientUserId.Value}");
            }

            var group = _connectGroupService.GetGroup(groupId.Value).Result;

            if (group == null)
            {
                throw new BadRequestException($"unable to retrieve group details for {groupId.Value}");
            }

            var groupEmailConfiguration = _connectGroupService.GetGroupEmailConfiguration(groupId.Value, CommunicationJobTypes.GroupWelcome).Result;

            if (groupEmailConfiguration == null)
            {
                throw new BadRequestException($"unable to retrieve group email configuration for {groupId.Value}");
            }

            var headerRequired = groupEmailConfiguration.Where(x => x.Key == "HeaderRequired").FirstOrDefault();
            var groupContent = groupEmailConfiguration.Where(x => x.Key == "GroupContent").FirstOrDefault();
            var groupSignature = groupEmailConfiguration.Where(x => x.Key == "GroupSignature").FirstOrDefault();
            var groupPS = groupEmailConfiguration.Where(x => x.Key == "GroupPS").FirstOrDefault();

            return new EmailBuildData()
            {
                BaseDynamicData = new GroupWelcomeData(
                    title: $"Welcome to {group.Group.GroupName} on HelpMyStreet!",
                    subject: $"Welcome to {group.Group.GroupName}!",
                    firstName: user.UserPersonalDetails.FirstName,
                    groupName: group.Group.GroupName,
                    groupContentAvailable: !string.IsNullOrEmpty(groupContent.Value),
                    groupContent: groupContent.Value,
                    groupSignatureAvailable: !string.IsNullOrEmpty(groupSignature.Value),
                    groupSignature: groupSignature.Value,
                    groupPSAvailable: !string.IsNullOrEmpty(groupPS.Value),
                    groupPS: groupPS.Value
                    ),
                    JobID = jobId,
                    GroupID = groupId,
                    RecipientUserID = recipientUserId.Value,
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
            };
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId, int? requestId)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                JobID = jobId,
                GroupID = groupId,
                RequestID = requestId
            });
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            AddRecipientAndTemplate(TemplateName.GroupWelcome, recipientUserId.Value, null, groupId, null);
            return _sendMessageRequests;
        }
    }
}
