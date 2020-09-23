using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.GroupService.Response;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
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

        private const int REQUESTOR_DUMMY_USERID = -1;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.TaskNotification;
            }
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

            if (recipientUserId == REQUESTOR_DUMMY_USERID)
            {
                return new EmailBuildData()
                {
                    BaseDynamicData = new TaskNotificationData
                    (
                        job.Requestor.FirstName,
                        true,
                        encodedJobId,
                        Mapping.ActivityMappings[job.JobSummary.SupportActivity],
                        job.JobSummary.PostCode,
                        0,
                        job.JobSummary.DueDate.ToString("dd/MM/yyyy"),
                        false,
                        false,
                        job.JobSummary.IsHealthCritical,
                        isFaceMask
                    ),
                    EmailToAddress = job.Requestor.EmailAddress,
                    EmailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}",
                    RecipientUserID = REQUESTOR_DUMMY_USERID,
                };
            }
            else
            {
                var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
                var volunteers = _connectUserService.GetVolunteersByPostcodeAndActivity
                    (
                        job.JobSummary.PostCode,
                        new List<SupportActivities>() { job.JobSummary.SupportActivity },
                        CancellationToken.None
                    ).Result;

                bool isStreetChampionForGivenPostCode = false;

                if (volunteers != null)
                {

                    var volunteer = volunteers.Volunteers.FirstOrDefault(x => x.UserID == user.ID);
                    if (volunteer != null)
                    {
                        isStreetChampionForGivenPostCode = volunteer.IsStreetChampionForGivenPostCode.Value;
                    }
                    if (user != null && job != null)
                    {
                        return new EmailBuildData()
                        {
                            BaseDynamicData = new TaskNotificationData
                            (
                                user.UserPersonalDetails.FirstName,
                                false,
                                encodedJobId,
                                Mapping.ActivityMappings[job.JobSummary.SupportActivity],
                                job.JobSummary.PostCode,
                                Math.Round(volunteer.DistanceInMiles, 1),
                                job.JobSummary.DueDate.ToString("dd/MM/yyyy"),
                                user.IsVerified.HasValue ? !user.IsVerified.Value : true,
                                isStreetChampionForGivenPostCode,
                                job.JobSummary.IsHealthCritical,
                                isFaceMask
                            ),
                            EmailToAddress = user.UserPersonalDetails.EmailAddress,
                            EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
                        };
                    }

                }
            }

            throw new Exception("unable to retrieve user details");
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            List<int> groupUsers = new List<int>();

            if(!groupId.HasValue || !jobId.HasValue)
            {
                throw new Exception($"GroupID or JobID is missing");
            }

            var groupMembers = await _connectGroupService.GetGroupMembers(groupId.Value);
            groupUsers = groupMembers.Users;
            
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);
            List<SupportActivities> supportActivities = new List<SupportActivities>();
            if (job != null)
            {
                // Add dummy recipient to represent requestor, who will not necessarily exist within our DB and so has no userID to lookup/refer to
                AddRecipientAndTemplate(TemplateName.RequestorTaskNotification, REQUESTOR_DUMMY_USERID, jobId, groupId);
                // Continue
                supportActivities.Add(job.JobSummary.SupportActivity);
                var volunteers = await _connectUserService.GetVolunteersByPostcodeAndActivity(job.JobSummary.PostCode, supportActivities, CancellationToken.None);

                if (volunteers != null)
                {
                    foreach (VolunteerSummary vs in volunteers.Volunteers)
                    {
                        if (groupUsers.Contains(vs.UserID))
                        {
                            AddRecipientAndTemplate(TemplateName.TaskNotification, vs.UserID, jobId, groupId);
                        }   
                    }
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
