using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class NewTaskNotificationMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        public NewTaskNotificationMessage(IConnectUserService connectUserService, IConnectRequestService connectRequestService)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
        }

        public string TemplateId
        {
            get
            {
                return "d-14a35071720e4a9fa0619c6891b3f108";
            }
        }

        public string MessageId
        {
            get
            {
                return "2";
            }
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
                        recipients.Add(vs.UserID, TemplateId);
                    }
                }
            }
            return recipients;
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if (user != null && job != null)
            {
                return new EmailBuildData()
                {
                    BaseDynamicData = new NewTaskNotificationData(recipientUserId.Value, user.UserPersonalDetails.FirstName, user.UserPersonalDetails.LastName, job.SupportActivity),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = user.UserPersonalDetails.DisplayName,
                    RecipientUserID = recipientUserId.Value
                };
            }
            throw new Exception("unable to retrive user details");
        }
    }
}
