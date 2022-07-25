using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Extensions;
using CommunicationService.Core.Interfaces.Repositories;
using System.Linq;
using HelpMyStreet.Utils.Enums;

namespace CommunicationService.MessageService
{
    public class UserDeletedMessage : IMessage
    {
        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientId)
        {
            return UnsubscribeGroupName.NotUnsubscribable;
        }

        public UserDeletedMessage()
        {
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            if (!recipientUserId.HasValue)
            {
                throw new Exception("RecipientUserID is null");
            }

            string emailAddress = additionalParameters["EmailAddress"];
            string recipientName = additionalParameters["RecipientDisplayName"];
            string recipientFirstName = additionalParameters["RecipientFirstName"];

            if (string.IsNullOrEmpty(emailAddress))
            {
                throw new Exception($" no emailAddress supplied");
            }

            if (string.IsNullOrEmpty(recipientName))
            {
                throw new Exception($" no recipientName supplied");
            }

            if (string.IsNullOrEmpty(recipientFirstName))
            {
                throw new Exception($" no recipientFirstName supplied");
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new UserDeletedData(
                    title: "Your account has been deleted",
                    subject: "Your account has been deleted",
                    firstName : recipientFirstName
                    ),
                RecipientUserID = recipientUserId.Value,
                EmailToAddress = emailAddress,
                EmailToName = recipientName
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.UserDeleted,
                RecipientUserID = recipientUserId.Value,
                GroupID = groupId,
                JobID = jobId,
                RequestID = requestId,
                AdditionalParameters = additionalParameters
            });
            
            return _sendMessageRequests;
        }
    }
}
