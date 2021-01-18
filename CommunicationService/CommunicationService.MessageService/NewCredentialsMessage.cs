using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Extensions;
using System.Linq;

namespace CommunicationService.MessageService
{
    public class NewCredentialsMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectGroupService _connectGroupService;
        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientId)
        {

                return UnsubscribeGroupName.NewCredentials;
        }

        public NewCredentialsMessage(IConnectUserService connectUserService, IConnectGroupService connectGroupService)
        {
            _connectUserService = connectUserService;
            _connectGroupService = connectGroupService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            if (!groupId.HasValue || !recipientUserId.HasValue)
            {
                throw new Exception("GroupId or RecipientUserID is null");
            }

            if (!additionalParameters.TryGetValue("CredentialId", out string credentialId))
            {
                throw new Exception("CredentialId is missing");
            }

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;

            if (user == null)
            {
                throw new Exception($"unable to retrieve user object for {recipientUserId.Value}");
            }

            var groupMemberDetails = await _connectGroupService.GetGroupMemberDetails(groupId.Value, recipientUserId.Value);
            var group = await _connectGroupService.GetGroup(groupId.Value);

            if(groupMemberDetails==null)
            {
                throw new Exception("GroupMemberDetails is null");
            }

            var credentials = groupMemberDetails.UserCredentials.Where(x => x.CredentialId.ToString() == credentialId).FirstOrDefault();

            if (credentials == null)
            {
                throw new Exception("Credentials is null");
            }

            var groupCredentials = await _connectGroupService.GetGroupCredentials(groupId.Value);

            if(groupCredentials==null)
            {
                throw new Exception($"GroupCredentials missingn for { groupId.Value }");
            }

            string credentialName = groupCredentials.GroupCredentials.Where(x => x.CredentialID.ToString() == credentialId).FirstOrDefault().Name;

            return new EmailBuildData()
            {
                BaseDynamicData = new NewCredentialsData(
                    "Credentials updated",
                    "New credentials have been successfully added",
                    user.UserPersonalDetails.FirstName,
                    credentialName,
                    group.Group.GroupName,
                    credentials.ExpiryDate.HasValue,
                    credentials.ExpiryDate.HasValue ? credentials.ExpiryDate.Value.FriendlyPastDate() : string.Empty
                    ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.DisplayName
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.NewCredentials,
                RecipientUserID = recipientUserId.Value,
                GroupID = groupId.Value,
                JobID = jobId,
                RequestID = requestId,
                AdditionalParameters = additionalParameters
            });

            return _sendMessageRequests;
        }
    }
}
