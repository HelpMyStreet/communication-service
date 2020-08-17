using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class RegistrationChaserMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IOptions<EmailConfig> _emailConfig;
        private const int REGISTRATION_STEP4 = 4;
        List<SendMessageRequest> _sendMessageRequests;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.RegistrationUpdates;
            }
        }

        public RegistrationChaserMessage(IConnectUserService connectUserService, ICosmosDbService cosmosDbService, IOptions<EmailConfig> emailConfig)
        {
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            _emailConfig = emailConfig;
            _sendMessageRequests = new List<SendMessageRequest>();
            
        }

        private string GetTitleFromTemplateName(string templateName)
        {
            switch (templateName)
            {
                case TemplateName.PartialRegistration:
                    return "Almost there, complete your registration to start helping your street";
                case TemplateName.YotiReminder:
                    return "Welcome to Help My Street!";
                default:
                    throw new Exception($"{templateName} is unknown");
            }
        }

        public async Task<EmailBuildData> PrepareTemplateData(Guid batchId, int? recipientUserId, int? jobId, int? groupId,string templateName)
        {
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if (user != null)
            {
                return new EmailBuildData()
                {
                    BaseDynamicData = new RegistrationChaserData(user.UserPersonalDetails.FirstName, user.UserPersonalDetails.LastName, GetTitleFromTemplateName(templateName)),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = user.UserPersonalDetails.DisplayName
                };
            }
            else
            {
                throw new Exception("unable to retrieve user details");
            }
        }

        private void AddRecipientAndTemplate(string templateName, int userId, int? jobId, int? groupId)
        {
            List<EmailHistory> history = _cosmosDbService.GetEmailHistory(templateName, userId.ToString()).Result;
            if (history.Count == 0)
            {
                _sendMessageRequests.Add(new SendMessageRequest()
                {
                    TemplateName = templateName,
                    RecipientUserID = userId,
                    GroupID = groupId,
                    JobID = jobId
                });
            }
        }

        public async Task<List<SendMessageRequest>> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            var users = await _connectUserService.GetIncompleteRegistrationStatusAsync();

            if(users!=null)
            {
                var validUsersWithRange = users.Users.Where(x => ((DateTime.Now.ToUniversalTime() - x.DateCompleted).TotalMinutes >= _emailConfig.Value.RegistrationChaserMinTimeInMinutes)
                && ((DateTime.Now.ToUniversalTime() - x.DateCompleted).TotalHours <= _emailConfig.Value.RegistrationChaserMaxTimeInHours)).ToList();

                if(validUsersWithRange!=null)
                {
                    foreach(var u in validUsersWithRange)
                    {
                        var user = await _connectUserService.GetUserByIdAsync(u.UserId);
                        if (!user.IsVerified.HasValue)
                        {
                            if (u.RegistrationStep == REGISTRATION_STEP4)
                            {
                                AddRecipientAndTemplate(TemplateName.YotiReminder, u.UserId, jobId, groupId);
                            }
                            else
                            {
                                AddRecipientAndTemplate(TemplateName.PartialRegistration, u.UserId, jobId, groupId);
                            }
                        }
                    }
                }

            }
            return _sendMessageRequests;
        }
    }
}
