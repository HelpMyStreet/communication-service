using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Models;
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

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.TaskNotification;
        }

        public NewRequestNotificationMessage(IConnectRequestService connectRequestService, IConnectAddressService connectAddressService, IConnectUserService connectUserService, ICosmosDbService cosmosDbService)
        {
            _connectRequestService = connectRequestService;
            _connectAddressService = connectAddressService;
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {            
            GetOpenShiftJobsByFilterRequest request = new GetOpenShiftJobsByFilterRequest();
            var shifts = await _connectRequestService.GetOpenShiftJobsByFilter(request);

            List<Tuple<int, Location>> usersToBeNotified = new List<Tuple<int, Location>>();

            if (shifts.ShiftJobs.Count>0)
            {         
                var locationsSupportActivities = shifts.ShiftJobs.GroupBy(d => new { d.Location, d.SupportActivity })
                    .Select(m => new { m.Key.Location, m.Key.SupportActivity });

                if(locationsSupportActivities!=null && locationsSupportActivities.Count()>0)
                {
                    foreach(var x in locationsSupportActivities)
                    {
                        var locations = await _connectAddressService.GetLocationDetails(x.Location);

                        if(locations !=null)
                        {
                            var users = await _connectUserService.GetVolunteersByPostcodeAndActivity(
                                locations.LocationDetails.Address.Postcode,
                                new List<SupportActivities>() { x.SupportActivity },
                                CancellationToken.None);

                            if(users!=null && users.Volunteers.Count()>0)
                            {
                                usersToBeNotified.AddRange(users.Volunteers.Select(i => new Tuple<int,Location>(i.UserID,x.Location)).ToList());
                            }
                        }
                    }                    
                }

                if (usersToBeNotified.Count > 0)
                {
                    foreach (var userId in usersToBeNotified.GroupBy(g => g.Item1).Select(m=> m.Key).ToList())
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



            LocationsRequest lr = new LocationsRequest() { Locations = new List<Location>() };

            locations.Split(",").ToList()
                .ForEach(x =>
                {
                    lr.Locations.Add((Location)Enum.Parse(typeof(Location), x));
                });

            GetOpenShiftJobsByFilterRequest request = new GetOpenShiftJobsByFilterRequest();
            var shifts = await _connectRequestService.GetOpenShiftJobsByFilter(request);

            if (shifts == null || shifts.ShiftJobs.Count == 0)
            {
                throw new Exception($"No shifts returned from user id {recipientUserId.Value}");
            }

            List<int> requestsAlreadyNotified = await _cosmosDbService.GetShiftRequestDetailsSent(user.ID);

            shifts.ShiftJobs = shifts.ShiftJobs.
                Where(x => !requestsAlreadyNotified.Contains(x.RequestID)).ToList();

            if (shifts.ShiftJobs.Count > 0)
            {

                shifts.ShiftJobs.GroupBy(x => x.RequestID)
                    .ToList()
                    .ForEach(async job =>
                    {
                        ExpandoObject o = new ExpandoObject();
                        o.TryAdd("id", Guid.NewGuid());
                        o.TryAdd("RequestId", job.Key);
                        o.TryAdd("RecipientUserId", recipientUserId.Value);
                        o.TryAdd("TemplateName", templateName);

                        await _cosmosDbService.AddItemAsync(o);
                    });

                SupportActivities? mostCommonActivity = GetMostCommonSupportActivityFromShifts(shifts.ShiftJobs);

                return new EmailBuildData()
                {
                    BaseDynamicData = new NewRequestNotificationMessageData
                             (
                                title: mostCommonActivity.HasValue ? $"New { mostCommonActivity.Value.FriendlyNameShort() } shifts" : string.Empty,
                                subject: mostCommonActivity.HasValue ? $"New { mostCommonActivity.Value.FriendlyNameShort() } shifts have been added to HelpMyStreet" : "New activities have been added to HelpMyStreet",
                                firstName: user.UserPersonalDetails.FirstName,
                                shift: true,
                                requestList: GetRequestList(shifts.ShiftJobs, user.PostalCode)
                             ),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
                };
            }
            else
            {
                return null;
            }
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
            var locationDistances = _connectAddressService.GetLocationsByDistance(postCode, 100).Result;

            var summary = jobs.GroupBy(x => new { x.SupportActivity, x.StartDate, x.EndDate, x.ShiftLength, x.Location })
                .OrderBy(o => o.Key.StartDate)
                .Select(m => new {
                    SupportActivity = m.Key.SupportActivity,
                    Location = m.Key.Location,
                    ShiftDetails = $"{m.Key.StartDate.ToString("ddd, dd MMMM yyy h:mm tt")} - {m.Key.EndDate.ToString("h:mm tt")}",
                    Duration = $"{ Math.Floor(TimeSpan.FromMinutes(m.Key.ShiftLength).TotalHours)} hrs { TimeSpan.FromMinutes(m.Key.ShiftLength).Minutes } mins",
                    Count = m.Count()
                }).ToList();

            List<JobDetails> result  = new List<JobDetails>();
            foreach (var item in summary)
            {
                var locationDetails = _connectAddressService.GetLocationDetails(item.Location).Result;
                var distanceFromUser = locationDistances.LocationDistances.Where(x => x.Location == item.Location).Select(x => x.DistanceFromPostCode).FirstOrDefault();

                result.Add(new JobDetails($"<strong>{item.SupportActivity.FriendlyNameShort()}</strong> at <strong>{locationDetails.LocationDetails.Name}</strong>" +
                    $" ({Math.Round(distanceFromUser, 2)} miles away). " +
                    $"{item.Count} volunteers required. " +
                    $"Shift: <strong>{ item.ShiftDetails }</strong>" +
                    $" (Duration: {item.Duration })"));
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
