using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
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
        private const int REGISTRATION_STEP4 = 4;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.RegistrationUpdates;
            }
        }

        public RegistrationChaserMessage(IConnectUserService connectUserService, ICosmosDbService cosmosDbService)
        {
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
            
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

        private Dictionary<int, string> AddRecipientAndTemplate(string templateName, int userId)
        {
            List<EmailHistory> history = _cosmosDbService.GetEmailHistory(templateName, userId.ToString()).Result;
            if (history.Count == 0)
            {
                return new Dictionary<int, string>()
                {
                    {userId,templateName }
                };
            }
            else
            {
                return new Dictionary<int, string>();
            }
        }

        public Dictionary<int,string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int, string> response = new Dictionary<int, string>();
            var users = _connectUserService.GetIncompleteRegistrationStatusAsync().Result;

            if(users!=null)
            {
                var validUsersWithRange = users.Users.Where(x => ((DateTime.Now.ToUniversalTime() - x.DateCompleted).TotalMinutes >= 30)
                && ((DateTime.Now.ToUniversalTime() - x.DateCompleted).TotalHours <=48)).ToList();

                if(validUsersWithRange!=null)
                {
                    foreach(var u in validUsersWithRange)
                    {
                        var user = _connectUserService.GetUserByIdAsync(u.UserId).Result;
                        if (!user.IsVerified.HasValue)
                        {
                            if (u.RegistrationStep == REGISTRATION_STEP4)
                            {
                                response = AddRecipientAndTemplate(TemplateName.YotiReminder, u.UserId);
                            }
                            else
                            {
                                response = AddRecipientAndTemplate(TemplateName.PartialRegistration, u.UserId);
                            }
                        }
                    }
                }

            }
            return response;
        }
    }
}
