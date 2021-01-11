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

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            string encodedJobId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(job.JobSummary.JobID.ToString());
            bool isFaceMask = job.JobSummary.SupportActivity == SupportActivities.FaceMask;

            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var volunteers = _connectUserService.GetVolunteersByPostcodeAndActivity
                (
                    job.JobSummary.PostCode,
                    new List<SupportActivities>() { job.JobSummary.SupportActivity },
                    CancellationToken.None
                ).Result;

            if (volunteers != null)
            {

                var volunteer = volunteers.Volunteers.FirstOrDefault(x => x.UserID == user.ID);
                if (user != null && job != null)
                {
                    return new EmailBuildData()
                    {
                        BaseDynamicData = new TaskNotificationData
                        (
                            user.UserPersonalDetails.FirstName,
                            false,
                            encodedJobId,
                            job.JobSummary.SupportActivity.FriendlyNameShort(),
                            job.JobSummary.PostCode,
                            Math.Round(volunteer.DistanceInMiles, 1),
                            job.JobSummary.DueDate.ToString("dd/MM/yyyy"),
                            job.JobSummary.IsHealthCritical,
                            isFaceMask
                        ),
                        EmailToAddress = user.UserPersonalDetails.EmailAddress,
                        EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
                    };
                }

            }

            throw new Exception("unable to retrieve user details");
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {
            List<int> groupUsers = new List<int>();

            if(!groupId.HasValue || !jobId.HasValue)
            {
                throw new Exception($"GroupID or JobID is missing");
            }

            var groupMembers = await _connectGroupService.GetGroupMembers(groupId.Value);
            groupUsers = groupMembers.Users;
            
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            var strategy = await _connectGroupService.GetGroupNewRequestNotificationStrategy(job.JobSummary.ReferringGroupID);

            if(strategy==null)
            {
                throw new Exception($"No strategy for {job.JobSummary.ReferringGroupID}");
            }

            List<SupportActivities> supportActivities = new List<SupportActivities>();
            if (job != null)
            {
                supportActivities.Add(job.JobSummary.SupportActivity);
                var volunteers = await _connectUserService.GetVolunteersByPostcodeAndActivity(job.JobSummary.PostCode, supportActivities, CancellationToken.None);

                if (volunteers != null)
                {
                    volunteers.Volunteers
                        .Where(v => groupUsers.Contains(v.UserID))
                        .OrderBy(v => v.DistanceInMiles)
                        .Take(strategy.MaxVolunteer)
                        .ToList()
                        .ForEach(v =>
                            {
                                AddRecipientAndTemplate(TemplateName.TaskNotification, v.UserID, jobId, groupId);
                            });
                }
            }
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
