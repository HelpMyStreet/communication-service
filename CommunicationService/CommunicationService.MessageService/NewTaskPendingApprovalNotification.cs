using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
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

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            if (!groupId.HasValue || !requestId.HasValue)
            {
                throw new Exception($"GroupID or RequestID are missing");
            }

            List<int> groupTaskAdmins = await _connectGroupService.GetGroupMembersForGivenRole(groupId.Value, GroupRoles.TaskAdmin);

            foreach(int userId in groupTaskAdmins)
            {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.NewTaskPendingApprovalNotification,
                    RecipientUserID = userId,
                    GroupID = groupId,
                    JobID = null,
                    RequestID = requestId,
                });
            }

            return _sendMessageRequests;
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var requestDetails = await _connectRequestService.GetRequestDetailsAsync(requestId.Value);

            if (requestDetails == null)
            {
                throw new Exception($"Unable to return request details for requestId {requestId.Value}");
            }

            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var group = await _connectGroupService.GetGroup(requestDetails.RequestSummary.ReferringGroupID);

            string encodedRequestId = Base64Utils.Base64Encode(requestDetails.RequestSummary.RequestID.ToString());

            if (requestDetails != null && user?.UserPersonalDetails != null && group?.Group != null)
            {
                var token = await _linkRepository.CreateLink($"/link/r/{encodedRequestId}", _linkConfig.Value.ExpiryDays);

                return new EmailBuildData()
                {
                    BaseDynamicData = new NewTaskPendingApprovalData
                    (
                        user.UserPersonalDetails.FirstName,
                        group.Group.GroupName,
                        token
                    ),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}",
                    RequestID = requestDetails.RequestSummary.RequestID,
                    GroupID = requestDetails.RequestSummary.ReferringGroupID,
                    ReferencedJobs = new List<ReferencedJob>()
                {
                    new ReferencedJob()
                    {
                        G = requestDetails.RequestSummary.ReferringGroupID,
                        R = requestDetails.RequestSummary.RequestID,
                    }
                }
                };
            }
            throw new Exception("Unable to retrieve necessary details to build email");
        }
    }
}
