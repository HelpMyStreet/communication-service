using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.EqualityComparers;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class NewRequestNotificationMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectAddressService _connectAddressService;
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IOptions<EmailConfig> _emailConfig;
        private readonly IConnectGroupService _connectGroupService;
        private IEqualityComparer<ShiftJob> _shiftJobDedupe_EqualityComparer;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.TaskNotification;
        }

        public NewRequestNotificationMessage(IConnectRequestService connectRequestService,
            IConnectAddressService connectAddressService,
            IConnectUserService connectUserService,
            ICosmosDbService cosmosDbService,
            IOptions<EmailConfig> emailConfig,
            IConnectGroupService connectGroupService)
        {
            _connectRequestService = connectRequestService;
            _connectAddressService = connectAddressService;
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            _emailConfig = emailConfig;
            _connectGroupService = connectGroupService;
            _shiftJobDedupe_EqualityComparer = new JobBasicDedupe_EqualityComparer();
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            GetOpenShiftJobsByFilterRequest request = new GetOpenShiftJobsByFilterRequest();
            var shifts = await _connectRequestService.GetOpenShiftJobsByFilter(request);

            List<Tuple<int, Location>> usersToBeNotified = new List<Tuple<int, Location>>();

            if (shifts != null)
            {
                var locationsSupportActivities = shifts.GroupBy(d => new { d.Location, d.SupportActivity })
                    .Select(m => new { m.Key.Location, m.Key.SupportActivity });

                if (locationsSupportActivities != null && locationsSupportActivities.Count() > 0)
                {
                    foreach (var x in locationsSupportActivities)
                    {
                        var locations = await _connectAddressService.GetLocationDetails(x.Location);

                        if (locations != null)
                        {
                            var users = await _connectUserService.GetVolunteersByPostcodeAndActivity(
                                locations.LocationDetails.Address.Postcode,
                                new List<SupportActivities>() { x.SupportActivity },
                                _emailConfig.Value.OpenRequestRadius,
                                CancellationToken.None);

                            if (users != null && users.Volunteers.Count() > 0)
                            {
                                usersToBeNotified.AddRange(users.Volunteers.Select(i => new Tuple<int, Location>(i.UserID, x.Location)).ToList());
                            }
                        }
                    }
                }

                if (usersToBeNotified.Count > 0)
                {
                    foreach (var userId in usersToBeNotified.GroupBy(g => g.Item1).Select(m => m.Key).ToList())
                    {
                        List<Location> locations = usersToBeNotified.Where(x => x.Item1 == userId).Select(m => m.Item2).ToList();
                        string parameter = string.Join(",", locations.Cast<int>().ToArray());
                        Dictionary<string, string> locationParameters = new Dictionary<string, string>();
                        locationParameters.Add("locations", parameter);
                        AddRecipientAndTemplate(TemplateName.RequestNotification, userId, null, null, null, locationParameters);
                    }
                }

            }

            return _sendMessageRequests;
        }

        private async Task<List<ShiftJob>> GetShiftsForUser(int userId)
        {
            GetUserShiftJobsByFilterRequest getUserShiftJobsByFilterRequest = new GetUserShiftJobsByFilterRequest()
            {
                VolunteerUserId = userId,
                JobStatusRequest = new JobStatusRequest() { JobStatuses = new List<JobStatuses>() { JobStatuses.Accepted, JobStatuses.InProgress, JobStatuses.Done } }
            };
            var response = await _connectRequestService.GetUserShiftJobsByFilter(getUserShiftJobsByFilterRequest);
            
            if (response != null)
            {
                return response;
            }
            else
            {
                return new List<ShiftJob>();
            }
        }

        private async Task<List<ShiftJob>> GetOpenShiftsForUser(int userId, string locations)
        {
            LocationsRequest lr = new LocationsRequest() { Locations = new List<Location>() };

            locations.Split(",").ToList()
                .ForEach(x =>
                {
                    lr.Locations.Add((Location)Enum.Parse(typeof(Location), x));
                });

            var userGroups = await _connectGroupService.GetUserGroups(userId);
            List<int> groups = new List<int>();
            if (userGroups != null)
            {
                groups = userGroups.Groups;
            }

            GetOpenShiftJobsByFilterRequest getOpenShiftJobsByFilterRequest = new GetOpenShiftJobsByFilterRequest()
            {
                Locations = lr,
                SupportActivities = new SupportActivityRequest { SupportActivities = new List<SupportActivities>() },
                Groups = new GroupRequest { Groups = groups },
            };

            var allShifts = await _connectRequestService.GetOpenShiftJobsByFilter(getOpenShiftJobsByFilterRequest);
            
            if (allShifts == null)
            {
                throw new Exception($"No shifts returned from user id {userId}");
            }
            
            var dedupedShifts = allShifts.Distinct(_shiftJobDedupe_EqualityComparer);
            var userShifts = await GetShiftsForUser(userId);            
            var notMyShifts = dedupedShifts.Where(s => !userShifts.Contains(s, _shiftJobDedupe_EqualityComparer)).ToList();

            return notMyShifts;
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if (user == null)
            {
                throw new Exception($"Unable to retrieve user details for userid{ recipientUserId.Value }");
            }

            if (!additionalParameters.TryGetValue("locations", out string locations))
            {
                throw new Exception($"Location parameter expected for {recipientUserId.Value}");
            }

            var openShifts = await GetOpenShiftsForUser(user.ID, locations);

            List<int> requestsAlreadyNotified = await _cosmosDbService.GetShiftRequestDetailsSent(user.ID);
                        
            openShifts = openShifts.Where(x => !requestsAlreadyNotified.Contains(x.RequestID)).ToList();

            if (openShifts.Count() > 0)
            {

                openShifts.GroupBy(x => x.RequestID)
                    .ToList()
                    .ForEach(async job =>
                    {
                        ExpandoObject o = new ExpandoObject();
                        o.TryAdd("id", Guid.NewGuid());
                        o.TryAdd("RequestId", job.Key);
                        o.TryAdd("RecipientUserId", recipientUserId.Value);
                        o.TryAdd("TemplateName", templateName);
                        o.TryAdd("event", "PrepareTemplateData");
                        await _cosmosDbService.AddItemAsync(o);
                    });

                SupportActivities? mostCommonActivity = GetMostCommonSupportActivityFromShifts(openShifts);

                return new EmailBuildData()
                {
                    BaseDynamicData = new NewRequestNotificationMessageData
                             (
                                title: mostCommonActivity.HasValue ? $"New { mostCommonActivity.Value.FriendlyNameShort() } shifts" : string.Empty,
                                subject: mostCommonActivity.HasValue ? $"New { mostCommonActivity.Value.FriendlyNameShort() } shifts have been added to HelpMyStreet" : "New activities have been added to HelpMyStreet",
                                firstName: user.UserPersonalDetails.FirstName,
                                shift: true,
                                requestList: GetRequestList(openShifts, user.PostalCode)
                             ),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}",
                    ReferencedJobs = GetReferencedJobs(openShifts)
                };
            }
            else
            {
                return null;
            }
        }

        private List<ReferencedJob> GetReferencedJobs(List<ShiftJob> shiftJobs)
        {
            if (shiftJobs.Count == 0)
            {
                return new List<ReferencedJob>();
            }

            List<ReferencedJob> jobs = new List<ReferencedJob>();
            shiftJobs.GroupBy(x => x.RequestID)
                    .ToList()
                    .ForEach(request =>
                    jobs.Add(new ReferencedJob()
                    {
                        R = request.Key
                    }
                    ));
            return jobs;
        }

        private SupportActivities? GetMostCommonSupportActivityFromShifts(List<ShiftJob> jobs)
        {
            if (jobs.Count == 0)
                return null;
            
            var a = jobs.GroupBy(g => g.SupportActivity)
                .Select(x => new { Activity = x.Key, Count = x.Count() })
                .OrderByDescending(o => o.Count)
                .First();

            return a.Activity;
        }

        private List<JobDetails> GetRequestList(List<ShiftJob> jobs, string postCode)
        {
            var summary = jobs.GroupBy(x => new { x.SupportActivity, x.StartDate, x.EndDate, x.ShiftLength, x.Location })
                .OrderBy(o => o.Key.StartDate)
                .Select(m => new {
                    SupportActivity = m.Key.SupportActivity,
                    Location = m.Key.Location,
                    ShiftDetails = $"{m.Key.StartDate.FormatDate(DateTimeFormat.LongDateTimeFormat)} - {m.Key.EndDate.FormatDate(DateTimeFormat.TimeFormat)}",
                }).ToList();

            List<JobDetails> result  = new List<JobDetails>();
            foreach (var item in summary)
            {
                var locationDetails = _connectAddressService.GetLocationDetails(item.Location).Result;
                
                result.Add(new JobDetails(
                    $"<strong>{item.SupportActivity.FriendlyNameShort()}</strong> " +
                    $"at <strong>{locationDetails.LocationDetails.Name}</strong>." +
                    $"Shift: { item.ShiftDetails }"));
            }

            return result;
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                GroupID = groupId,
                JobID = jobId,
                RequestID = requestId,
                AdditionalParameters = additionalParameters
            });
        }
    }
}
