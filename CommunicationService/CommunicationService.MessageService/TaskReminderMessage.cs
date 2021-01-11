using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Models;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Enums;

namespace CommunicationService.MessageService
{
    public class TaskReminderMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private const string DATE_FORMAT = "dddd, dd MMMM";

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? receipientId)
        {
                return UnsubscribeGroupName.TaskReminder;
        }

        public TaskReminderMessage(IConnectRequestService connectRequestService, IConnectUserService connectUserService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        private string GetTitleFromDays(int days, DueDateType dueDateType)
        {
            if (days == 0)
            {
                return $"A request for help you accepted is due today";
            }
            else
            {
                switch (dueDateType)
                {
                    case DueDateType.Before:
                        return $"A request for help you accepted is due within {days} days";
                    case DueDateType.On:
                        return $"A request for help you accepted is due in {days} days";
                    default:
                        throw new Exception("Unknown title");
                }
            }
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
        {
            if (!recipientUserId.HasValue || !jobId.HasValue)
            {
                throw new Exception($"Recipient or JobID is missing");
            }
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            string encodedJobId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(job.JobSummary.JobID.ToString());

            string dueDateMessage = string.Empty;

            switch (job.JobSummary.DueDateType)
            {
                case DueDateType.Before:
                    dueDateMessage = $"The help is needed on or before {job.JobSummary.DueDate.ToString(DATE_FORMAT)} – {job.JobSummary.DueDays} days from now.";
                    break;
                case DueDateType.On:
                    dueDateMessage = $"The help is needed on {job.JobSummary.DueDate.ToString(DATE_FORMAT)} – {job.JobSummary.DueDays} days from now.";
                    break;
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskReminderData(
                    encodedJobId,
                    GetTitleFromDays(job.JobSummary.DueDays, job.JobSummary.DueDateType),
                    user.UserPersonalDetails.FirstName,
                    job.JobSummary.SupportActivity.FriendlyNameShort(),
                    job.JobSummary.PostCode,
                    job.JobSummary.DueDays == 0 ? true : false,
                    job.JobSummary.DueDays == 1 ? true : false,
                    job.JobSummary.DueDateType == DueDateType.Before,
                    job.JobSummary.DateStatusLastChanged.ToString(DATE_FORMAT),
                    dueDateMessage
                    ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
            };
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                JobID = jobId,
                GroupID = groupId
            });
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters)
        {
            var jobs = await _connectRequestService.GetJobsInProgress();

            if (jobs != null && jobs.JobSummaries.Count>0)
            {
                foreach(JobSummary summary in jobs.JobSummaries)
                {
                    switch(summary.DueDays, summary.DueDateType)
                    {
                        case (0, DueDateType.Before):
                            AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId);
                            break;
                        case (1, DueDateType.On):
                            AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId);
                            break;
                        case (3, DueDateType.Before):
                        case (7, DueDateType.Before):
                            if ((DateTime.Now - summary.DateStatusLastChanged).TotalHours > 24)
                            {
                                AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return _sendMessageRequests;
        }
    }
}
