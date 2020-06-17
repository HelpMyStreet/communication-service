using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class SuccessfulYotiMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;

        public string UnsubscriptionGroupName
        {
            get
            {
                return TemplateName.SuccessfulYoti;
            }
        }

        public SuccessfulYotiMessage(IConnectUserService connectUserService, ICosmosDbService cosmosDbService)
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
                    BaseDynamicData = new SuccessfulYotiMessageData(user.UserPersonalDetails.FirstName, user.UserPersonalDetails.LastName),
                    EmailToAddress = user.UserPersonalDetails.EmailAddress,
                    EmailToName = user.UserPersonalDetails.DisplayName,
                    RecipientUserID = recipientUserId.Value
                };
            }
            else
            {
                throw new Exception("unable to retrive user details");
            }
        }

        public Dictionary<int,string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int, string> response = new Dictionary<int, string>();

            var user = _connectUserService.GetUserByIdAsync(recipientUserId.Value).Result;
            if (user != null && user.IsVerified.HasValue && user.IsVerified.Value)
            {
                List<EmailHistory> reminderhistory = _cosmosDbService.GetEmailHistory(TemplateName.YotiReminder, user.ID.ToString()).Result;
                if (reminderhistory.Count>0)
                {
                    List<EmailHistory> history = _cosmosDbService.GetEmailHistory(TemplateName.SuccessfulYoti, user.ID.ToString()).Result;
                    if (history.Count == 0)
                    {
                        response.Add(recipientUserId.Value, TemplateName.SuccessfulYoti);
                    }
                }
            }
            return response;
        }
    }
}
