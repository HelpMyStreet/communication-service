using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Dynamic;

namespace CommunicationService.AzureFunction
{
    public class ProcessMessageQueue
    {
        private readonly IMessageFactory _messageFactory;
        private readonly ISendEmailService _sendEmailService;
        private readonly ICosmosDbService _cosmosDbService;

        public ProcessMessageQueue(IMessageFactory messageFactory, ISendEmailService sendEmailService, ICosmosDbService cosmosDbService)
        {
            _messageFactory = messageFactory;
            _sendEmailService = sendEmailService;
            _cosmosDbService = cosmosDbService;
        }

        [FunctionName("ProcessMessageQueue")]
        public void Run([ServiceBusTrigger("message", Connection = "ServiceBus")]string myQueueItem, ILogger log)
        {
            SendMessageRequest sendMessageRequest = JsonConvert.DeserializeObject<SendMessageRequest>(myQueueItem);
            IMessage message = _messageFactory.Create(sendMessageRequest);
            EmailBuildData sendGridData = message.PrepareTemplateData(sendMessageRequest.RecipientUserID, sendMessageRequest.JobID, sendMessageRequest.GroupID).Result;

            _sendEmailService.SendDynamicEmail(sendMessageRequest.TemplateID, sendGridData);
            AddCommunicationRequestToCosmos(sendMessageRequest);
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }

        private void AddCommunicationRequestToCosmos(SendMessageRequest sendMessageRequest)
        {
            dynamic message;

            message = new ExpandoObject();
            message.id = Guid.NewGuid();
            message.TemplateId = sendMessageRequest.TemplateID;
            message.RecipientUserID = sendMessageRequest.RecipientUserID;
            message.JobID = sendMessageRequest.JobID;
            message.GroupID = sendMessageRequest.GroupID;
            message.CommunicationJob = sendMessageRequest.CommunicationJobType;
            _cosmosDbService.AddItemAsync(message);
        }
    }
}
