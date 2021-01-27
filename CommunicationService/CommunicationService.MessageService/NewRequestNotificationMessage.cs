using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.RequestService.Request;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Models;
using System;
using System.Collections.Generic;
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
        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? recipientUserId)
        {
            return UnsubscribeGroupName.TaskNotification;
        }

        public NewRequestNotificationMessage(IConnectRequestService connectRequestService, IConnectAddressService connectAddressService, IConnectUserService connectUserService)
        {
            _connectRequestService = connectRequestService;
            _connectAddressService = connectAddressService;
            _connectUserService = connectUserService;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            GetOpenShiftJobsByFilterRequest request = new GetOpenShiftJobsByFilterRequest();
            var shifts = await _connectRequestService.GetOpenShiftJobsByFilter(request);

            List<int> usersToBeNotified = new List<int>();

            if(shifts.ShiftJobs.Count>0)
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
                                usersToBeNotified.AddRange(users.Volunteers.Select(x => x.UserID).ToList());
                            }
                        }
                    }                    
                }

                if(usersToBeNotified.Count>0)
                {
                    usersToBeNotified
                        .Distinct()
                        .ToList()
                        .ForEach(userId =>
                        {
                            AddRecipientAndTemplate(TemplateName.RequestNotification, userId, null, null, null, additionalParameters);
                        });
                }
            }
            
            return _sendMessageRequests;
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            return new EmailBuildData()
            {
                BaseDynamicData = new NewRequestNotificationMessageData
                         (
                            title: "New vaccination programmme support shifts",
                            subject: "New vacinnation programme support shifts have been added to HelpMyStreet",
                            firstName:"Jawwad",
                            shift:true,
                            requestList: GetRequestList()
                         ),
                EmailToAddress = "jawwad@factor-50.co.uk",
                EmailToName = $"Jawwad Mukhtar"
            };
        }

        private List<JobDetails> GetRequestList()
        {
            return new List<JobDetails>()
            {
                new JobDetails("<strong>Vaccination Programme Support</strong> at <strong>Lincoln Hospital</strong>(20.00 miles away). 5 volunteers required. Shift: Tue, 22 December 2020 11:04 AM - 11:14 AM](Duration: [duration 1 hour 30 mins])"),
                new JobDetails("<strong>Vaccination Programme Support</strong> at <strong>Lincoln Hospital</strong>(20.00 miles away). 5 volunteers required. Shift: Tue, 20 January 2020 11:04 AM - 11:14 AM](Duration: [duration 1 hour 30 mins])")
            };
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
