using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
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

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.TaskNotification;
            }
        }

        public TaskNotificationMessage(IConnectUserService connectUserService, IConnectRequestService connectRequestService)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            byte[] jobIdBytes = System.Text.Encoding.UTF8.GetBytes(job.JobID.ToString());
            string encodedJobId = System.Convert.ToBase64String(jobIdBytes);
            bool isFaceMask = job.SupportActivity == SupportActivities.FaceMask;

            if (recipientUserId < 0)
            {
                return new EmailBuildData()
                {
                    BaseDynamicData = new TaskNotificationData
                    (
                        true,
                        encodedJobId,
                        Mapping.ActivityMappings[job.SupportActivity],
                        job.PostCode,
                        0,
                        job.DueDate.ToString("dd/MM/yyyy"),
                        false,
                        false,
                        job.HealthCritical,
                        isFaceMask
                    ),
                    EmailToAddress = job.Requestor.EmailAddress,
                    EmailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}",
                    RecipientUserID = -1,
                };
            }

            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var volunteers = _connectUserService.GetHelpersByPostcodeAndTaskType
                (
                    job.PostCode,
                    new List<SupportActivities>() { job.SupportActivity },
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
                            false,
                            encodedJobId,
                            Mapping.ActivityMappings[job.SupportActivity],
                            job.PostCode,
                            volunteer.DistanceInMiles,
                            job.DueDate.ToString("dd/MM/yyyy"),
                            user.IsVerified.HasValue ? !user.IsVerified.Value : false,
                            isStreetChampionForGivenPostCode,
                            job.HealthCritical,
                            isFaceMask
                        ),
                        EmailToAddress = user.UserPersonalDetails.EmailAddress,
                        EmailToName = user.UserPersonalDetails.DisplayName,
                        RecipientUserID = recipientUserId.Value
                    };
                }
                
            }

            throw new Exception("unable to retrieve user details");
        }

        public Dictionary<int, string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int, string> recipients = new Dictionary<int, string>();
            
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            List<SupportActivities> supportActivities = new List<SupportActivities>();
            if (job != null)
            {
                // Add dummy recipient to represent requestor, who will not necessarily exist within our DB and so has no userID to lookup/refer to
                recipients.Add(-1, TemplateName.TaskNotification);
                // Continue
                supportActivities.Add(job.SupportActivity);
                var volunteers = _connectUserService.GetHelpersByPostcodeAndTaskType(job.PostCode, supportActivities, CancellationToken.None).Result;

                if (volunteers != null)
                {
                    foreach (VolunteerSummary vs in volunteers.Volunteers)
                    {
                        recipients.Add(vs.UserID, TemplateName.TaskNotification);
                        return recipients;
                    }
                }
            }
            return recipients;
        }
    }
}
