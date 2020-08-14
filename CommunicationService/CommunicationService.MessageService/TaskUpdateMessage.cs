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
    public class TaskUpdateMessage : IMessage
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

        public TaskUpdateMessage(IConnectRequestService connectRequestService)
        {
            _connectRequestService = connectRequestService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            var britishZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
            DateTime datestatuschanged;
            datestatuschanged = TimeZoneInfo.ConvertTime(job.DateStatusLastChanged, TimeZoneInfo.Local, britishZone);
            var timeOfDay = datestatuschanged.ToString("t");
            timeOfDay = Regex.Replace(timeOfDay, @"\s+", "");
            var timeUpdated = $"today at {timeOfDay.ToLower()}";

            if ((DateTime.Now.Date - datestatuschanged.Date).TotalDays != 0)
            {
                timeUpdated = $"on {datestatuschanged.ToString("dd/MM/yyyy")} at {timeOfDay.ToLower()}";
            }

            string reference = string.Empty;

            var jobRequest = new GetJobsByFilterRequest();
            var jobStatusRequest = new JobStatusRequest();
            jobStatusRequest.JobStatuses = new List<JobStatuses>() { job.JobStatus };
            jobRequest.Postcode = job.PostCode;
            jobRequest.JobStatuses = jobStatusRequest;

            GetJobsByFilterResponse jobsResponse = await _connectRequestService.GetJobsByFilter(jobRequest);
            if(jobsResponse!=null && jobsResponse.JobSummaries.Count>0)
            {
                HelpMyStreet.Utils.Models.JobSummary summary = jobsResponse.JobSummaries.FirstOrDefault(x => x.JobID == job.JobID);
                int groupID_ageuk = -3;
                if(summary!=null && summary.ReferringGroupID == groupID_ageuk)
                {
                    var question = summary.Questions.FirstOrDefault(x => x.Name == "AgeUK Reference");

                    if(question!=null)
                    {
                        reference = question.Answer;
                    }
                }
            }


            bool isFaceMask = job.SupportActivity == SupportActivities.FaceMask;
            bool isOpen = job.JobStatus == JobStatuses.Open;
            bool isDone = job.JobStatus == JobStatuses.Done;
            bool isInProgress = job.JobStatus == JobStatuses.InProgress;
            return new EmailBuildData()
            {
                BaseDynamicData = new TaskUpdateData
                (
                job.Requestor.FirstName,
                "Request status updated",
                job.DateRequested.ToString("dd/MM/yyyy"),
                Mapping.ActivityMappings[job.SupportActivity],
                Mapping.StatusMappings[job.JobStatus],
                timeUpdated,
                isFaceMask,
                isDone,
                isOpen,
                isInProgress,
                job.ForRequestor,
                job.Recipient.FirstName,
                string.IsNullOrEmpty(reference) ? false : true,
                reference
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
                TemplateName = TemplateName.TaskUpdate,
                RecipientUserID = REQUESTOR_DUMMY_USERID,
                GroupID = groupId,
                JobID = jobId
            });
            
            return _sendMessageRequests;
        }
    }
}
