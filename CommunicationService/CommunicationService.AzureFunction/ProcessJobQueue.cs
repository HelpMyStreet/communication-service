using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Utils.Enums;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CommunicationService.AzureFunction
{
    public class ProcessJobQueue
    {
        private readonly IMessageFactory _messageFactory;
        private readonly ICosmosDbService _cosmosDbService;

        public ProcessJobQueue(IMessageFactory messageFactory, ICosmosDbService cosmosDbService)
        {
            _messageFactory = messageFactory;
            _cosmosDbService = cosmosDbService;
        }

        [FunctionName("ProcessJobQueue")]
        public async Task Run([ServiceBusTrigger("job", Connection = "ServiceBus")]Message mySbMsg, ILogger log)
        {
            log.LogInformation($"ProcessJobQueue received message id: {mySbMsg.MessageId} Retry attempt: {mySbMsg.SystemProperties.DeliveryCount}");
            string converted = Encoding.UTF8.GetString(mySbMsg.Body, 0, mySbMsg.Body.Length);
            
            RequestCommunicationRequest requestCommunicationRequest  = JsonConvert.DeserializeObject<RequestCommunicationRequest>(converted);
            AddCommunicationRequestToCosmos(mySbMsg, "start", requestCommunicationRequest);
            IMessage message = _messageFactory.Create(requestCommunicationRequest);
            List<SendMessageRequest> messageDetails = await message.IdentifyRecipients(requestCommunicationRequest.RecipientUserID, requestCommunicationRequest.JobID, requestCommunicationRequest.GroupID, requestCommunicationRequest.RequestID, requestCommunicationRequest.AdditionalParameters);

            if (messageDetails.Count == 0)
            {
                log.LogInformation("No recipients identified");
                AddCommunicationRequestToCosmos(mySbMsg, "No recipients identified", requestCommunicationRequest);
            }
            else
            {
                AddCommunicationRequestToCosmos(mySbMsg, $"potential recipients {messageDetails.Count}", requestCommunicationRequest);
                var rec = JsonConvert.SerializeObject(messageDetails);
                log.LogInformation($"Recipients { rec}");
            }

            Guid batchId = Guid.NewGuid();

            foreach (var m in messageDetails)
            {
                await _messageFactory.AddToMessageQueueAsync(new SendMessageRequest()
                {
                    BatchID = batchId,
                    CommunicationJobType = requestCommunicationRequest.CommunicationJob.CommunicationJobType,
                    TemplateName = m.TemplateName,
                    RecipientUserID = m.RecipientUserID,
                    JobID = m.JobID,
                    GroupID = m.GroupID,
                    MessageType = MessageTypes.Email,
                    RequestID = m.RequestID,
                    AdditionalParameters =  m.AdditionalParameters
                });
            }

            log.LogInformation($"End ProcessJobQueue id: {mySbMsg.MessageId}");
            AddCommunicationRequestToCosmos(mySbMsg, "finished", requestCommunicationRequest);
        }

        private void AddCommunicationRequestToCosmos(Message mySbMsg, string status, RequestCommunicationRequest requestCommunicationRequest)
        {
            try
            {
                dynamic message;

                message = new ExpandoObject();
                message.id = Guid.NewGuid();
                message.QueueName = "job";
                message.MessageId = mySbMsg.MessageId;
                message.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;
                message.Status = status;

                if (requestCommunicationRequest != null)
                {
                    message.RecipientUserID = requestCommunicationRequest.RecipientUserID;
                    message.JobId = requestCommunicationRequest.JobID;
                    message.CommunicationJob = requestCommunicationRequest.CommunicationJob.CommunicationJobType;
                    message.GroupId = requestCommunicationRequest.GroupID;
                }

                _cosmosDbService.AddItemAsync(message);
            }
            catch (Exception exc)
            {
                string m = exc.ToString();
            }
        }
    }
}
