using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Extensions;
using CommunicationService.Core.Interfaces.Repositories;

namespace CommunicationService.MessageService
{
    public class ImpendingUserDeletionMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;
        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientId)
        {

                return UnsubscribeGroupName.ImpendingDeleteUser;
        }

        public ImpendingUserDeletionMessage(IConnectUserService connectUserService,ICosmosDbService cosmosDbService)
        {
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            if (!recipientUserId.HasValue)
            {
                throw new Exception("RecipientUserID is null");
            }

            HelpMyStreet.Utils.Models.User user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;

            if (user == null)
            {
                throw new Exception($"unable to retrieve user object for {recipientUserId.Value}");
            }

            DateTime dateToDelete = DateTime.SpecifyKind(DateTime.Now.Date.AddDays(31), DateTimeKind.Utc);

            return new EmailBuildData()
            {
                BaseDynamicData = new ImpendingUserDeletionData(
                    title: "Your HelpMyStreet account is due to be deleted",
                    subject: "Your HelpMyStreet account is due to be deleted",
                    firstName : user.UserPersonalDetails.FirstName,
                    dateToDelete: dateToDelete.ToString("dd/MM/yyyy")
                    ),
                RecipientUserID = recipientUserId.Value,
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.DisplayName
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            bool emailSent = await _cosmosDbService.EmailSent(TemplateName.ImpendingUserDeletion, recipientUserId.Value);

            if (!emailSent)
            {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.ImpendingUserDeletion,
                    RecipientUserID = recipientUserId.Value,
                    GroupID = groupId,
                    JobID = jobId,
                    RequestID = requestId,
                    AdditionalParameters = additionalParameters
                });
            }

            return _sendMessageRequests;
        }
    }
}
