using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Utils.Exceptions;
using System.Linq;
using HelpMyStreet.Contracts.RequestService.Response;
using Microsoft.Extensions.Options;
using CommunicationService.Core.Configuration;
using HelpMyStreet.Utils.Enums;
using HelpMyStreet.Utils.Extensions;

namespace CommunicationService.MessageService
{
    public class NewUserNotificationMessage : IMessage
    {
        private readonly IConnectGroupService _connectGroupService;
        private readonly IConnectUserService _connectUserService;
        private readonly IOptions<SendGridConfig> _sendGridConfig;

        List<SendMessageRequest> _sendMessageRequests;

        public string GetUnsubscriptionGroupName(int? receipientId)
        {
                return UnsubscribeGroupName.NewUserNotification;
        }

        public NewUserNotificationMessage(IConnectGroupService connectGroupService, IConnectUserService connectUserService, IOptions<SendGridConfig> sendGridConfig)
        {
            _connectGroupService = connectGroupService;
            _connectUserService = connectUserService;
            _sendGridConfig = sendGridConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters, string templateName)
        {
            var userAdmin = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;

            if (userAdmin == null)
            {
                throw new BadRequestException($"unable to retrieve user object for {recipientUserId.Value}");
            }

            additionalParameters.TryGetValue("Volunteer", out string strVolunteer);

            if(string.IsNullOrEmpty(strVolunteer))
            {
                throw new BadRequestException($"missing volunteer information");
            }

            var volunteer = _connectUserService.GetUserByIdAsync(Convert.ToInt32(strVolunteer)).Result;

            if (volunteer == null)
            {
                throw new BadRequestException($"unable to retrieve volunteer details for {strVolunteer}");
            }


            string volunteerSupportActivities = string.Join(",", volunteer.SupportActivities
                                 .Select(v => v.FriendlyNameShort()));


            var group = _connectGroupService.GetGroup(groupId.Value).Result;

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
            
            if(groupLogoAvailable)
            {
                groupLogo = $"group-logos/{group.Group.GroupKey}-partnership.png";
            }

            return new EmailBuildData()
            {
                BaseDynamicData = new NewUserNotificationData(
                    title: $"A new user has joined { group.Group.GroupName} on HelpMyStreet",
                    subject: $"A new user has joined { group.Group.GroupName} on HelpMyStreet",
                    firstName: userAdmin.UserPersonalDetails.FirstName,
                    groupLogoAvailable: groupLogoAvailable,
                    groupLogo: groupLogo,
                    volunteerName: volunteer.UserPersonalDetails.FirstName + " " + volunteer.UserPersonalDetails.LastName,
                    volunteerLocation:volunteer.PostalCode.Split(" ").First(),
                    volunteerActivities: volunteerSupportActivities,
                    groupKey : group.Group.GroupKey
                    ),
                    JobID = jobId,
                    GroupID = groupId,
                    RecipientUserID = recipientUserId.Value,
                    EmailToAddress = userAdmin.UserPersonalDetails.EmailAddress,
                    EmailToName = $"{userAdmin.UserPersonalDetails.FirstName} {userAdmin.UserPersonalDetails.LastName}"
            };
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

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            _sendMessageRequests.Add(new SendMessageRequest()
            {
                TemplateName = templateName,
                RecipientUserID = userId,
                JobID = jobId,
                GroupID = groupId,
                RequestID = requestId,
                AdditionalParameters = additionalParameters
            });
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId, int? requestId, Dictionary<string, string> additionalParameters)
        {
            var userAdmins = await _connectGroupService.GetGroupMembersForGivenRole(groupId.Value, GroupRoles.UserAdmin);
            var userAdminsReadOnly = await _connectGroupService.GetGroupMembersForGivenRole(groupId.Value, GroupRoles.UserAdmin_ReadOnly);
            var combinedUsers = userAdmins.Concat(userAdminsReadOnly).Distinct().ToList();

            if (combinedUsers != null)
            {
                combinedUsers
                    .ForEach(m =>
                    {
                        AddRecipientAndTemplate(TemplateName.NewUserNotification, m, jobId, groupId, requestId, additionalParameters);
                    });
            };

            return _sendMessageRequests;
        }
    }
}
