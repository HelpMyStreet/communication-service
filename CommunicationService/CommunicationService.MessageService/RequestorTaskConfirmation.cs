using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public struct GroupJob
    {
        public GroupJob(
            SupportActivities supportActivity,
            int count
            )
        {
            SupportActivity = supportActivity;
            Count = count;
        }
        public SupportActivities SupportActivity { get; private set; }
        public int Count { get; private set; }        
    }

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

        private List<RequestJob> GetJobsForStandardRequest(GetRequestDetailsResponse response, List<GroupJob> groupJobs)
        {
            List<RequestJob> requestJobs = new List<RequestJob>();

            string dueDateString = string.Empty;

            //TODO - This can be written to handle multiple jobs for a standard request
            if (groupJobs.Count==1 && groupJobs[0].Count==1)
            {
                GetJobSummaryResponse jobResponse = _connectRequestService.GetJobSummaryAsync(response.RequestSummary.JobSummaries[0].JobID).Result;

                if (jobResponse != null)
                {
                    dueDateString = $" - DueDate: {jobResponse.JobSummary.DueDate} ";
                }
                else
                {
                    throw new Exception($"Unable to retrieve job summary for jobid { response.RequestSummary.JobSummaries[0].JobID}");
                }                
            }
                        
            foreach (GroupJob gj in groupJobs)
            {
                requestJobs.Add(new RequestJob(
                    activity: gj.SupportActivity.FriendlyNameShort(),
                    countString: gj.Count == 1 ? string.Empty : $" - count ({gj.Count})",
                    dueDateString: dueDateString
                    ));
            }
            return requestJobs;
        }

        private List<RequestJob> GetJobsForShiftRequest(GetRequestDetailsResponse response, List<GroupJob> groupJobs)
        {
            List<RequestJob> requestJobs = new List<RequestJob>();

            string dueDateString = $" - Start: {response.RequestSummary.Shift.StartDate } - {response.RequestSummary.Shift.EndDate}";

            foreach (GroupJob gj in groupJobs)
            {
                requestJobs.Add(new RequestJob(
                    activity: gj.SupportActivity.FriendlyNameShort(), 
                    countString: gj.Count == 1 ? string.Empty : $" - count ({gj.Count})",
                    dueDateString: dueDateString
                    ));
            }
            return requestJobs;
        }

        private List<RequestJob> GetJobs(GetRequestDetailsResponse response)
        {
            List<RequestJob> requestJobs = new List<RequestJob>();
            var groupJobs = response.RequestSummary.JobSummaries
                .GroupBy(x => x.SupportActivity)
                .Select(g => new GroupJob(g.Key, g.Count()))
                .ToList();

            switch (response.RequestSummary.RequestType)
            {
                case RequestType.Shift:
                    return GetJobsForShiftRequest(response, groupJobs);
                case RequestType.Task:
                    return GetJobsForStandardRequest(response, groupJobs);
                default:
                    throw new Exception($"Unknown requestType { response.RequestSummary.RequestType }");
            }            
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var requestDetails = await _connectRequestService.GetRequestDetailsAsync(requestId.Value);

            if(requestDetails == null)
            {
                throw new Exception($"Unable to return request details for requestId {requestId.Value}");
            }

            var group = await _connectGroupService.GetGroup(requestDetails.RequestSummary.ReferringGroupID);

            return new EmailBuildData()
            {
                BaseDynamicData = new RequestorTaskConfirmationData
                (
                    firstname: requestDetails.Requestor.FirstName,
                    statusIsOpen: additionalParameters["PendingApproval"] == true.ToString(),
                    groupName: group.Group.GroupName,
                    requestJobList: GetJobs(requestDetails)
                ),
                EmailToAddress = requestDetails.Requestor.EmailAddress,
                EmailToName = $"{requestDetails.Requestor.FirstName} {requestDetails.Requestor.LastName}",
                RecipientUserID = REQUESTOR_DUMMY_USERID,
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            int? pRequestId = null;
            if (jobId.HasValue && !requestId.HasValue)
            {
                var jobSummary = await _connectRequestService.GetJobSummaryAsync(jobId.Value);
                if(jobSummary!=null)
                {
                    pRequestId = jobSummary.JobSummary.RequestID;
                }
                else
                {
                    throw new Exception($"Unable to retrieve job summary for job id { jobId.Value }");
                }
            }
            else
            {
                pRequestId = requestId.Value;
            }

            
            // Add dummy recipient to represent requestor, who will not necessarily exist within our DB and so has no userID to lookup/refer to
            AddRecipientAndTemplate(TemplateName.RequestorTaskNotification, REQUESTOR_DUMMY_USERID, null, groupId, pRequestId, additionalParameters);

            return _sendMessageRequests;
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
