﻿using CommunicationService.Core.Configuration;
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
using HelpMyStreet.Utils.Utils;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
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
        private readonly IConnectAddressService _connectAddressService;
        private readonly ILinkRepository _linkRepository;
        private readonly IOptions<LinkConfig> _linkConfig;
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        List<SendMessageRequest> _sendMessageRequests;

        public const int REQUESTOR_DUMMY_USERID = -1;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.ReqTaskNotification;
        }

        public RequestorTaskConfirmation(IConnectRequestService connectRequestService, 
            IConnectGroupService connectGroupService, 
            IConnectAddressService connectAddressService,
            ILinkRepository linkRepository, 
            IOptions<LinkConfig> linkConfig, 
            IOptions<SendGridConfig> sendGridConfig)
        {
            _connectRequestService = connectRequestService;
            _connectGroupService = connectGroupService;
            _connectAddressService = connectAddressService;
            _linkRepository = linkRepository;
            _linkConfig = linkConfig;
            _sendGridConfig = sendGridConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        private string GetJobUrl(int jobId, int requestId, int jobCount)
        {
            string baseUrl = _sendGridConfig.Value.BaseUrl;
            string encodedId = jobCount == 1 ? Base64Utils.Base64Encode(jobId.ToString()) : Base64Utils.Base64Encode(requestId.ToString());

            string tailUrl = jobCount == 1 ? $"/link/j/{encodedId}" : $"/link/r/{encodedId}";
            var token = _linkRepository.CreateLink(tailUrl, _linkConfig.Value.ExpiryDays).Result;
            return $"{baseUrl}/link/{token}";
        }

        private RequestRoles GetChangedByRole(GetJobDetailsResponse job)
        {
            int lastUpdatedByUserId = _connectRequestService.GetLastUpdatedBy(job);
            int? currentOrLastVolunteerUserID = _connectRequestService.GetRelevantVolunteerUserID(job);
            return currentOrLastVolunteerUserID.HasValue && currentOrLastVolunteerUserID.Value == lastUpdatedByUserId ? RequestRoles.Volunteer : RequestRoles.GroupAdmin;
        }

        private List<RequestJob> GetJobsForStandardRequest(GetRequestDetailsResponse response, List<GroupJob> groupJobs)
        {
            List<RequestJob> requestJobs = new List<RequestJob>();

            string dueDateString = string.Empty;
            bool showJobUrl = false;
            string jobUrl = string.Empty;
            
            //TODO - This can be written to handle multiple jobs for a standard request. Need to tweak when repeat enquiries functionality added
            if (groupJobs.Count==1)
            {
                int jobid = response.RequestSummary.JobBasics[0].JobID;
                GetJobDetailsResponse jobResponse = _connectRequestService.GetJobDetailsAsync(jobid).Result;

                if (jobResponse != null)
                {
                    RequestRoles getChangedBy = GetChangedByRole(jobResponse);
                    showJobUrl = getChangedBy == RequestRoles.Volunteer
               || getChangedBy == RequestRoles.GroupAdmin
               || (getChangedBy == RequestRoles.Requestor && jobResponse.JobSummary.RequestorDefinedByGroup);
                    jobUrl = showJobUrl ? GetJobUrl(jobid, response.RequestSummary.RequestID, groupJobs[0].Count) : string.Empty;

                    dueDateString = $" - Due Date: <strong>{jobResponse.JobSummary.DueDate.FormatDate(DateTimeFormat.LongDateFormat)}.</strong>";
                }
                else
                {
                    throw new Exception($"Unable to retrieve job details for jobid { jobid }");
                }                
            }
                        
            foreach (GroupJob gj in groupJobs)
            {
                requestJobs.Add(new RequestJob(
                    activity: gj.SupportActivity.FriendlyNameShort(),
                    countString: gj.Count == 1 ? string.Empty: $" - {gj.Count} volunteers required. ",
                    dueDateString: dueDateString,
                    showJobUrl: showJobUrl,
                    jobUrl: jobUrl
                    ));
            }
            return requestJobs;
        }

        private List<RequestJob> GetJobsForShiftRequest(GetRequestDetailsResponse response, List<GroupJob> groupJobs)
        {
            List<RequestJob> requestJobs = new List<RequestJob>();

            var locationDetails = _connectAddressService.GetLocationDetails(response.RequestSummary.Shift.Location, CancellationToken.None).Result;

            if(locationDetails ==null)
            {
                throw new Exception($"Unable to retrieve location details for request {response.RequestSummary.RequestID}");
            }

            string locationName = locationDetails.ShortName;

            var time = TimeSpan.FromMinutes(response.RequestSummary.Shift.ShiftLength);

            string dueDateString = $"Shift: <strong>{response.RequestSummary.Shift.StartDate.FormatDate(DateTimeFormat.LongDateTimeFormat)} - {response.RequestSummary.Shift.EndDate.FormatDate(DateTimeFormat.TimeFormat)}</strong> " +
                $"(Duration: {Math.Floor(time.TotalHours)} hrs {time.Minutes} mins). " +
                $"Location: <strong>{locationName}</strong>";

            foreach (GroupJob gj in groupJobs)
            {
                requestJobs.Add(new RequestJob(
                    activity: gj.SupportActivity.FriendlyNameShort(),
                    countString: gj.Count == 1 ? $" - 1 volunteer required. " : $" - {gj.Count} volunteers required. ",
                    dueDateString: dueDateString,
                    showJobUrl: false,
                    jobUrl:string.Empty
                    ));
                ;
            }
            return requestJobs;
        }

        private List<RequestJob> GetJobs(GetRequestDetailsResponse response)
        {
            List<RequestJob> requestJobs = new List<RequestJob>();
            var groupJobs = response.RequestSummary.JobBasics
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
                    pendingApproval: additionalParameters["PendingApproval"] == true.ToString(),
                    groupName: group.Group.GroupName,
                    requestJobList: GetJobs(requestDetails)
                ),
                EmailToAddress = requestDetails.Requestor.EmailAddress,
                EmailToName = $"{requestDetails.Requestor.FirstName} {requestDetails.Requestor.LastName}",
                RecipientUserID = REQUESTOR_DUMMY_USERID,
                RequestID = requestId,
                ReferencedJobs = new List<ReferencedJob>()
                {
                    new ReferencedJob()
                    {
                        G = requestDetails.RequestSummary.ReferringGroupID,
                        R = requestId.Value
                    }
                }
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
