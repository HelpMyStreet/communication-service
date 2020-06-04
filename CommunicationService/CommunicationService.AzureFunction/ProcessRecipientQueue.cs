using System;
using System.Collections.Generic;
using CommunicationService.Core.Domains;
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
    public class ProcessRecipientQueue
    {
        private readonly IMessageFactory _messageFactory;
        private readonly ISendEmailService _sendEmailService;

        public ProcessRecipientQueue(IMessageFactory messageFactory, ISendEmailService sendEmailService)
        {
            _messageFactory = messageFactory;
            _sendEmailService = sendEmailService;
        }

        [FunctionName("ProcessRecipientQueue")]
        public void Run([ServiceBusTrigger("recipient", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            SendCommunicationRequest sendCommunicationRequest = JsonConvert.DeserializeObject<SendCommunicationRequest>(myQueueItem);
            IMessage message = _messageFactory.Create(sendCommunicationRequest);
            SendGridData sendGridData = message.PrepareTemplateData(sendCommunicationRequest.RecipientUserID, sendCommunicationRequest.JobID, sendCommunicationRequest.GroupID).Result;
            _sendEmailService.SendDynamicEmail(message.GetTemplateId(), sendGridData);
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
