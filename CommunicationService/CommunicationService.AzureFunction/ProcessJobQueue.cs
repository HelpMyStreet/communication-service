using System;
using System.Collections.Generic;
using System.Linq;
using CommunicationService.Core.Domains.Entities.Request;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Services;
using CommunicationService.MessageService;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
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
        public void Run([ServiceBusTrigger("job", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {

            SendCommunicationRequest sendCommunicationRequest = JsonConvert.DeserializeObject<SendCommunicationRequest>(myQueueItem);
            IMessage message = _messageFactory.Create(sendCommunicationRequest);
            List<int> recipients = message.IdentifyRecipients(sendCommunicationRequest.RecipientUserID, sendCommunicationRequest.JobID, sendCommunicationRequest.GroupID);

            foreach (int i in recipients)
            {
                _messageFactory.AddToRecipientQueueAsync(new SendCommunicationRequest()
                {
                    EmailTemplate = sendCommunicationRequest.EmailTemplate,
                    RecipientUserID = i,
                    JobID = sendCommunicationRequest.JobID,
                    GroupID = sendCommunicationRequest.GroupID
                });
            }
            
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }



    }
}
