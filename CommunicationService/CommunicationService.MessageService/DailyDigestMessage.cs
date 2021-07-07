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
using System.IO;
using UserService.Core.Utils;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using HelpMyStreet.Utils.EqualityComparers;

namespace CommunicationService.MessageService
{
    public class DailyDigestMessage : IMessage
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;
        private readonly IOptions<EmailConfig> _emailConfig;
        private readonly IConnectAddressService _connectAddressService;
        private readonly ICosmosDbService _cosmosDbService;
        private const string CACHEKEY_OPEN_REQUESTS = "openRequests";
        private const string CACHEKEY_OPEN_ADDRESS = "openAddresses";
        private IEqualityComparer<JobSummary> _jobSummaryDedupe_EqualityComparer;
        private IEqualityComparer<ShiftJob> _shiftJobDedupe_EqualityComparer;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientId)
        {
            return UnsubscribeGroupName.DailyDigests;
        }

        public DailyDigestMessage(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IConnectRequestService connectRequestService, IOptions<EmailConfig> eMailConfig, IConnectAddressService connectAddressService, ICosmosDbService cosmosDbService)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
            _emailConfig = eMailConfig;
            _connectAddressService = connectAddressService;
            _cosmosDbService = cosmosDbService;
            _sendMessageRequests = new List<SendMessageRequest>();
            _jobSummaryDedupe_EqualityComparer = new JobBasicDedupe_EqualityComparer();
            _shiftJobDedupe_EqualityComparer = new JobBasicDedupe_EqualityComparer();
        }


        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
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

            GetAllJobsByFilterResponse openRequests;
            openRequests = await _connectRequestService.GetAllJobsByFilter(new GetAllJobsByFilterRequest()
            {
                JobStatuses = new JobStatusRequest()
                {
                    JobStatuses = new List<JobStatuses>()
                    { JobStatuses.Open}
                },
                Postcode = user.PostalCode,
                ExcludeSiblingsOfJobsAllocatedToUserID = recipientUserId,
                Groups = new GroupRequest()
                {
                    Groups = groups.Groups
                }
            });

                
            var openTasks = openRequests.JobSummaries.ToList();
            var openShifts = openRequests.ShiftJobs.Where(x=> user.SupportActivities.Contains(x.SupportActivity)).ToList();
            
            if((openTasks == null || openTasks.Count==0) && (openShifts ==null || openShifts.Count==0 ) )
            {
                return null;
            }

            var chosenRequestTaskList = new List<DailyDigestDataJob>();
            var otherRequestTaskList = new List<DailyDigestDataJob>();
            var shiftItemList = new List<ShiftItem>();
            List<JobSummary> criteriaRequestTasks = new List<JobSummary>();
            List<JobSummary> otherRequestTasks = new List<JobSummary>();


            if (openTasks.Count() > 0)
            {
                criteriaRequestTasks = openTasks
                    .Where(x => user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles <= user.SupportRadiusMiles)
                    .Distinct(_jobSummaryDedupe_EqualityComparer)
                    .ToList();

                criteriaRequestTasks = criteriaRequestTasks.OrderBy(x => x.DueDate).ToList();

                otherRequestTasks = openTasks.Where(x => !criteriaRequestTasks.Contains(x, _jobSummaryDedupe_EqualityComparer)).ToList();
                var otherRequestTasksStats = otherRequestTasks.GroupBy(x => x.SupportActivity, x => x.DueDate, (activity, dueDate) => new { Key = activity, Count = dueDate.Count(), Min = dueDate.Min() });
                otherRequestTasksStats = otherRequestTasksStats.OrderByDescending(x => x.Count);

                foreach (var request in criteriaRequestTasks)
                {
                    string encodedRequestId = HelpMyStreet.Utils.Utils.Base64Utils.Base64Encode(request.RequestID.ToString());

                    chosenRequestTaskList.Add(new DailyDigestDataJob(
                       activity: request.SupportActivity.FriendlyNameShort(),
                       postCode: request.PostCode,
                       dueDate: request.DueDate.FormatDate(DateTimeFormat.ShortDateFormat),
                       soon: request.DueDate < DateTime.Now.AddDays(1),
                       urgent: request.IsHealthCritical,
                       isSingleItem: true, //not used in the chosen task component of the email
                       count: 1, //not used in the chosen task component of the email
                       encodedRequestId: encodedRequestId,
                       distanceInMiles: Math.Round(request.DistanceInMiles, 1).ToString()
                    ));
                }

                foreach (var request in otherRequestTasksStats)
                {
                    otherRequestTaskList.Add(new DailyDigestDataJob(
                        activity: request.Key.FriendlyNameShort(),
                        postCode: string.Empty, //not used in the other task component of the email
                        dueDate: request.Min.FormatDate(DateTimeFormat.ShortDateFormat),
                        soon: false, //not used in the other task component of the email
                        urgent: false, //not used in the other task component of the email
                        isSingleItem: request.Count == 1 ? true : false,
                        count: request.Count,
                        encodedRequestId: "", //not used in the other task component of the email
                        distanceInMiles: "" //not used in the other task component of the email
                        ));
                }
            }

            if (openShifts?.Count > 0)
            {
                var requests = openShifts
                    .Distinct(_shiftJobDedupe_EqualityComparer)
                    .ToList();

                foreach (var shift in requests)
                {
                    var location = await _connectAddressService.GetLocationDetails(shift.Location, CancellationToken.None);
                    string shiftDate = shift.StartDate.FormatDate(DateTimeFormat.LongDateTimeFormat) + " - " + shift.EndDate.FormatDate(DateTimeFormat.TimeFormat);
                    shiftItemList.Add(new ShiftItem($"<strong>{ shift.SupportActivity.FriendlyNameShort() }</strong> " +
                        $"at {location.Name} " +
                        $"( {Math.Round(shift.DistanceInMiles, 2)} miles away) " +
                        $"- {shiftDate}"));
                }
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new DailyDigestData(
                    title: string.Empty,
                    firstName: user.UserPersonalDetails.FirstName,
                    chosenRequestTasks: criteriaRequestTasks.Count(),
                    otherRequestTasks: otherRequestTasks.Count() > 0,
                    shiftsAvailable: shiftItemList.Count >0,
                    shiftCount: shiftItemList.Count,
                    chosenRequestTaskList: chosenRequestTaskList,
                    otherRequestTaskList: otherRequestTaskList,
                    shiftItemList: shiftItemList
                    ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = user.UserPersonalDetails.DisplayName,
                ReferencedJobs = GetReferencedJobs(criteriaRequestTasks, otherRequestTasks, openShifts),
            };
        }

        private List<ReferencedJob> GetReferencedJobs(List<JobSummary> criteriaJobs, List<JobSummary> otherJobs, List<ShiftJob> shiftJobs)
        {
            var concatedList = criteriaJobs.Select(x => x.RequestID)
                .Concat(otherJobs.Select(x => x.RequestID))
                .Concat(shiftJobs.Select(x => x.RequestID));

            List<ReferencedJob> jobs = new List<ReferencedJob>();
            concatedList.Select(x => x).Distinct()
                    .ToList()
                    .ForEach(request =>
                    jobs.Add(new ReferencedJob()
                    {
                        R = request
                    }
                    ));

            return jobs;
        }
        public async  Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
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
                    JobID = jobId,
                    RequestID = requestId
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