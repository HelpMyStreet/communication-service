using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
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

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.DailyDigests;
            }
        }

        public DailyDigestMessage(IConnectUserService connectUserService, IConnectRequestService connectRequestService)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            if (recipientUserId == null)
            {
                return null;
            }

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;
            var nationalSupportActivities = new List<SupportActivities>() { SupportActivities.FaceMask, SupportActivities.HomeworkSupport, SupportActivities.PhoneCalls_Anxious, SupportActivities.PhoneCalls_Friendly };

            var activitySpecificSupportDistancesInMiles = nationalSupportActivities.Where(a => user.SupportActivities.Contains(a)).ToDictionary(a => a, a => (double?)null);

            var jobRequest = new GetJobsByFilterRequest();
            var jobStatusRequest = new JobStatusRequest();
            jobStatusRequest.JobStatuses = new List<JobStatuses>() { JobStatuses.Done };
            jobRequest.Postcode = user.PostalCode;
            jobRequest.DistanceInMiles = 20; //needs connecting to Application Settings
            jobRequest.JobStatuses = jobStatusRequest;
            jobRequest.ActivitySpecificSupportDistancesInMiles = activitySpecificSupportDistancesInMiles;


            GetJobsByFilterResponse jobsResponse = _connectRequestService.GetJobsByFilter(jobRequest).Result;
            var jobs = jobsResponse.JobSummaries;

            if (!user.SupportActivities.Contains(SupportActivities.CommunityConnector))
            {
                jobs = jobs.Where(x => x.SupportActivity != SupportActivities.CommunityConnector).ToList();
            }

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
                byte[] jobIdBytes = System.Text.Encoding.UTF8.GetBytes(job.JobID.ToString());
                string encodedJobId = System.Convert.ToBase64String(jobIdBytes);
                chosenJobsList.Add(new DailyDigestDataJob(
                    Mapping.ActivityMappings[job.SupportActivity],
                    job.DueDate.ToString("dd/MM/yyyy"),
                    job.DueDate < DateTime.Now.AddDays(1),
                    job.IsHealthCritical,
                    1,
                    encodedJobId,
                    job.DistanceInMiles.ToString()
                    ));
            }


            var otherJobsList = new List<DailyDigestDataJob>();
            foreach (var job in otherJobsStats)
            {
                otherJobsList.Add(new DailyDigestDataJob(
                    Mapping.ActivityMappings[job.Key],
                    job.Min.ToString("dd/MM/yyyy"),
                    false,
                    false,
                    job.Count,
                    "",
                    ""
                    ));
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new DailyDigestData(
                    user.UserPersonalDetails.FirstName,
                    user.PostalCode,
                    criteriaJobs.Count(),
                    otherJobs.Count(),
                    chosenJobsList,
                    otherJobsList,
                    user.IsVerified.Value ? true : false
                    ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.DisplayName,
                RecipientUserID = recipientUserId.Value
            };
            ;
        }

        public Dictionary<int, string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            var volunteers = _connectUserService.GetUsers().Result;
            
            volunteers.UserDetails = volunteers.UserDetails.Where(x => x.SupportRadiusMiles.HasValue);

            Dictionary<int, string> recipients = new Dictionary<int, string>();
            foreach (var volunteer in volunteers.UserDetails) {
                recipients.Add(volunteer.UserID, TemplateName.DailyDigest);
            }
            return recipients;
        }
    }
}