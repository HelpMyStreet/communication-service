using System.Collections.Generic;
using System.Threading;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Utils.Enums;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CommunicationService.AzureFunction
{
    public class ProcessJobQueue
    {
        private readonly IMessageFactory _messageFactory;

        public ProcessJobQueue(IMessageFactory messageFactory)
        {
            _messageFactory = messageFactory;
        }

        [FunctionName("ProcessJobQueue")]
        public void Run([ServiceBusTrigger("job", Connection = "ServiceBus")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"start ProcessJobQueue myQueueItem {myQueueItem}");

            RequestCommunicationRequest sendCommunicationRequest = JsonConvert.DeserializeObject<RequestCommunicationRequest>(myQueueItem);
            IMessage message = _messageFactory.Create(sendCommunicationRequest);
            List<SendMessageRequest> messageDetails = message.IdentifyRecipients(sendCommunicationRequest.RecipientUserID, sendCommunicationRequest.JobID, sendCommunicationRequest.GroupID);

            if (messageDetails.Count == 0)
            {
                log.LogInformation("No recipients identified");
            }
            else
            {
                var rec = JsonConvert.SerializeObject(messageDetails);
                log.LogInformation($"Recipients { rec}");
            }

            foreach (var m in messageDetails)
            {
                _messageFactory.AddToMessageQueueAsync(new SendMessageRequest()
                {
                    CommunicationJobType = sendCommunicationRequest.CommunicationJob.CommunicationJobType,
                    TemplateName = m.TemplateName,
                    RecipientUserID = m.RecipientUserID,
                    JobID = m.JobID,
                    GroupID = m.GroupID,
                    MessageType = MessageTypes.Email
                });
                Thread.Sleep(2000);
            }

            log.LogInformation($"End ProcessJobQueue:{myQueueItem}");
        }



    }
}
