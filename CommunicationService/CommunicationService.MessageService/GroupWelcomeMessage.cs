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
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;

namespace CommunicationService.MessageService
{
    public class GroupWelcomeMessage : IMessage
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly IOptions<SendGridConfig> _sendGridConfig;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? receipientId)
        {
                return UnsubscribeGroupName.GroupWelcome;
        }

        public GroupWelcomeMessage(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IOptions<SendGridConfig> sendGridConfig)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _sendGridConfig = sendGridConfig;
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

            var groupMember = await _connectGroupService.GetGroupMember((int)HelpMyStreet.Utils.Enums.Groups.Generic, recipientUserId.Value, recipientUserId.Value);
            var showGroupLogo = GetValueFromConfig(groupEmailConfiguration, "ShowGroupLogo");
            var groupContent = GetValueFromConfig(groupEmailConfiguration, "GroupContent");
            var groupSignature = GetValueFromConfig(groupEmailConfiguration, "GroupSignature");
            var groupPS = GetValueFromConfig(groupEmailConfiguration, "GroupPS");
            string encodeGroupId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(groupId.Value.ToString());
            var showGroupRequestFormLink = GetValueFromConfig(groupEmailConfiguration, "ShowGroupRequestFormLink");

            bool groupLogoAvailable = string.IsNullOrEmpty(showGroupLogo) ? false : Convert.ToBoolean(showGroupLogo);
            string groupLogo = string.Empty;
            
            if(groupLogoAvailable)
            {
                groupLogo = $"group-logos/{group.Group.GroupKey}-partnership.png";
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new GroupWelcomeData(
                    title: $"Welcome to {group.Group.GroupName} on HelpMyStreet!",
                    subject: $"Welcome to {group.Group.GroupName}!",
                    firstName: user.UserPersonalDetails.FirstName,
                    groupLogoAvailable: groupLogoAvailable,
                    groupLogo: groupLogo,
                    groupName: group.Group.GroupName,
                    groupContentAvailable: !string.IsNullOrEmpty(groupContent),
                    groupContent: groupContent,
                    groupSignatureAvailable: !string.IsNullOrEmpty(groupSignature),
                    groupSignature: groupSignature,
                    groupPSAvailable: !string.IsNullOrEmpty(groupPS),
                    groupPS: groupPS,
                    encodedGroupId: encodeGroupId,
                    needYotiVerification: !groupMember.UserIsYotiVerified,
                    groupLocation: group.Group.GeographicName,
                    showGroupRequestFormLink: string.IsNullOrEmpty(showGroupRequestFormLink) ? false : Convert.ToBoolean(showGroupRequestFormLink)
                    ),
                    JobID = jobId,
                    GroupID = groupId,
                    RecipientUserID = recipientUserId.Value,
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
            };
        }

        private string GetValueFromConfig(List<KeyValuePair<string, string>> groupEmailConfiguration, string key)
        {
            var result = groupEmailConfiguration.Where(x => x.Key == key).FirstOrDefault();

            if(!string.IsNullOrEmpty(result.Value))
            {
                return result.Value.Replace("{{BaseUrl}}", _sendGridConfig.Value.BaseUrl);
            }
            else
            {
                return string.Empty;
            }
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
