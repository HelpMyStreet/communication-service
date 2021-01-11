using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class PostYotiCommunicationMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;
        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientId)
        {
                return UnsubscribeGroupName.RegistrationUpdates;
        }

        private string GetTitleFromTemplateName(string templateName)
        {
            switch(templateName)
            {
                case TemplateName.Welcome:
                    return "Welcome to HelpMyStreet!";
                case TemplateName.ThanksForVerifying:
                    return "Thanks for verifying your ID";
                case TemplateName.UnableToVerify:
                    return "Hmm, something’s not quite right – can we help?";
                default:
                    throw new Exception($"{templateName} is unknown");
            }
        }

        public PostYotiCommunicationMessage(IConnectUserService connectUserService, ICosmosDbService cosmosDbService)
        {
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            _sendMessageRequests = new List<SendMessageRequest>();
            
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if (user != null)
            {
                return new EmailBuildData()
                {
                    BaseDynamicData = new PostYotiCommunicationData(user.UserPersonalDetails.FirstName, user.UserPersonalDetails.LastName, GetTitleFromTemplateName(templateName)),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = user.UserPersonalDetails.DisplayName
                };
            }
            else
            {
                throw new Exception("unable to retrieve user details");
            }
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId)
        {
            List<EmailHistory> history = _cosmosDbService.GetEmailHistory(templateName, userId.ToString()).Result;
            if (history.Count == 0)
            {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = templateName,
                    RecipientUserID = userId,
                    GroupID = groupId,
                    JobID = jobId
                });
            }
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {
            AddRecipientAndTemplate(TemplateName.Welcome, recipientUserId.Value, jobId, groupId);            
            return _sendMessageRequests;
        }
    }
}
