using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Models;
using CommunicationService.MessageService.Substitution;
using System.Globalization;
using Polly.Caching;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Extensions;

namespace CommunicationService.MessageService
{
    public class InterUserMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private const string EMAIL_TO_ADDRESS = "EmailToAddress";
        private const string EMAIL_TO_NAME = "EmailToName";
        private const string RECIPIENT_FIRST_NAME = "RecipientFirstName";

        List<SendMessageRequest> _sendMessageRequests;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.InterUserMessage;
            }
        }

        public InterUserMessage(IConnectRequestService connectRequestService, IConnectUserService connectUserService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<Dictionary<string,string>> GetRecipientDetails(int? recipientUserId, Dictionary<string, string> additionalParameters)
        {
            var result = new Dictionary<string, string>();

            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if(user!=null)
            {
                result.Add(EMAIL_TO_ADDRESS, user.UserPersonalDetails.EmailAddress);
                result.Add(EMAIL_TO_NAME, $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}");
                result.Add(RECIPIENT_FIRST_NAME, user.UserPersonalDetails.FirstName);
                return result;
            }

            if(additionalParameters.TryGetValue("RecipientEmailAddress", out string emailAddress) 
                && additionalParameters.TryGetValue("RecipientDisplayName", out string displayName))
            {
                result.Add(EMAIL_TO_ADDRESS, emailAddress);
                result.Add(EMAIL_TO_NAME, displayName);
                result.Add(RECIPIENT_FIRST_NAME, displayName);
                return result;
            }

            throw new Exception("Unable to Get Recipient details");
        }

        private async Task<string> SenderAndContext(string senderName, string fromRequestorRole, int? jobId)
        {
            string result = string.Empty;
            RequestRoles requestRole = (RequestRoles)Enum.Parse(typeof(RequestRoles), fromRequestorRole);
            switch(requestRole)
            {
                case RequestRoles.Recipient:
                    result = $"{senderName}, the person you helped.";
                    break;
                case RequestRoles.Requestor:
                    result = $"{senderName}, the person you requested help for.";
                    break;
                case RequestRoles.Volunteer:
                    result = $"{senderName}, the person who requested help for you.";
                    break;
                default:
                    break;
            }

            if(jobId.HasValue)
            {
                var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);
                if (job != null)
                {
                    DateTime dtStatusChanged = job.JobSummary.DateStatusLastChanged;
                    result = $"{result} The request was for { Mapping.ActivityMappings[job.JobSummary.SupportActivity]} and was {Mapping.StatusMappingsNotifications[job.JobSummary.JobStatus]} {dtStatusChanged.FriendlyPastDate()}";
                }
            }
            return result;
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            if (!recipientUserId.HasValue || !jobId.HasValue)
            {
                throw new Exception($"Recipient or JobID is missing");
            }

            if (recipientUserId.Value > 0)
            {
                var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            }
            var recipientDetails = await GetRecipientDetails(recipientUserId, additionalParameters);

            string subject = "A personal message sent through HelpMyStreet";
            string title = "Personal message received";
            string recipientFirstName = string.Empty;
            string senderFirstName = string.Empty;
            additionalParameters.TryGetValue("FromRequestorRole", out string fromRequestorRole);            
            string senderMessage = string.Empty;
            string emailToAddress = string.Empty;
            string emailToName = string.Empty;            

            if(recipientDetails!=null)
            {
                emailToAddress = recipientDetails[EMAIL_TO_ADDRESS];
                emailToName = recipientDetails[EMAIL_TO_NAME];
                recipientFirstName = recipientDetails[RECIPIENT_FIRST_NAME];
            }

            if (additionalParameters != null)
            {
                additionalParameters.TryGetValue("SenderMessage", out senderMessage);
                additionalParameters.TryGetValue("SenderFirstName", out senderFirstName);               
            }
            string senderAndContext = await SenderAndContext( senderFirstName, fromRequestorRole, jobId);

            return new EmailBuildData()
            {
                BaseDynamicData = new InterUserMessageData(
                    title,
                    subject,
                    recipientFirstName,
                    senderAndContext,
                    senderFirstName,
                    senderMessage
                    ),
                EmailToAddress = emailToAddress,
                EmailToName = emailToName
            };
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                JobID = jobId,
                GroupID = groupId
            });
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            return null;
        }
    }
}
