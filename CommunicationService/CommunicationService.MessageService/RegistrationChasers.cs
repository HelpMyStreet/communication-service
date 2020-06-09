using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using Polly.Caching;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class RegistrationChasers : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private const int REGISTRATION_STEP4 = 4;
        private const string TEMPLATEID_PLEASEVERIFY = "d-c720b30757354dc0b2cb6f02195afcaa";

        public RegistrationChasers(IConnectUserService connectUserService)
        {
            _connectUserService = connectUserService;
        }
        public string TemplateId
        {
            get
            {
                return "d-0a0f3568847142d4ace58330522a6cf6";
            }
        }

        public Dictionary<int, string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            var users = _connectUserService.GetIncompleteRegistrationStatusAsync().Result;

            if(users==null || users.Users.Count>0)
            {
                foreach(UserRegistrationStep u in users.Users)
                {
                    if(u.RegistrationStep==REGISTRATION_STEP4)
                    {
                        if (result.Count < 2)
                        {
                            result.Add(u.UserId, TEMPLATEID_PLEASEVERIFY);
                        }
                    }
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
