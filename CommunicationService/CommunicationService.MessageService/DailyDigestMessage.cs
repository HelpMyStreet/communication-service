using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using CommunicationService.Core.Configuration;
using HelpMyStreet.Contracts.UserService.Response;
using Microsoft.Extensions.Options;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class DailyDigestMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IOptions<EmailConfig> _emailConfig;
        List<SendMessageRequest> _sendMessageRequests;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.DailyDigests;
            }
        }

        public DailyDigestMessage(IConnectUserService connectUserService, IConnectRequestService connectRequestService, IOptions<EmailConfig> eMailConfig)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _emailConfig = eMailConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            if (recipientUserId == null)
            {
                throw new Exception("recipientUserId is null");
            }

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;
            var nationalSupportActivities = new List<SupportActivities>() { SupportActivities.FaceMask, SupportActivities.HomeworkSupport, SupportActivities.PhoneCalls_Anxious, SupportActivities.PhoneCalls_Friendly };

            var activitySpecificSupportDistancesInMiles = nationalSupportActivities.Where(a => user.SupportActivities.Contains(a)).ToDictionary(a => a, a => (double?)null);

            var jobRequest = new GetJobsByFilterRequest();
            var jobStatusRequest = new JobStatusRequest();
            jobStatusRequest.JobStatuses = new List<JobStatuses>() { JobStatuses.Open };
            jobRequest.Postcode = user.PostalCode;
            jobRequest.DistanceInMiles = _emailConfig.Value.DigestOtherJobsDistance;
            jobRequest.JobStatuses = jobStatusRequest;
            jobRequest.ActivitySpecificSupportDistancesInMiles = activitySpecificSupportDistancesInMiles;


            GetJobsByFilterResponse jobsResponse = _connectRequestService.GetJobsByFilter(jobRequest).Result;
            var jobs = jobsResponse.JobSummaries;

            var criteriaJobs = jobs.Where(x => user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles < user.SupportRadiusMiles);
            if (criteriaJobs.Count() == 0)
            {
                return null;
            }

            criteriaJobs = criteriaJobs.OrderBy(x => x.DueDate);

            var otherJobs = jobs.Where(x => !criteriaJobs.Contains(x));
            var otherJobsStats = otherJobs.GroupBy(x => x.SupportActivity, x => x.DueDate, (activity, dueDate) => new { Key = activity, Count = dueDate.Count(), Min = dueDate.Min() });
            otherJobsStats = otherJobsStats.OrderByDescending(x => x.Count);


            var chosenJobsList = new List<DailyDigestDataJob>();

            foreach (var job in criteriaJobs)
            {
                string encodedJobId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(job.JobID.ToString());

                chosenJobsList.Add(new DailyDigestDataJob(
                    Mapping.ActivityMappings[job.SupportActivity],
                    job.PostCode,
                    job.DueDate.ToString("dd/MM/yyyy"),
                    job.DueDate < DateTime.Now.AddDays(1),
                    job.IsHealthCritical,
                    true,
                    1,
                    encodedJobId,
                    Math.Round(job.DistanceInMiles,1).ToString()
                    ));
            }


            var otherJobsList = new List<DailyDigestDataJob>();
            foreach (var job in otherJobsStats)
            {
                otherJobsList.Add(new DailyDigestDataJob(
                    Mapping.ActivityMappings[job.Key],
                    string.Empty,
                    job.Min.ToString("dd/MM/yyyy"),
                    false,
                    false,
                    job.Count==1 ? true : false,
                    job.Count,
                    "",
                    ""
                    ));
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new DailyDigestData(_emailConfig.Value.ShowUserIDInEmailTitle ? recipientUserId.Value.ToString() : string.Empty,
                    user.UserPersonalDetails.FirstName,
                    criteriaJobs.Count() >1 ? false: true,
                    criteriaJobs.Count(),
                    otherJobs.Count() > 0,
                    chosenJobsList,
                    otherJobsList,
                    user.IsVerified ?? false
                    ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.DisplayName,
                RecipientUserID = recipientUserId.Value
            };
            ;
        }

        public List<SendMessageRequest> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            var volunteers = _connectUserService.GetUsers().Result;
            
            volunteers.UserDetails = volunteers.UserDetails.Where(x => x.SupportRadiusMiles.HasValue);

            Dictionary<int, string> recipients = new Dictionary<int, string>();
            foreach (var volunteer in volunteers.UserDetails) {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = TemplateName.DailyDigest,
                    RecipientUserID = volunteer.UserID,
                    GroupID = groupId,
                    JobID = jobId
                });
            }
            return _sendMessageRequests;
        }
    }
}