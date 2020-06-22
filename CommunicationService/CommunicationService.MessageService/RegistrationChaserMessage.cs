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
        private Dictionary<int, string> _recipients;

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
            _recipients = new Dictionary<int, string>();
            
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId)
        {
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if (user != null)
            {
                return new EmailBuildData()
                {
                    BaseDynamicData = new RegistrationChaserData(user.UserPersonalDetails.FirstName, user.UserPersonalDetails.LastName),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = user.UserPersonalDetails.DisplayName,
                    RecipientUserID = recipientUserId.Value
                };
            }
            else
            {
                throw new Exception("unable to retrieve user details");
            }
        }

        private void AddRecipientAndTemplate(string templateName, int userId)
        {
            List<EmailHistory> history = _cosmosDbService.GetEmailHistory(templateName, userId.ToString()).Result;
            if (history.Count == 0)
            {
                _recipients.Add(userId, templateName);
            }
        }

        public Dictionary<int,string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            _recipients = new Dictionary<int, string>();
            var users = _connectUserService.GetIncompleteRegistrationStatusAsync().Result;

            if(users!=null)
            {
                var validUsersWithRange = users.Users.Where(x => ((DateTime.Now.ToUniversalTime() - x.DateCompleted).TotalMinutes >= _emailConfig.Value.RegistrationChaserMinTimeInMinutes)
                && ((DateTime.Now.ToUniversalTime() - x.DateCompleted).TotalHours <= _emailConfig.Value.RegistrationChaserMaxTimeInHours)).ToList();

                if(validUsersWithRange!=null)
                {
                    foreach(var u in validUsersWithRange)
                    {
                        var user = _connectUserService.GetUserByIdAsync(u.UserId).Result;
                        if (!user.IsVerified.HasValue)
                        {
                            if (u.RegistrationStep == REGISTRATION_STEP4)
                            {
                                AddRecipientAndTemplate(TemplateName.YotiReminder, u.UserId);
                            }
                            else
                            {
                                AddRecipientAndTemplate(TemplateName.PartialRegistration, u.UserId);
                            }
                        }
                    }
                }

            }
            return _recipients;
        }
    }
}
