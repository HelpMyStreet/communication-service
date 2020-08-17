using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using CommunicationService.Core.Configuration;
using Microsoft.Extensions.Options;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Exceptions;
using System.Runtime.Caching;
using CommunicationService.Core.Domains.RequestService;
using CommunicationService.Core.Services;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Contracts.AddressService.Response;

namespace CommunicationService.MessageService
{
    public class DailyDigestMessage : IMessage
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IJobFilteringService _jobFilteringService;
        private readonly IOptions<EmailConfig> _emailConfig;
        private readonly IConnectAddressService _connectAddressService;
        private const string CACHEKEY_OPEN_REQUESTS = "openRequests";
        private const string CACHEKEY_OPEN_ADDRESS = "openAddresses";

        List<SendMessageRequest> _sendMessageRequests;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.DailyDigests;
            }
        }

        public DailyDigestMessage(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IConnectRequestService connectRequestService, IOptions<EmailConfig> eMailConfig, IJobFilteringService jobFilteringService, IConnectAddressService connectAddressService)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _emailConfig = eMailConfig;
            _jobFilteringService = jobFilteringService;
            _connectAddressService = connectAddressService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
            cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddHours(1.0);
            
            ObjectCache cache = MemoryCache.Default;

            if (recipientUserId == null)
            {
                throw new BadRequestException("recipientUserId is null");
            }

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;

            if (user == null)
            {
                throw new BadRequestException($"unable to retrieve user object for {recipientUserId.Value}");
            }

            var groups = _connectGroupService.GetUserGroups(recipientUserId.Value).Result;

            if(groups==null || groups.Groups==null || groups.Groups.Count ==0)
            {
                return null;
            }

            GetJobsByStatusesResponse openjobs;

            if (cache.Contains(CACHEKEY_OPEN_REQUESTS))
            {
                openjobs = (GetJobsByStatusesResponse)cache.Get(CACHEKEY_OPEN_REQUESTS);
            }
            else
            {
                openjobs = await _connectRequestService.GetJobsByStatuses(new GetJobsByStatusesRequest()
                {
                    JobStatuses = new JobStatusRequest()
                    {
                        JobStatuses = new List<JobStatuses>()
                        { JobStatuses.Open}
                    }
                });

                if (openjobs != null)
                {
                    cache.Add(CACHEKEY_OPEN_REQUESTS, openjobs, cacheItemPolicy);
                }
                else
                {
                    return null;
                }
            }

            List<PostcodeCoordinate> postcodeCoordinates = new List<PostcodeCoordinate>();

            if(cache.Contains(CACHEKEY_OPEN_ADDRESS))
            {
                postcodeCoordinates = (List<PostcodeCoordinate>)cache.Get(CACHEKEY_OPEN_ADDRESS);
            }
            else
            {
                var addresses = await _connectAddressService.GetPostcodeCoordinates(new HelpMyStreet.Contracts.AddressService.Request.GetPostcodeCoordinatesRequest()
                {
                    Postcodes = openjobs.JobSummaries.Select(x=>x.PostCode).Distinct().ToList()
                });

                if(addresses!=null)
                {
                    postcodeCoordinates = addresses.PostcodeCoordinates.ToList();
                    cache.Add(CACHEKEY_OPEN_ADDRESS, postcodeCoordinates, cacheItemPolicy);
                }
            }

            var userPostalCode = postcodeCoordinates.FirstOrDefault(x => x.Postcode == user.PostalCode);

            if (userPostalCode == null)
            {
                var userAddress = await _connectAddressService.GetPostcodeCoordinates(new HelpMyStreet.Contracts.AddressService.Request.GetPostcodeCoordinatesRequest()
                {
                    Postcodes = new List<string> { user.PostalCode }
                });

                if (userAddress != null)
                {
                    postcodeCoordinates.Add(userAddress.PostcodeCoordinates.First());
                }
            }

            var nationalSupportActivities = new List<SupportActivities>() { SupportActivities.FaceMask, SupportActivities.HomeworkSupport, SupportActivities.PhoneCalls_Anxious, SupportActivities.PhoneCalls_Friendly };

            var activitySpecificSupportDistancesInMiles = nationalSupportActivities.Where(a => user.SupportActivities.Contains(a)).ToDictionary(a => a, a => (double?)null);

            List<JobSummary> jobs = null;
            jobs = await _jobFilteringService.FilterJobSummaries(
                openjobs.JobSummaries,
                null,
                user.PostalCode,
                _emailConfig.Value.DigestOtherJobsDistance,
                activitySpecificSupportDistancesInMiles,
                null,
                groups.Groups,
                null,
                postcodeCoordinates
                );

            if (jobs.Count() == 0)
            {
                return null;
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
                    Math.Round(job.DistanceInMiles, 1).ToString()
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
                    job.Count == 1 ? true : false,
                    job.Count,
                    "",
                    ""
                    ));
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new DailyDigestData(_emailConfig.Value.ShowUserIDInEmailTitle ? recipientUserId.Value.ToString() : string.Empty,
                    user.UserPersonalDetails.FirstName,
                    criteriaJobs.Count() > 1 ? false : true,
                    criteriaJobs.Count(),
                    otherJobs.Count() > 0,
                    chosenJobsList,
                    otherJobsList,
                    user.IsVerified ?? false
                    ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.DisplayName
            };
        }

        public async  Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            var volunteers = await _connectUserService.GetUsers();
            
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