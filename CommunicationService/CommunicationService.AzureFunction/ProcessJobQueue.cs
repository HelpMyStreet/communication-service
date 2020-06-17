using System.Collections.Generic;
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
            //RequestCommunicationRequest sendCommunicationRequest = JsonConvert.DeserializeObject<RequestCommunicationRequest>(myQueueItem);
            //IMessage message = _messageFactory.Create(sendCommunicationRequest);
            //Dictionary<int, string> recipients = message.IdentifyRecipients(sendCommunicationRequest.RecipientUserID, sendCommunicationRequest.JobID, sendCommunicationRequest.GroupID);

            //foreach (var item in recipients)
            //{
            //    _messageFactory.AddToMessageQueueAsync(new SendMessageRequest()
            //    {
            //        CommunicationJobType = sendCommunicationRequest.CommunicationJob.CommunicationJobType,
            //        TemplateName = item.Value,
            //        RecipientUserID = item.Key,
            //        JobID = sendCommunicationRequest.JobID,
            //        GroupID = sendCommunicationRequest.GroupID,
            //        MessageType = MessageTypes.Email
            //    });
            //}

            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }



    }
}
