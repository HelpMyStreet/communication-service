using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using CommunicationService.Core.Configuration;
using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.CommunicationService.Request;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Utils.Enums;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Newtonsoft.Json;

namespace CommunicationService.AzureFunction
{
    public class ProcessJobQueue
    {
        private readonly IMessageFactory _messageFactory;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IOptions<ServiceBusConfig> _serviceBusConfig;
        private LogDetails _logDetails;

        public ProcessJobQueue(IMessageFactory messageFactory, ICosmosDbService cosmosDbService, IOptions<ServiceBusConfig> serviceBusConfig)
        {
            _messageFactory = messageFactory;
            _cosmosDbService = cosmosDbService;
            _serviceBusConfig = serviceBusConfig;
            _logDetails = new LogDetails() { Queue = "Job" };
                
        }

        [FunctionName("ProcessJobQueue")]
        public async Task Run([ServiceBusTrigger("job", Connection = "ServiceBus")]Message mySbMsg, ILogger log)
        {
            _logDetails.Started = DateTime.Now;
            _logDetails.MessageId = mySbMsg.MessageId;
            _logDetails.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;

            try
            {
                string converted = Encoding.UTF8.GetString(mySbMsg.Body, 0, mySbMsg.Body.Length);
                RequestCommunicationRequest requestCommunicationRequest = JsonConvert.DeserializeObject<RequestCommunicationRequest>(converted);

                _logDetails.Job = Enum.GetName(typeof(CommunicationJobTypes), requestCommunicationRequest.CommunicationJob.CommunicationJobType);

                IMessage message = _messageFactory.Create(requestCommunicationRequest);
                List<SendMessageRequest> messageDetails = await message.IdentifyRecipients(requestCommunicationRequest.RecipientUserID, requestCommunicationRequest.JobID, requestCommunicationRequest.GroupID, requestCommunicationRequest.RequestID, requestCommunicationRequest.AdditionalParameters);

                if (messageDetails.Count == 0)
                {
                    log.LogInformation("No recipients identified");
                    _logDetails.PotentialRecipientCount = 0;
                }
                else
                {
                    _logDetails.PotentialRecipientCount = messageDetails.Count;
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
                        AdditionalParameters = m.AdditionalParameters
                    });
                    System.Threading.Thread.Sleep(_serviceBusConfig.Value.ProcessQueueSleepInMilliSeconds);

                }
            }
            catch(Exception exc)
            {
                await LogAndAddToCosmos(log);
                throw exc;
            }

            await LogAndAddToCosmos(log);
        }

        private async Task LogAndAddToCosmos(ILogger log)
        {
            _logDetails.Finished = DateTime.Now;
            string jsonLogging = JsonConvert.SerializeObject(_logDetails);
            log.LogInformation(jsonLogging);
            await _cosmosDbService.AddItemAsync(_logDetails);
        }
    }
}
