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
        private const string RECIPIENT_EMAIL_ADDRESS = "RecipientEmailAddress";
        private const string RECIPIENT_DISPLAY_NAME = "RecipientDisplayName";
        private const string RECIPIENT_FIRST_NAME = "RecipientFirstName";
       
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
        }

        public async Task<Dictionary<string,string>> GetRecipientDetails(int? recipientUserId, Dictionary<string, string> additionalParameters)
        {
            var result = new Dictionary<string, string>();

            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if(user!=null)
            {
                result.Add(RECIPIENT_EMAIL_ADDRESS, user.UserPersonalDetails.EmailAddress);
                result.Add(RECIPIENT_DISPLAY_NAME, $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}");
                result.Add(RECIPIENT_FIRST_NAME, user.UserPersonalDetails.FirstName);
                return result;
            }

            if(additionalParameters.TryGetValue("RecipientEmailAddress", out string emailAddress) 
                && additionalParameters.TryGetValue("RecipientDisplayName", out string displayName))
            {
                result.Add(RECIPIENT_EMAIL_ADDRESS, emailAddress);
                result.Add(RECIPIENT_DISPLAY_NAME, displayName);
                result.Add(RECIPIENT_FIRST_NAME, displayName);
                return result;
            }

            throw new Exception("Unable to Get Recipient details");
        }

        private async Task<string> SenderAndContext(string senderName, string senderRequestorRole, int? jobId)
        {
            string result = string.Empty;
            RequestRoles requestRole = (RequestRoles)Enum.Parse(typeof(RequestRoles), senderRequestorRole);
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
            if (!recipientUserId.HasValue)
            {
                throw new Exception($"RecipientID is missing");
            }

            if (recipientUserId.Value > 0)
            {
                var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            }
            var recipientDetails = await GetRecipientDetails(recipientUserId, additionalParameters);

            string subject = "A personal message sent through HelpMyStreet";
            string title = "Personal message received";
            string recipientFirstName = string.Empty;
            string senderName = string.Empty;
            additionalParameters.TryGetValue("SenderRequestorRole", out string senderRequestorRole);            
            string senderMessage = string.Empty;
            string emailToAddress = string.Empty;
            string emailToName = string.Empty;

            if(recipientDetails!=null)
            {
                emailToAddress = recipientDetails[RECIPIENT_EMAIL_ADDRESS];
                emailToName = recipientDetails[RECIPIENT_DISPLAY_NAME];
                recipientFirstName = recipientDetails[RECIPIENT_FIRST_NAME];
            }

            if (additionalParameters != null)
            {
                additionalParameters.TryGetValue("SenderMessage", out senderMessage);
                additionalParameters.TryGetValue("SenderName", out senderName);               
            }
            string senderAndContext = await SenderAndContext( senderName, senderRequestorRole, jobId);

            return new EmailBuildData()
            {
                BaseDynamicData = new InterUserMessageData(
                    title,
                    subject,
                    recipientFirstName,
                    senderAndContext,
                    senderName,
                    senderMessage
                    ),
                EmailToAddress = emailToAddress,
                EmailToName = emailToName
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            return null;
        }
    }
}
