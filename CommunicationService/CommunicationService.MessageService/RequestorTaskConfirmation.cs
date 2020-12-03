using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class RequestorTaskConfirmation : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectGroupService _connectGroupService;
        List<SendMessageRequest> _sendMessageRequests;

        public const int REQUESTOR_DUMMY_USERID = -1;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.ReqTaskNotification;
        }

        public RequestorTaskConfirmation(IConnectRequestService connectRequestService, IConnectGroupService connectGroupService)
        {
            _connectRequestService = connectRequestService;
            _connectGroupService = connectGroupService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            var group = await _connectGroupService.GetGroup(job.JobSummary.ReferringGroupID);

            return new EmailBuildData()
            {
                BaseDynamicData = new RequestorTaskConfirmationData
                (
                    job.Requestor.FirstName,
                    additionalParameters["PendingApproval"] == true.ToString(),
                    group.Group.GroupName
                ),
                EmailToAddress = job.Requestor.EmailAddress,
                EmailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}",
                RecipientUserID = REQUESTOR_DUMMY_USERID,
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {
            // Add dummy recipient to represent requestor, who will not necessarily exist within our DB and so has no userID to lookup/refer to
            AddRecipientAndTemplate(TemplateName.RequestorTaskNotification, REQUESTOR_DUMMY_USERID, jobId, groupId);

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
