using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
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
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IConnectSendGridService _connectSendGridService;

        public ProcessMessageQueue(IMessageFactory messageFactory,ICosmosDbService cosmosDbService, IConnectSendGridService connectSendGridService)
        {
            _messageFactory = messageFactory;
            _cosmosDbService = cosmosDbService;
            _connectSendGridService = connectSendGridService;
        }

        [FunctionName("ProcessMessageQueue")]
        public void Run([ServiceBusTrigger("message", Connection = "ServiceBus")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"Start ProcessMessageQueue:{myQueueItem}");
            try
            {
                SendMessageRequest sendMessageRequest = JsonConvert.DeserializeObject<SendMessageRequest>(myQueueItem);
                IMessage message = _messageFactory.Create(sendMessageRequest);
                EmailBuildData emailBuildData = message.PrepareTemplateData(sendMessageRequest.RecipientUserID, sendMessageRequest.JobID, sendMessageRequest.GroupID, sendMessageRequest.TemplateName).Result;
                if (emailBuildData != null)
                {
                    AddCommunicationRequestToCosmos(sendMessageRequest);

                    var result = _connectSendGridService.SendDynamicEmail(sendMessageRequest.TemplateName, message.UnsubscriptionGroupName, emailBuildData).Result;
                    log.LogInformation($"SendDynamicEmail({sendMessageRequest.TemplateName}) returned {result}");

                }
            }
            catch (AggregateException exc)
            {
                log.LogError($"{exc.Flatten()} error for queueitem {myQueueItem}");
            }
            catch (Exception exc)
            {
                log.LogError($"{exc} error for queueitem {myQueueItem}");
            }
            log.LogInformation($"End ProcessMessageQueue:{myQueueItem}");
        }

        private void AddCommunicationRequestToCosmos(SendMessageRequest sendMessageRequest)
        {
            try
            {
                dynamic message;

                message = new ExpandoObject();
                message.id = Guid.NewGuid();
                message.TemplateName = sendMessageRequest.TemplateName;
                message.RecipientUserID = sendMessageRequest.RecipientUserID;
                message.JobID = sendMessageRequest.JobID;
                message.GroupID = sendMessageRequest.GroupID;
                message.CommunicationJob = sendMessageRequest.CommunicationJobType;
                _cosmosDbService.AddItemAsync(message);
            }
            catch(Exception exc)
            {
                string m = exc.ToString();
            }
        }
    }
}
