using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Contracts.RequestService.Response;

namespace CommunicationService.MessageService
{
    public class InterUserMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private const string RECIPIENT_EMAIL_ADDRESS = "RecipientEmailAddress";
        private const string RECIPIENT_DISPLAY_NAME = "RecipientDisplayName";
        private const string RECIPIENT_FIRST_NAME = "RecipientFirstName";
       
        public string GetUnsubscriptionGroupName(int? recipientId)
        {

                return UnsubscribeGroupName.InterUserMessage;
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

        private string SenderAndContextRecipient(string senderName, RequestRoles toRole)
        {
            switch (toRole)
            {
                case RequestRoles.Requestor:
                    return $"<strong>{senderName}</strong>, the person who you requested help for.";                  
                case RequestRoles.Volunteer:
                    return $"<strong>{senderName}</strong>, the person who you helped.";
                case RequestRoles.GroupAdmin:
                    return $"<strong>{senderName}</strong>, the person who needed help.";
                default:
                    throw new Exception($"Invalid Request Role {toRole}");
            }
        }

        private string SenderAndContextRequestor(string senderName, string helpRecipient, RequestRoles toRole)
        {
            switch (toRole)
            {
                case RequestRoles.Recipient:
                    return $"<strong>{senderName}</strong>, the person who requested help for you.";                    
                case RequestRoles.Volunteer:
                case RequestRoles.GroupAdmin:
                    return $"<strong>{senderName}</strong>, the person who requested help for <strong>{helpRecipient}</strong>.";
                default:
                    throw new Exception($"Invalid Request Role {toRole}");
            }
        }

        private string SenderAndContextVolunteer(string senderName, string helpRecipient, RequestRoles toRole)
        {
            switch (toRole)
            {
                case RequestRoles.Recipient:
                    return $"<strong>{senderName}</strong>, the volunteer who accepted the request for help.";
                case RequestRoles.Requestor:
                    return $"<strong>{senderName}</strong>, the volunteer who accepted the request for <strong>{helpRecipient}</strong>.";
                case RequestRoles.GroupAdmin:
                    return $"<strong>{senderName}</strong>, the volunteer who helped <strong>{helpRecipient}</strong>.";
                default:
                    throw new Exception($"Invalid Request Role {toRole}");
            }
        }

        private string SenderAndContextGroupAdmin(string senderName, string groupName)
        {
            return $"<strong>{senderName}</strong> at <strong>{groupName}</strong>.";
        }

        private async Task<string> SenderAndContext(string senderName, string senderRequestorRole, string toRequestorRole, string senderGroupName, GetJobDetailsResponse job)
        {
            string result = string.Empty ;
            RequestRoles senderRole = (RequestRoles)Enum.Parse(typeof(RequestRoles), senderRequestorRole);
            RequestRoles toRole = (RequestRoles)Enum.Parse(typeof(RequestRoles), toRequestorRole);
            string helpRecipient = string.Empty;

            if (job != null)
            {
                helpRecipient = job.Recipient?.FirstName;
            }

            switch (senderRole)
            {
                case RequestRoles.Recipient:
                    result = SenderAndContextRecipient(senderName, toRole);
                    break;
                case RequestRoles.Requestor:
                    result = SenderAndContextRequestor(senderName, helpRecipient, toRole);
                    break;
                case RequestRoles.Volunteer:
                    result = SenderAndContextVolunteer(senderName, helpRecipient, toRole);
                    break;
                case RequestRoles.GroupAdmin:
                    result = SenderAndContextGroupAdmin(senderName, senderGroupName);
                    break;                    
            }   

            if (job!=null)
            {
                DateTime dtStatusChanged = job.JobSummary.DateStatusLastChanged;
                result = $"{result} The request was for <strong>{ job.JobSummary.GetSupportActivityName}</strong> and was {Mapping.StatusMappingsNotifications[job.JobSummary.JobStatus]} <strong>{dtStatusChanged.FriendlyPastDate()}</strong>";
            }            
            return result;
        }

        private string GetTitle(string toGroupName)
        {
            if (string.IsNullOrEmpty(toGroupName))
            {
                return "Personal message received";
            }
            else
            {
                return $"Message received for {toGroupName}";
            }
        }

        private string GetSubject(string toGroupName)
        {
            if (string.IsNullOrEmpty(toGroupName))
            {
                return "A personal message sent through HelpMyStreet";
            }
            else
            {
                return $"A message for {toGroupName} sent through HelpMyStreet";
            }
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
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

            string recipientFirstName = string.Empty;
            string senderName = string.Empty;
            additionalParameters.TryGetValue("SenderRequestorRole", out string senderRequestorRole);
            string senderMessage = string.Empty;
            string emailToAddress = string.Empty;
            string emailToName = string.Empty;
            additionalParameters.TryGetValue("ToRequestorRole", out string toRequestorRole);
            additionalParameters.TryGetValue("SenderGroupName", out string senderGroupName);
            additionalParameters.TryGetValue("ToGroupName", out string toGroupName);
            string subject = GetSubject(toGroupName);
            string title = GetTitle(toGroupName);


            if (recipientDetails!=null)
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

            GetJobDetailsResponse job = null;
            if (jobId.HasValue)
            {
                job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);
            }

            string senderAndContext = await SenderAndContext( senderName, senderRequestorRole, toRequestorRole, senderGroupName, job);

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
                EmailToName = emailToName,
                JobID = job.JobSummary.JobID,
                RequestID = job.JobSummary.RequestID,
                GroupID = job.JobSummary.ReferringGroupID,
                ReferencedJobs = new List<ReferencedJob>()
                {
                    new ReferencedJob()
                    {
                        G = job.JobSummary.ReferringGroupID,
                        R = job.JobSummary.RequestID,
                        J = job.JobSummary.JobID
                    }
                }
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            return null;
        }
    }
}
