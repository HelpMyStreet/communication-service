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
        private readonly ICosmosDbService _cosmosDbService;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? receipientId)
        {
                return UnsubscribeGroupName.TaskReminder;
        }

        public TaskReminderMessage(IConnectRequestService connectRequestService, IConnectUserService connectUserService, ICosmosDbService cosmosDbService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;

            _sendMessageRequests = new List<SendMessageRequest>();
        }

        private string GetTitleFromDays(int days, DueDateType dueDateType, DateTime dueDate)
        {
            if (days == 0)
            {
                return $"A request for help you accepted is due today ({dueDate.FormatDate(DateTimeFormat.ShortDateFormat)})";
            }
            else if (days == 1)
            {                    
                return $"A request for help you accepted is due tomorrow ({dueDate.FormatDate(DateTimeFormat.ShortDateFormat)})";
            }
            else
            {
                switch (dueDateType)
                {
                    case DueDateType.ASAP:
                        return $"A request for help you accepted is due as soon as possible";
                    case DueDateType.Before:
                        return $"A request for help you accepted is due within {days} days";
                    case DueDateType.On:
                        return $"A request for help you accepted is due in {days} days";
                    default:
                        throw new Exception("Unknown title");
                }
            }
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
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
                case DueDateType.ASAP:
                    dueDateMessage = $"The help is needed as soon as possible.";
                    break;
                case DueDateType.Before:
                    dueDateMessage = $"The help is needed on or before {job.JobSummary.DueDate.FormatDate(DateTimeFormat.ShortDateFormat)} – {job.JobSummary.DueDays} days from now.";
                    break;
                case DueDateType.On:
                    dueDateMessage = $"The help is needed on {job.JobSummary.DueDate.FormatDate(DateTimeFormat.ShortDateFormat)} – {job.JobSummary.DueDays} days from now.";
                    break;
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskReminderData(
                    encodedJobId,
                    GetTitleFromDays(job.JobSummary.DueDays, job.JobSummary.DueDateType, job.JobSummary.DueDate),
                    user.UserPersonalDetails.FirstName,
                    job.JobSummary.SupportActivity.FriendlyNameShort(),
                    job.JobSummary.PostCode,
                    job.JobSummary.DueDays == 0 ? true : false,
                    job.JobSummary.DueDays == 1 ? true : false,
                    job.JobSummary.DueDateType == DueDateType.Before || job.JobSummary.DueDateType == DueDateType.ASAP,
                    job.JobSummary.DateStatusLastChanged.FormatDate(DateTimeFormat.ShortDateFormat),
                    dueDateMessage,
                    dueDateString: $"({job.JobSummary.DueDate.FormatDate(DateTimeFormat.ShortDateFormat)})" 
                    ),
                JobID = jobId,
                GroupID = groupId,
                RecipientUserID = recipientUserId.Value,
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}",
                RequestID = job.JobSummary.RequestID,
                ReferencedJobs = new List<ReferencedJob>()
                {
                    new ReferencedJob()
                    {
                        G = job.JobSummary.ReferringGroupID,
                        R = job.JobSummary.RequestID,
                        J = job.JobSummary.JobID
                    }
                }
            };
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId, int? requestId)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                JobID = jobId,
                GroupID = groupId,
                RequestID = requestId
            });
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            var jobs = await _connectRequestService.GetJobsInProgress();

            if (jobs != null && jobs.JobSummaries.Count>0)
            {
                foreach(JobSummary summary in jobs.JobSummaries)
                {
                    switch(summary.DueDays, summary.DueDateType)
                    {
                        case (0, DueDateType.ASAP):
                        case (0, DueDateType.Before):
                            AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId, requestId);
                            break;
                        case (0, DueDateType.On):
                            if (!_cosmosDbService.EmailSent(TemplateName.TaskReminder, summary.JobID, summary.VolunteerUserID.Value).Result)
                            {
                                AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId, requestId);
                            }
                            break;
                        case (1, DueDateType.On):
                            AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId, requestId);
                            break;
                        case (3, DueDateType.ASAP):
                        case (7, DueDateType.ASAP):
                        case (3, DueDateType.Before):
                        case (7, DueDateType.Before):
                            if ((DateTime.Now - summary.DateStatusLastChanged).TotalHours > 24)
                            {
                                AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId, requestId);
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
