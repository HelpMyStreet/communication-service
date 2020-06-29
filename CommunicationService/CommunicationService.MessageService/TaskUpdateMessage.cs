using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService.Substitution;
using HelpMyStreet.Contracts.UserService.Response;
using HelpMyStreet.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationService.MessageService
{
    public class TaskUpdateMessage : IMessage
    {
        private readonly IConnectUserService _connectUserService;
        private readonly IConnectRequestService _connectRequestService;

        public string UnsubscriptionGroupName
        {
            get
            {
                return UnsubscribeGroupName.TaskNotification;
            }
        }

        public TaskUpdateMessage(IConnectUserService connectUserService, IConnectRequestService connectRequestService)
        {
            _connectUserService = connectUserService;
            _connectRequestService = connectRequestService;
        }

        public async Task<EmailBuildData> PrepareTemplateData(int? recipientUserId, int? jobId, int? groupId, string templateName)
        {
            var job = _connectRequestService.GetJobDetailsAsync(jobId.Value).Result;

            return new EmailBuildData()
            {
                BaseDynamicData = new TaskUpdateData
                (
                job.DateRequested.ToString("dd/MM/yyyy"),
                Mapping.ActivityMappings[job.SupportActivity],
                Mapping.StatusMappings[job.JobStatus]
                ),
                EmailToAddress = job.Requestor.EmailAddress,
                EmailToName = $"{job.Requestor.FirstName} {job.Requestor.LastName}",
                RecipientUserID = -1,
            };
        }

        public Dictionary<int, string> IdentifyRecipients(int? recipientUserId, int? jobId, int? groupId)
        {
            Dictionary<int, string> recipients = new Dictionary<int, string>();
            recipients.Add(-1, TemplateName.TaskUpdate);

            return recipients;
        }
    }
}
