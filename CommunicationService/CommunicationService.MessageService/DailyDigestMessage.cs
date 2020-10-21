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
using CommunicationService.Core.Services;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Contracts.AddressService.Response;
using CommunicationService.Core.Interfaces.Repositories;
using System.Dynamic;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Utils.Extensions;

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
        private readonly ICosmosDbService _cosmosDbService;
        private const string CACHEKEY_OPEN_REQUESTS = "openRequests";
        private const string CACHEKEY_OPEN_ADDRESS = "openAddresses";

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientId)
        {

                return UnsubscribeGroupName.DailyDigests;
        }

        public DailyDigestMessage(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IConnectRequestService connectRequestService, IOptions<EmailConfig> eMailConfig, IJobFilteringService jobFilteringService, IConnectAddressService connectAddressService, ICosmosDbService cosmosDbService)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _emailConfig = eMailConfig;
            _jobFilteringService = jobFilteringService;
            _connectAddressService = connectAddressService;
            _cosmosDbService = cosmosDbService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, Dictionary<string, string> additionalParameters, string templateName)
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
                AddCacheDetailsToCosmos(batchId, "Open requests", true, recipientUserId, jobId, groupId, templateName);
                openjobs = (GetJobsByStatusesResponse)cache.Get(CACHEKEY_OPEN_REQUESTS);
            }
            else
            {
                AddCacheDetailsToCosmos(batchId, "Open requests", false, recipientUserId, jobId, groupId, templateName);
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
                AddCacheDetailsToCosmos(batchId, "postcode", true, recipientUserId, jobId, groupId, templateName);
                postcodeCoordinates = (List<PostcodeCoordinate>)cache.Get(CACHEKEY_OPEN_ADDRESS);
            }
            else
            {
                AddCacheDetailsToCosmos(batchId, "postcode", false, recipientUserId, jobId, groupId, templateName);
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

                if (userAddress != null && userAddress.PostcodeCoordinates.Count()>0)
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

            var criteriaJobs = jobs.Where(x => user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles <= user.SupportRadiusMiles);
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
                    job.SupportActivity.FriendlyNameShort(),
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
                    job.Key.FriendlyNameShort(),
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
                BaseDynamicData = new DailyDigestData(string.Empty,
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

        private void AddCacheDetailsToCosmos(Guid batchId, string cacheName, bool inCache, int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            try
            {
                dynamic message;

                message = new ExpandoObject();
                message.id = Guid.NewGuid();
                message.BatchId = batchId;
                message.CacheName = cacheName;
                message.InCache = inCache;
                message.RecipientUserID = recipientUserId;
                message.TemplateName = templateName;
                message.JobId = jobId;
                message.GroupId = groupId;
                _cosmosDbService.AddItemAsync(message);
            }
            catch (Exception exc)
            {
                string m = exc.ToString();
            }
        }
    }
}