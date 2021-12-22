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

namespace CommunicationService.MessageService
{
    public class NextDayReminderMessage : IMessage
    {
        private readonly IConnectRequestService _connectRequestService;
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IConnectGroupService _connectGroupService;
        private readonly IOptions<SendGridConfig> _sendGridConfig;

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

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            GetJobDetailsResponse job = await _connectRequestService.GetJobDetailsAsync(jobId.Value);

            if (job == null)
            {
                throw new Exception($"Job details cannot be retrieved for jobId {jobId}");
            }

            var availableVolunteers = await _connectGroupService.GetEligibleVolunteersForRequest(
                job.RequestSummary.ReferringGroupID,
                job.RequestSummary.Source,
                job.RequestSummary.PostCode,
                job.JobSummary.SupportActivity
                );

            foreach (VolunteerSummary vs in availableVolunteers)
            {
                if (!_cosmosDbService.EmailSent(TemplateName.NextDayReminder, jobId.Value, vs.UserID).Result)
                {
                    _sendMessageRequests.Add(new SendMessageRequest()
                    {
                        TemplateName = TemplateName.NextDayReminder,
                        RecipientUserID = vs.UserID,
                        GroupID = job.RequestSummary.ReferringGroupID,
                        JobID = jobId,
                        RequestID = requestId,
                        AdditionalParameters = new Dictionary<string, string>()
                        {
                            { "Distance", Math.Round(vs.DistanceInMiles,1).ToString() }
                        }
                    });
                }
            }
         
            return _sendMessageRequests;
        }
    }
}
