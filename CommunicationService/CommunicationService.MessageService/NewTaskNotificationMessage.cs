using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public string GetTemplateId()
        {
            return "d-14a35071720e4a9fa0619c6891b3f108";
        }

        public List<int> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            List<int> recipients = new List<int>();
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            List<HelpMyStreet.Utils.Enums.SupportActivities> supportActivities = new List<HelpMyStreet.Utils.Enums.SupportActivities>();
            if (job != null)
            {
                supportActivities.Add(job.SupportActivity);
                var volunteers = _connectUserService.GetHelpersByPostcodeAndTaskType(job.PostCode, supportActivities, CancellationToken.None).Result;

                if (volunteers != null)
                {
                    recipients.AddRange(volunteers.Volunteers.Select(x => x.UserID).ToList());
                }
            }
            return recipients;
        }
        public async Task<SendGridData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if (user != null && job != null)
            {
                return new SendGridData()
                {
                    BaseDynamicData = new NewTaskNotification(recipientUserId.Value, user.UserPersonalDetails.FirstName, user.UserPersonalDetails.LastName, job.SupportActivity),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = user.UserPersonalDetails.DisplayName
                };
            }
            throw new Exception("unable to retrive user details");
        }
    }
}
