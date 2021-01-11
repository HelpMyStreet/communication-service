using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Enums;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Utils;
using Microsoft.Extensions.Options;

namespace CommunicationService.MessageService
{
    public class NewTaskPendingApprovalNotification : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly ILinkRepository _linkRepository;
        private readonly IOptions<LinkConfig> _linkConfig;
        List<SendMessageRequest> _sendMessageRequests;

        public NewTaskPendingApprovalNotification(IConnectRequestService connectRequestService, IConnectGroupService connectGroupService, IConnectUserService connectUserService, ILinkRepository linkRepository, IOptions<LinkConfig> linkConfig)
        {
            _connectRequestService = connectRequestService;
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _linkRepository = linkRepository;
            _linkConfig = linkConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.NewTaskPendingApprovalNotification;
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {
            if (!groupId.HasValue || !jobId.HasValue)
            {
                throw new Exception($"GroupID or JobID is missing");
            }

            List<int> groupTaskAdmins = await _connectGroupService.GetGroupMembersForGivenRole(groupId.Value, GroupRoles.TaskAdmin);

            foreach(int userId in groupTaskAdmins)
            {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.NewTaskPendingApprovalNotification,
                    RecipientUserID = userId,
                    GroupID = groupId,
                    JobID = jobId
                });
            }

            return _sendMessageRequests;
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var group = await _connectGroupService.GetGroup(job.JobSummary.ReferringGroupID);

            string encodedJobId = Base64Utils.Base64Encode(job.JobSummary.JobID.ToString());

            if (job != null && user?.UserPersonalDetails != null && group?.Group != null)
            {
                var token = await _linkRepository.CreateLink($"/link/j/{encodedJobId}", _linkConfig.Value.ExpiryDays);

                return new EmailBuildData()
                {
                    BaseDynamicData = new NewTaskPendingApprovalData
                    (
                        user.UserPersonalDetails.FirstName,
                        group.Group.GroupName,
                        token
                    ),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
                };
            }
            throw new Exception("Unable to retrieve necessary details to build email");
        }
    }
}
