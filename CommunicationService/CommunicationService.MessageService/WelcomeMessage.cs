using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class WelcomeMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        public WelcomeMessage(IConnectUserService connectUserService)
        {
            _connectUserService = connectUserService;
            
        }
        public List<int> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            return new List<int>()
            {
                recipientUserId.Value
            };
        }
        public async Task<object> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId)
        {
            var user = await _connectUserService.GetUserByIdAsync(recipientUserId.Value);

            if (user != null)
            {
                return new Welcome(user.UserPersonalDetails.FirstName, user.UserPersonalDetails.LastName);
            }
            throw new Exception("unable to retrive user details");
        }

        public Task<string> PrepareJSON(int? recipientUserId, int? jobId, int? groupId)
        {
            throw new NotImplementedException();
        }

        public string GetTemplateId()
        {
            return "d-14a35071720e4a9fa0619c6891b3f108";
        }
    }
}
