using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class NewTaskNotificationMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private const string TEMPLATENAME = "TaskNotification";

        public string UnsubscriptionGroupName
        {
            get
            {
                return "IncompleteRegistration";
            }
        }

        public NewTaskNotificationMessage(IConnectUserService connectUserService, IConnectRequestService connectRequestService)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
        }

        public Dictionary<int,string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int,string> recipients = new Dictionary<int, string>();
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            List<HelpMyStreet.Utils.Enums.SupportActivities> supportActivities = new List<HelpMyStreet.Utils.Enums.SupportActivities>();
            if (job != null)
            {
                supportActivities.Add(job.SupportActivity);
                var volunteers = _connectUserService.GetHelpersByPostcodeAndTaskType(job.PostCode, supportActivities, CancellationToken.None).Result;

                if (volunteers != null)
                {
                    foreach(VolunteerSummary vs in volunteers.Volunteers)
                    {
                        recipients.Add(vs.UserID, TEMPLATENAME);
                        return recipients;
                    }
                }
            }
            return recipients;
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var volunteers = _connectUserService.GetHelpersByPostcodeAndTaskType
                (
                    job.PostCode,
                    new List<HelpMyStreet.Utils.Enums.SupportActivities>() { job.SupportActivity },
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
                        BaseDynamicData = new NewTaskNotificationData
                        (
                            Mapping.ActivityMappings[job.SupportActivity],
                            job.PostCode,
                            volunteer.DistanceInMiles,
                            job.DueDate.ToString("dd/MM/yyyy"),
                            user.IsVerified.HasValue ? !user.IsVerified.Value : false,
                            isStreetChampionForGivenPostCode,
                            job.HealthCritical
                        ),
                        EmailToAddress = user.UserPersonalDetails.EmailAddress,
                        EmailToName = user.UserPersonalDetails.DisplayName,
                        RecipientUserID = recipientUserId.Value
                    };
                }
            }
           
            throw new Exception("unable to retrive user details");
        }
    }
}
