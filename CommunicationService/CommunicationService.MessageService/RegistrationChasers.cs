using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using Polly.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class RegistrationChasers : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly ICosmosDbService _cosmosDbService;
        private const int REGISTRATION_STEP4 = 4;
        private const string TEMPLATENAME_PLEASEVERIFY = "PleaseVerify";
        private const string TEMPLATENAME_INCOMPLETEREGISTRATION = "IncompleteRegistration";

        public RegistrationChasers(IConnectUserService connectUserService, ICosmosDbService cosmosDbService)
        {
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
        }

        public Dictionary<int, string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            var users = _connectUserService.GetIncompleteRegistrationStatusAsync().Result;

            if(users==null || users.Users.Count>0)
            {
                foreach(UserRegistrationStep u in users.Users)
                {
                    //if (u.RegistrationStep==REGISTRATION_STEP4)
                    //{
                    //    if (result.Count < 1)
                    //    {
                    //        List<EmailHistory> history = _cosmosDbService.GetEmailHistory(TEMPLATENAME_PLEASEVERIFY, u.UserId.ToString()).Result;
                    //        if (history.Count == 0)
                    //        {
                    //            result.Add(u.UserId, TEMPLATENAME_PLEASEVERIFY);
                    //        }
                    //    }
                    //}
                    //else
                    //{
                        if (result.Count < 1)
                        {
                            List<EmailHistory> history = _cosmosDbService.GetEmailHistory(TEMPLATENAME_INCOMPLETEREGISTRATION, u.UserId.ToString()).Result;
                            if (history.Count == 0)
                            {
                                result.Add(u.UserId, TEMPLATENAME_INCOMPLETEREGISTRATION);
                            }
                        }
                    //}
                }
            }
            return result;
         
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
                throw new Exception("unable to retrive user details");
            }
        }
    }
}
