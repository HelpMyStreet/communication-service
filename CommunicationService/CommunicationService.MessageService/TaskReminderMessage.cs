using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Models;
using CommunicationService.MessageService.Substitution;

namespace CommunicationService.MessageService
{
    public class TaskReminderMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;

        List<SendMessageRequest> _sendMessageRequests;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.TaskReminder;
            }
        }

        public TaskReminderMessage(IConnectRequestService connectRequestService, IConnectUserService connectUserService)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        private string GetTitleFromDays(int days)
        {
            if (days == 0)
            {
                return $"A request for help you accepted is due today";
            }
            else
            {
                return $"A request for help you accepted is due within {days} days";
            }
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            if (!recipientUserId.HasValue || !jobId.HasValue)
            {
                throw new Exception($"Recipient or JobID is missing");
            }
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            string encodedJobId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(job.JobSummary.JobID.ToString());

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskReminderData(
                    encodedJobId,
                    GetTitleFromDays(job.JobSummary.DueDays),
                    user.UserPersonalDetails.FirstName,
                    Mapping.ActivityMappings[job.JobSummary.SupportActivity],
                    job.JobSummary.PostCode,
                    job.JobSummary.DueDays,
                    job.JobSummary.DueDate.ToString("dd/MM/yyyy"),
                    job.JobSummary.DueDays == 0 ? true : false,
                    job.JobSummary.DateStatusLastChanged.ToString("dd/MM/yyyy")
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

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            var jobs = await _connectRequestService.GetJobsInProgress();

            if (jobs != null && jobs.JobSummaries.Count>0)
            {
                foreach(JobSummary summary in jobs.JobSummaries)
                {
                    switch(summary.DueDays)
                    {
                        case 0:
                            AddRecipientAndTemplate(TemplateName.TaskReminder, summary.VolunteerUserID.Value, summary.JobID, groupId);
                            break;
                        case 3:
                        case 7:
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
