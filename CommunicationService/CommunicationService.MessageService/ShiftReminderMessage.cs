using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelpMyStreet.Utils.Models;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Extensions;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Contracts.RequestService.Request;
using System.Linq;
using CommunicationService.Core.Domains.SendGrid;
using HelpMyStreet.Utils.Utils;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;

namespace CommunicationService.MessageService
{
    public class ShiftReminderMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectAddressService _connectAddressService;
        private readonly ILinkRepository _linkRepository;
        private readonly IOptions<LinkConfig> _linkConfig;        

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? receipientId)
        {
                return UnsubscribeGroupName.TaskReminder;
        }

        public ShiftReminderMessage(IConnectRequestService connectRequestService, 
            IConnectUserService connectUserService, 
            IConnectAddressService connectAddressService,
            ILinkRepository linkRepository,
            IOptions<LinkConfig> linkConfig
            )
        {
            _connectRequestService = connectRequestService;
            _connectUserService = connectUserService;
            _connectAddressService = connectAddressService;
            _linkRepository = linkRepository;
            _linkConfig = linkConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        private string FormatDate(DateTime dateTime)
        {
            string s = dateTime.FriendlyFutureDate();
            if(s == "tomorrow")
            {
                s = $"Tommorow ({ dateTime.FormatDate(DateTimeFormat.LongDateFormat)})";
            }
            return $"{s} at {dateTime.FormatDate(DateTimeFormat.TimeFormat)}";
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var request = await _connectRequestService.GetRequestDetailsAsync(requestId.Value);
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);
            var job = request.RequestSummary.JobSummaries.Where(x => x.JobID == jobId.Value).FirstOrDefault();
            var location = await _connectAddressService.GetLocationDetails(request.RequestSummary.Shift.Location);

            string encodedJobId = Base64Utils.Base64Encode(jobId.Value.ToString());
            var joburlToken = await _linkRepository.CreateLink($"/link/j/{encodedJobId}", _linkConfig.Value.ExpiryDays);

            return new EmailBuildData()
            {
                BaseDynamicData = new ShiftReminderMessageData(
                    title: "Volunteer shift reminder",
                    subject: "Volunteer shift reminder",
                    firstname: user.UserPersonalDetails.FirstName,
                    activity: job.SupportActivity.FriendlyNameShort(),
                    location: location.LocationDetails.Name,
                    shiftStartDateString: FormatDate(request.RequestSummary.Shift.StartDate),
                    shiftEndDateString: FormatDate(request.RequestSummary.Shift.EndDate),
                    locationAddress: string.Empty,
                    joburlToken: joburlToken
                    ),
                EmailToAddress = user.UserPersonalDetails.EmailAddress,
                EmailToName = $"{user.UserPersonalDetails.FirstName} {user.UserPersonalDetails.LastName}"
            };
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId, int? requestId)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                JobID = jobId,
                GroupID = groupId,
                RequestID = requestId
            });
        }


        private async Task GetRecipients(DateTime dateFilter)
        {
            GetShiftRequestsByFilterRequest request = new GetShiftRequestsByFilterRequest()
            {
                DateFrom = dateFilter.Date,
                DateTo = dateFilter.AddDays(1).Date
            };

            var shifts = await _connectRequestService.GetShiftRequestsByFilter(request);

            if (shifts != null && shifts?.RequestSummaries.Count > 0)
            {
                shifts.RequestSummaries
                    .SelectMany(shiftjobs => shiftjobs.JobSummaries)?
                    .Where(w => w.JobStatus == JobStatuses.Accepted)
                    .ToList()
                    .ForEach(job => AddRecipientAndTemplate(TemplateName.ShiftReminder, job.VolunteerUserID.Value, job.JobID, job.ReferringGroupID, job.RequestID));
            }
        }
        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            await GetRecipients(DateTime.Now.Date.AddDays(1));
            await GetRecipients(DateTime.Now.Date.AddDays(7));
            return _sendMessageRequests;
        }
    }
}
