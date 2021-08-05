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

        public ProcessJobQueue(IMessageFactory messageFactory, ICosmosDbService cosmosDbService)
        {
            _messageFactory = messageFactory;
            _cosmosDbService = cosmosDbService;
        }

        [FunctionName("ProcessJobQueue")]
        public async Task Run([ServiceBusTrigger("job", Connection = "ServiceBus")]Message mySbMsg, ILogger log)
        {
            LogDetails logDetails = new LogDetails() { Queue = "Job" };
            logDetails.Started = DateTime.Now;
            logDetails.MessageId = mySbMsg.MessageId;
            logDetails.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;

            try
            {
                string converted = Encoding.UTF8.GetString(mySbMsg.Body, 0, mySbMsg.Body.Length);
                RequestCommunicationRequest requestCommunicationRequest = JsonConvert.DeserializeObject<RequestCommunicationRequest>(converted);

                logDetails.Job = Enum.GetName(typeof(CommunicationJobTypes), requestCommunicationRequest.CommunicationJob.CommunicationJobType);

                IMessage message = _messageFactory.Create(requestCommunicationRequest);
                List<SendMessageRequest> messageDetails = await message.IdentifyRecipients(requestCommunicationRequest.RecipientUserID, requestCommunicationRequest.JobID, requestCommunicationRequest.GroupID, requestCommunicationRequest.RequestID, requestCommunicationRequest.AdditionalParameters);

                if (messageDetails.Count == 0)
                {
                    log.LogInformation("No recipients identified");
                    logDetails.PotentialRecipientCount = 0;
                }
                else
                {
                    logDetails.PotentialRecipientCount = messageDetails.Count;
                    var rec = JsonConvert.SerializeObject(messageDetails);
                    log.LogInformation($"Recipients { rec}");  // Can we see this anywhere?  could be useful for testing one of our theories.
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
                }
            }
            catch(Exception exc)
            {
                await LogAndAddToCosmos(log, logDetails, "error", exc);
                throw exc;
            }

            await LogAndAddToCosmos(log,logDetails);
        }

        private async Task LogAndAddToCosmos(ILogger log, LogDetails logDetails)
        {
            await LogAndAddToCosmos(log, logDetails, "completed");            
        }

        private async Task LogAndAddToCosmos(ILogger log, LogDetails logDetails, string status, Exception exc = null)
        {
            logDetails.Finished = DateTime.Now;
            logDetails.Status = status;

            if(exc!=null)
            {
                logDetails.ErrorDetails = exc.ToString();
            }

            string jsonLogging = JsonConvert.SerializeObject(logDetails);
            log.LogInformation(jsonLogging);
            await _cosmosDbService.AddItemAsync(logDetails);
        }
    }
}
