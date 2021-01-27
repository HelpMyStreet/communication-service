using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.MessageService.Substitution;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class NewRequestNotificationMessage : IMessage
    {
        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.TaskNotification;
        }

        public NewRequestNotificationMessage()
        {
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            AddRecipientAndTemplate(TemplateName.RequestNotification, recipientUserId.Value, jobId, groupId, requestId, additionalParameters);
            return _sendMessageRequests;
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            return new EmailBuildData()
            {
                BaseDynamicData = new NewRequestNotificationMessageData
                         (
                            title: "New vaccination programmme support shifts",
                            subject: "New vacinnation programme support shifts have been added to HelpMyStreet",
                            firstName:"Jawwad",
                            shift:true,
                            requestList: GetRequestList()
                         ),
                EmailToAddress = "jawwad@factor-50.co.uk",
                EmailToName = $"Jawwad Mukhtar"
            };
        }

        private List<JobDetails> GetRequestList()
        {
            return new List<JobDetails>()
            {
                new JobDetails("<strong>Vaccination Programme Support</strong> at <strong>Lincoln Hospital</strong>(20.00 miles away). 5 volunteers required. Shift: Tue, 22 December 2020 11:04 AM - 11:14 AM](Duration: [duration 1 hour 30 mins])"),
                new JobDetails("<strong>Vaccination Programme Support</strong> at <strong>Lincoln Hospital</strong>(20.00 miles away). 5 volunteers required. Shift: Tue, 20 January 2020 11:04 AM - 11:14 AM](Duration: [duration 1 hour 30 mins])")
            };
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                GroupID = groupId,
                JobID = jobId,
                RequestID = requestId,
                AdditionalParameters = additionalParameters
            });
        }
    }
}
