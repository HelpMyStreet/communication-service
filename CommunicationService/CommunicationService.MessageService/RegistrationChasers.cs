using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Models;
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
        private const string TEMPLATENAME_FAILEDYOTI = "FailedYoti";
        


        public RegistrationChasers(IConnectUserService connectUserService, ICosmosDbService cosmosDbService)
        {
            _connectUserService = connectUserService;
            _cosmosDbService = cosmosDbService;
        }

        public string UnsubscriptionGroupName
        {
            get
            {
                return "IncompleteRegistration";
            }
        }

        private Dictionary<int, string> AddFailedYoti(List<UserRegistrationStep> userRegistrationSteps)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            if(userRegistrationSteps != null && userRegistrationSteps.Count>0)
            {
                var usersInStep4 = userRegistrationSteps.Where(x => x.RegistrationStep == REGISTRATION_STEP4).ToList();
                foreach(UserRegistrationStep u in usersInStep4)
                {
                    var user = _connectUserService.GetUserByIdAsync(u.UserId).Result;
                    if (user.IsVerified.HasValue && !user.IsVerified.Value)
                    {
                        List<EmailHistory> history = _cosmosDbService.GetEmailHistory(TEMPLATENAME_FAILEDYOTI, u.UserId.ToString()).Result;
                        if (history.Count == 0)
                        {
                            result.Add(u.UserId, TEMPLATENAME_FAILEDYOTI);
                        }
                    }
                }
            }
            return result;
        }


        public Dictionary<int, string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            var users = _connectUserService.GetIncompleteRegistrationStatusAsync().Result;

            if (users == null || users.Users.Count > 0)
            {
                var yotiFailed = AddFailedYoti(users.Users);
                foreach (var kvp in yotiFailed)
                {
                    result.Add(kvp.Key, kvp.Value);
                }
            }

                //foreach(UserRegistrationStep u in users.Users)
                //{
                //    if (u.RegistrationStep == REGISTRATION_STEP4)
                //    {
                //        var user = _connectUserService.GetUserByIdAsync(u.UserId).Result;
                //        if (user.IsVerified.HasValue && !user.IsVerified.Value)
                //        {
                //            List<EmailHistory> history = _cosmosDbService.GetEmailHistory(TEMPLATENAME_FAILEDYOTI, u.UserId.ToString()).Result;
                //            if (history.Count == 0)
                //            {
                //                result.Add(u.UserId, TEMPLATENAME_FAILEDYOTI);
                //                return result;
                //            }
                //        }
                //    }
                    //else
                    //{
                    //    List<EmailHistory> history = _cosmosDbService.GetEmailHistory(TEMPLATENAME_INCOMPLETEREGISTRATION, u.UserId.ToString()).Result;
                    //    if (history.Count == 0)
                    //    {
                    //        result.Add(u.UserId, TEMPLATENAME_INCOMPLETEREGISTRATION);
                    //        return result;
                    //    }
                    //}
                //}
            //}
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
