using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Contracts.RequestService.Response;

namespace CommunicationService.MessageService
{
    public class TaskUpdateNewMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private const int REQUESTOR_DUMMY_USERID = -1;

        List<SendMessageRequest> _sendMessageRequests;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.TaskNotification;
            }
        }

        public TaskUpdateNewMessage(IConnectRequestService connectRequestService)
        {
            _connectRequestService = connectRequestService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;

            DateTime datestatuschanged;
            datestatuschanged = TimeZoneInfo.ConvertTime(job.JobSummary.DateStatusLastChanged, TimeZoneInfo.Local, britishZone);
            var timeOfDay = datestatuschanged.ToString("t");
            timeOfDay = Regex.Replace(timeOfDay, @"\s+", "");
            var timeUpdated = $"today at {timeOfDay.ToLower()}";

            if ((DateTime.Now.Date - datestatuschanged.Date).TotalDays != 0)
            {
                timeUpdated = $"on {datestatuschanged.ToString("dd/MM/yyyy")} at {timeOfDay.ToLower()}";
            }

            int groupID_ageuk = -3;
            string ageUKReference = string.Empty;
            if (job.JobSummary.ReferringGroupID == groupID_ageuk)
            {
                var question = job.JobSummary.Questions.FirstOrDefault(x => x.Id == (int)Questions.AgeUKReference);

                if (question != null)
                {
                    ageUKReference = question.Answer;
                }
            }

            string one = "Your";
            string two = "";
            string three = "accepted";
            string four = " volunteer";
            string five = job.Requestor.FirstName;
            string six = $" for {job.Recipient.FirstName} in {job.Recipient.Address.Locality}";
            string seven = Mapping.ActivityMappings[job.JobSummary.SupportActivity];
            string eight = ageUKReference;
            string nine = "you made";
            string ten = job.JobSummary.DateRequested.ToString("dd/MM/yyyy");
            string eleven = datestatuschanged.ToString("dd/MM/yyyy");
            string twelve = timeOfDay.ToLower();
            string thirteen = "This only usually  happens if they think that the help is no longer needed, or is not possible to deliver.";
            bool thirteensupplied = thirteen.Length > 0 ? true : false;
            string fourteen = "If you think that this has been done in error";

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskUpdateNewData
                (
                one,
                two,
                three,
                four,
                five,
                six,
                seven,
                eight,
                nine,
                ten,
                eleven,
                twelve,
                thirteen,
                thirteensupplied,
                fourteen
                ),
                EmailToAddress = job.Requestor.EmailAddress,
                EmailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}"
            };
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.TaskUpdateNew,
                RecipientUserID = REQUESTOR_DUMMY_USERID,
                GroupID = groupId,
                JobID = jobId
            });

            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.TaskUpdateNew,
                RecipientUserID = REQUESTOR_DUMMY_USERID,
                GroupID = groupId,
                JobID = jobId
            });

            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = TemplateName.TaskUpdateNew,
                RecipientUserID = REQUESTOR_DUMMY_USERID,
                GroupID = groupId,
                JobID = jobId
            });

            return _sendMessageRequests;
        }
    }
}
