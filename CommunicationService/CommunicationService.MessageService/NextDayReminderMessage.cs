using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelpMyStreet.Contracts.RequestService.Response;
using System.Globalization;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Utils;
using HelpMyStreet.Utils.Helpers;
using HelpMyStreet.Utils.Models;
using HelpMyStreet.Utils.Exceptions;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.EqualityComparers;

namespace CommunicationService.MessageService
{
    public class NextDayReminderMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IConnectGroupService _connectGroupService;
        private readonly IOptions<SendGridConfig> _sendGridConfig;
        private IEqualityComparer<JobSummary> _jobSummaryDedupe_EqualityComparer;

        public const int REQUESTOR_DUMMY_USERID = -1;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        { 
            return UnsubscribeGroupName.NextDayReminder;
        }

        public NextDayReminderMessage(IConnectRequestService connectRequestService, 
            IConnectUserService connectUserService,
            ICosmosDbService cosmosDbService,
            IConnectGroupService connectGroupService,
            IOptions<SendGridConfig> sendGridConfig)
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            _connectGroupService = connectGroupService;
            _sendGridConfig = sendGridConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
            _jobSummaryDedupe_EqualityComparer = new JobBasicDedupe_EqualityComparer();
        }

        private string GetValueFromConfig(List<KeyValuePair<string, string>> groupEmailConfiguration, string key)
        {
            var result = groupEmailConfiguration.Where(x => x.Key == key).FirstOrDefault();

            if (!string.IsNullOrEmpty(result.Value))
            {
                return result.Value.Replace("{{BaseUrl}}", _sendGridConfig.Value.BaseUrl);
            }
            else
            {
                return string.Empty;
            }
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

            if (groups == null || groups.Groups == null || groups.Groups.Count == 0)
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
                },
                DateFrom = DateTime.Now.Date.AddDays(1),
                DateTo = DateTime.Now.Date.AddDays(2)
            });

            var openTasks = openRequests.JobSummaries.ToList();

            if (openTasks == null)
            {
                return null;
            }

            var chosenRequestTaskList = new List<NextDayJob>();
            var otherRequestTaskList = new List<NextDayJob>();
            List<JobSummary> criteriaRequestTasks = new List<JobSummary>();
            List<JobSummary> otherRequestTasks = new List<JobSummary>();

            if (openTasks.Count() > 0)
            {
                criteriaRequestTasks = openTasks
                    .Where(x => user.SupportActivities.Contains(x.SupportActivity) && x.DistanceInMiles <= user.SupportRadiusMiles)
                    .Distinct(_jobSummaryDedupe_EqualityComparer)
                    .ToList();

                otherRequestTasks = openTasks.Where(x => !criteriaRequestTasks.Contains(x, _jobSummaryDedupe_EqualityComparer)).ToList();
                var otherRequestTasksStats = otherRequestTasks.GroupBy(x => x.SupportActivity, x => x.DueDate, (activity, dueDate) => new { Key = activity, Count = dueDate.Count(), Min = dueDate.Min() });
                otherRequestTasksStats = otherRequestTasksStats.OrderByDescending(x => x.Count);

                foreach (var request in criteriaRequestTasks)
                {
                    string encodedRequestId = Base64Utils.Base64Encode(request.RequestID.ToString());

                    chosenRequestTaskList.Add(new NextDayJob(
                       activity: request.SupportActivity.FriendlyNameShort(),
                       postCode: request.PostCode.Split(" ").First(),
                       isSingleItem: true, //not used in the chosen task component of the email
                       count: 1, //not used in the chosen task component of the email
                       encodedRequestId: encodedRequestId,
                       distanceInMiles: Math.Round(request.DistanceInMiles, 1).ToString()
                    ));
                }

                foreach (var request in otherRequestTasksStats)
                {
                    otherRequestTaskList.Add(new NextDayJob(
                        activity: request.Key.FriendlyNameShort(),
                        postCode: string.Empty, //not used in the other task component of the email
                        isSingleItem: request.Count == 1 ? true : false,
                        count: request.Count,
                        encodedRequestId: "", //not used in the other task component of the email
                        distanceInMiles: "" //not used in the other task component of the email
                        ));
                }

                if (chosenRequestTaskList.Count > 0)
                {
                    return new EmailBuildData()
                    {
                        BaseDynamicData = new NextDayReminderData(
                        title: "Urgent - help is needed near you in the next 24 hours",
                        subject: "Urgent - help is needed near you in the next 24 hours",
                        firstName: user.UserPersonalDetails.FirstName,
                        otherRequestTasks: otherRequestTasks.Count() > 0,
                        chosenRequestTaskList: chosenRequestTaskList,
                        otherRequestTaskList: otherRequestTaskList
                        ),
                        EmailToAddress = user.UserPersonalDetails.EmailAddress,
                        EmailToName = user.UserPersonalDetails.DisplayName,
                    };
                }
                else
                {
                    return null;
                }
            }

            return null;


        }

            /*
            public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
            {
                var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;
                string encodedRequestID = Base64Utils.Base64Encode(job.RequestSummary.RequestID.ToString());
                var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
                additionalParameters.TryGetValue("Distance", out string strDistance);

                var group = _connectGroupService.GetGroup(job.RequestSummary.ReferringGroupID).Result;

                if (group == null)
                {
                    throw new BadRequestException($"unable to retrieve group details for {groupId.Value}");
                }

                var groupEmailConfiguration = _connectGroupService.GetGroupEmailConfiguration(groupId.Value, CommunicationJobTypes.GroupWelcome).Result;

                if (groupEmailConfiguration == null)
                {
                    throw new BadRequestException($"unable to retrieve group email configuration for {groupId.Value}");
                }

                var showGroupLogo = GetValueFromConfig(groupEmailConfiguration, "ShowGroupLogo");
                bool groupLogoAvailable = string.IsNullOrEmpty(showGroupLogo) ? false : Convert.ToBoolean(showGroupLogo);
                string groupLogo = string.Empty;

                if (groupLogoAvailable)
                {
                    groupLogo = $"group-logos/{group.Group.GroupKey}-partnership.png";
                }

                return new EmailBuildData()
                {
                    BaseDynamicData = new NextDayReminderData
                    (
                        title: "Urgent - help is needed near you in the next 24 hours",
                        subject: "Urgent - help is needed near you in the next 24 hours",
                        groupLogoAvailable: groupLogoAvailable,
                        groupLogo: groupLogo,
                        firstName: user.UserPersonalDetails.FirstName,
                        encodedRequestID: encodedRequestID,
                        supportActivity: job.JobSummary.SupportActivity.FriendlyNameShort().ToLower(),
                        distanceInMiles: strDistance

                    ),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}",
                    JobID = job.JobSummary.JobID,
                    RequestID = job.JobSummary.RequestID,
                    GroupID = job.JobSummary.ReferringGroupID,
                    RecipientUserID  = recipientUserId.Value
                };
            }
            */

            public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            var request = new GetAllJobsByFilterRequest()
            {
                JobStatuses = new JobStatusRequest()
                {
                    JobStatuses = new List<JobStatuses>()
                    { 
                        JobStatuses.Open
                    }
                }
                ,DateFrom = DateTime.Now.Date.AddDays(1)
                ,DateTo = DateTime.Now.Date.AddDays(2)
            };

            var requestsDueTomorrow = await _connectRequestService.GetAllJobsByFilter(request);

            if(requestsDueTomorrow.JobSummaries.Count()>0)
            {
                var volunteers = await _connectUserService.GetUsers();

                volunteers.UserDetails = volunteers.UserDetails.Where(x => x.SupportRadiusMiles.HasValue);

                Dictionary<int, string> recipients = new Dictionary<int, string>();
                foreach (var volunteer in volunteers.UserDetails)
                {
                    _sendMessageRequests.Add(new SendMessageRequest()
                    {
                        TemplateName = TemplateName.NextDayReminder,
                        RecipientUserID = volunteer.UserID,
                        GroupID = groupId,
                        JobID = jobId,
                        RequestID = requestId
                    });
                }

                //IEnumerable<VolunteerSummary> potentialVolunteers = new List<VolunteerSummary>();

                //requestsDueTomorrow.JobSummaries.ForEach(async job =>
                //{
                //    var volunteers = await _connectGroupService.GetEligibleVolunteersForRequest(job.ReferringGroupID, string.Empty, job.PostCode, job.SupportActivity);
                //    potentialVolunteers = potentialVolunteers.Concat(volunteers);
                //});

                //potentialVolunteers.Distinct().ToList();

                //potentialVolunteers.ToList()
                //    .ForEach(volunteer =>
                //    {
                //        _sendMessageRequests.Add(new SendMessageRequest()
                //        {
                //            TemplateName = TemplateName.NextDayReminder,
                //            RecipientUserID = volunteer.UserID,
                //            GroupID = null,
                //            JobID = null,
                //            RequestID = requestId
                //        });
                //    });

            }

            //GetJobDetailsResponse job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            //if (job == null)
            //{
            //    throw new Exception($"Job details cannot be retrieved for jobId {jobId}");
            //}

            //var availableVolunteers = await _connectGroupService.GetEligibleVolunteersForRequest(
            //    job.RequestSummary.ReferringGroupID,
            //    job.RequestSummary.Source,
            //    job.RequestSummary.PostCode,
            //    job.JobSummary.SupportActivity
            //    );

            //foreach (VolunteerSummary vs in availableVolunteers)
            //{
            //    if (!_cosmosDbService.EmailSent(TemplateName.NextDayReminder, jobId.Value, vs.UserID).Result)
            //    {
            //        _sendMessageRequests.Add(new SendMessageRequest()
            //        {
            //            TemplateName = TemplateName.NextDayReminder,
            //            RecipientUserID = vs.UserID,
            //            GroupID = job.RequestSummary.ReferringGroupID,
            //            JobID = jobId,
            //            RequestID = requestId,
            //            AdditionalParameters = new Dictionary<string, string>()
            //            {
            //                { "Distance", Math.Round(vs.DistanceInMiles,1).ToString() }
            //            }
            //        });
            //    }
            //}
         
            return _sendMessageRequests;
        }
    }
}
