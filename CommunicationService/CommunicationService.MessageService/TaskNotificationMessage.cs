using CommunicationService.Core.Configuration;
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
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class TaskNotificationMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectGroupService _connectGroupService;
        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.TaskNotification;
        }

        public TaskNotificationMessage(IConnectUserService connectUserService, IConnectRequestService connectRequestService, IConnectGroupService connectGroupService)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _connectGroupService = connectGroupService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var requestDetails = await _connectRequestService.GetRequestDetailsAsync(requestId.Value);
            string encodedRequestId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(requestDetails.RequestSummary.RequestID.ToString());
            SupportActivities supportActivity = GetSupportActivityFromRequest(requestDetails);            

            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var volunteers = _connectUserService.GetVolunteersByPostcodeAndActivity
                (
                    requestDetails.RequestSummary.PostCode,
                    new List<SupportActivities>() { supportActivity },
                    null,
                    CancellationToken.None
                ).Result;

            if (volunteers != null)
            {

                var volunteer = volunteers.Volunteers.FirstOrDefault(x => x.UserID == user.ID);
                if (user != null && requestDetails != null)
                {
                    var job = requestDetails.RequestSummary.JobSummaries.OrderBy(x=> x.DueDate).First();

                    string repeatMessage = string.Empty;
                    string dueDate = $"Due - {job.DueDate.FormatDate(DateTimeFormat.ShortDateFormat)}";

                    var groupJobs = requestDetails.RequestSummary.JobBasics
                        .GroupBy(x => new { x.SupportActivity, x.DueDate })
                        .Select(g => new GroupJob(g.Key.SupportActivity, g.Key.DueDate, g.Count()))
                        .ToList();

                    if(groupJobs.Count>1)
                    {
                        repeatMessage = $" required {groupJobs.Count} times";
                        dueDate = $"First Due - {job.DueDate.FormatDate(DateTimeFormat.ShortDateFormat)}";
                    }


                    return new EmailBuildData()
                    {
                        BaseDynamicData = new TaskNotificationData
                        (
                            firstname: user.UserPersonalDetails.FirstName,
                            isRequestor: false,
                            encodedRequestID: encodedRequestId,
                            activity: supportActivity.FriendlyNameShort(),
                            postcode: requestDetails.RequestSummary.PostCode.Split(" ").First(),
                            distanceFromPostcode: Math.Round(volunteer.DistanceInMiles, 1),
                            dueDate: dueDate,
                            isHealthCritical: job.IsHealthCritical,
                            isFaceMask: supportActivity == SupportActivities.FaceMask,
                            repeatMessage: repeatMessage
                        ),
                        EmailToAddress = user.UserPersonalDetails.EmailAddress,
                        EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}",
                        RequestID = job.RequestID,
                        ReferencedJobs = new List<ReferencedJob>()
                        {
                            new ReferencedJob()
                            {
                                G = job.ReferringGroupID,
                                R = job.RequestID,
                                J = job.JobID
                            }
                        }
                    };
                }

            }

            throw new Exception("unable to retrieve user details");
        }

        private SupportActivities GetSupportActivityFromRequest(GetRequestDetailsResponse request)
        {
            var activities = request.RequestSummary.JobBasics.Select(x => x.SupportActivity).Distinct();

            if(activities.Count()==1)
            {
                return activities.First();
            }
            else
            {
                throw new Exception("Unable to retrive distinct support activity from request");
            }
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            List<int> groupUsers = new List<int>();

            if(!groupId.HasValue || !requestId.HasValue)
            {
                throw new Exception($"GroupID or RequestID is missing");
            }

            var requestDetails = await _connectRequestService.GetRequestDetailsAsync(requestId.Value);

            if (requestDetails == null)
            {
                throw new Exception($"Unable to return request details for requestId {requestId.Value}");
            }

            var groupMembers = await _connectGroupService.GetGroupMembers(groupId.Value);
            groupUsers = groupMembers.Users;

            var strategy = await _connectGroupService.GetGroupNewRequestNotificationStrategy(requestDetails.RequestSummary.ReferringGroupID);

            if(strategy==null)
            {
                throw new Exception($"No strategy for {requestDetails.RequestSummary.ReferringGroupID}");
            }

            List<SupportActivities> supportActivities = new List<SupportActivities>();
            if (requestDetails != null)
            {
                supportActivities.Add(GetSupportActivityFromRequest(requestDetails));
                var volunteers = await _connectUserService.GetVolunteersByPostcodeAndActivity(requestDetails.RequestSummary.PostCode, supportActivities, null, CancellationToken.None);

                if (volunteers != null)
                {
                    volunteers.Volunteers
                        .Where(v => groupUsers.Contains(v.UserID))
                        .OrderBy(v => v.DistanceInMiles)
                        .Take(strategy.MaxVolunteer)
                        .ToList()
                        .ForEach(v =>
                            {
                                AddRecipientAndTemplate(TemplateName.TaskNotification, v.UserID, null, groupId, requestId, additionalParameters);
                            });
                }
            }
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
