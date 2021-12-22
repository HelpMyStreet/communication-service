using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.RequestService.Response;
using System.Globalization;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Utils;
using HelpMyStreet.Utils.Helpers;

namespace CommunicationService.MessageService
{
    public class InProgressReminderMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;

        public const int REQUESTOR_DUMMY_USERID = -1;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        { 
            return UnsubscribeGroupName.InProgressReminder;
        }

        public InProgressReminderMessage(IConnectRequestService connectRequestService, 
            IConnectUserService connectUserService,
            ICosmosDbService cosmosDbService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            _sendMessageRequests = new List<SendMessageRequest>();            
        }


        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            string encodedJobId = Base64Utils.Base64Encode(jobId.ToString());
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            return new EmailBuildData()
            {
                BaseDynamicData = new InProgressReminderData
                (
                    title: "A request you’ve accepted may need updating",
                    subject: "A request you’ve accepted may need updating",
                    firstName: user.UserPersonalDetails.FirstName,
                    encodedJobID: encodedJobId
                ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}",
                JobID = job.JobSummary.JobID,
                RequestID = job.JobSummary.RequestID,
                GroupID = job.JobSummary.ReferringGroupID,
                RecipientUserID  = recipientUserId.Value
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            if (job == null)
            {
                throw new Exception($"Job details cannot be retrieved for jobId {jobId}");
            }

            int? currentOrLastVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);                        
            if (currentOrLastVolunteerUserID.HasValue)
            {
                if (!_cosmosDbService.EmailSent(TemplateName.InProgressReminder, jobId.Value, currentOrLastVolunteerUserID.Value).Result)
                {
                    _sendMessageRequests.Add(new SendMessageRequest()
                    {
                        TemplateName = TemplateName.InProgressReminder,
                        RecipientUserID = currentOrLastVolunteerUserID.Value,
                        GroupID = groupId,
                        JobID = jobId,
                        RequestID = requestId
                    });
                }

            }            
            return _sendMessageRequests;
        }


    }
}
