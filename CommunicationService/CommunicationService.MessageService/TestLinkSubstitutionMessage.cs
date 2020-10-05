using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class TestLinkSubstitutionMessage : IMessage
    {        
        private readonly IConnectRequestService _connectRequestService;
        private readonly ILinkRepository _linkRepository;
        private readonly IOptions<EmailConfig> _emailConfig;
        List<SendMessageRequest> _sendMessageRequests;

        public const int REQUESTOR_DUMMY_USERID = -1;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            if (recipientUserId == REQUESTOR_DUMMY_USERID)
            {
                return UnsubscribeGroupName.ReqTaskNotification;
            }
            else {
                return UnsubscribeGroupName.TaskNotification;
            }
                    
        }


        public TestLinkSubstitutionMessage(IConnectRequestService connectRequestService, ILinkRepository linkRepository, IOptions<EmailConfig> emailConfig)
        {            
            _connectRequestService = connectRequestService;
            _linkRepository = linkRepository;
            _emailConfig = emailConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            string encodedJobId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(job.JobSummary.JobID.ToString());
            string protectedUrl = $"/account/accepted-requests?j={encodedJobId}";

            var token = await _linkRepository.CreateLink(protectedUrl, _emailConfig.Value.ExpiryDays);

            return new EmailBuildData()
            {
                BaseDynamicData = new TestLinkSubstitutionData
                (
                    "Test Link Substitution",
                    job.Requestor.FirstName,
                    "{{BaseUrl}}/link/" + token
                ),
                EmailToAddress = job.Requestor.EmailAddress,
                EmailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}"
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.TaskUpdateNew,
                RecipientUserID = REQUESTOR_DUMMY_USERID,
                GroupID = groupId,
                JobID = jobId,
                AdditionalParameters = new Dictionary<string, string>()
                {
                    {"RecipientOrRequestor", "Requestor"}
                }
            });
            return _sendMessageRequests;
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId)
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
}
