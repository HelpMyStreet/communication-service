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
    public class RequestToHelpApprovedMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectGroupService _connectGroupService;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.TaskUpdate;
        }

        public RequestToHelpApprovedMessage(IConnectRequestService connectRequestService,
            IConnectUserService connectUserService,
            IConnectGroupService connectGroupService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _connectGroupService = connectGroupService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            GetJobDetailsResponse job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            var group = await _connectGroupService.GetGroup(job.JobSummary.ReferringGroupID);
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            return new EmailBuildData()
            {
                BaseDynamicData = new TaskApplicationApprovedData
                (
                    title: "Your application to volunteer has been updated",
                    subject: "Your application to volunteer has been updated",
                    recipient: user.UserPersonalDetails.FirstName,
                    activityName: job.JobSummary.GetSupportActivityName,
                    requestor: job.Requestor.FullName(),
                    groupName: group.Group.GroupName
                ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.FirstName,
                GroupID = group.Group.GroupId,
                JobID = jobId.Value
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.TaskApplicationApproved,
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
