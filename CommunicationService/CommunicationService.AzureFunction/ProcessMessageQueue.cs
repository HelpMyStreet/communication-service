using CommunicationService.Core.Domains;
using CommunicationService.Core.Interfaces;
using CommunicationService.Core.Interfaces.Repositories;
using CommunicationService.Core.Interfaces.Services;
using HelpMyStreet.Contracts.RequestService.Response;
using HelpMyStreet.Utils.Exceptions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;

public class ProcessMessageQueue
{
    private readonly IMessageFactory _messageFactory;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IConnectSendGridService _connectSendGridService;

    public ProcessMessageQueue(IMessageFactory messageFactory, ICosmosDbService cosmosDbService, IConnectSendGridService connectSendGridService)
    {
        _messageFactory = messageFactory;
        _cosmosDbService = cosmosDbService;
        _connectSendGridService = connectSendGridService;        
    }

    [FunctionName("ProcessMessageQueue")]
    public async Task Run([ServiceBusTrigger("message", Connection = "ServiceBus")]Message mySbMsg, ILogger log)
    {
        LogDetails logDetails = new LogDetails() { Queue = "Message" };
        logDetails.Started = DateTime.Now;
        logDetails.MessageId = mySbMsg.MessageId;
        logDetails.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;        

        SendMessageRequest sendMessageRequest = null;

        bool emailAlreadySent = await _cosmosDbService.EmailSent(mySbMsg.MessageId);

        if (emailAlreadySent)
        {
            logDetails.Status = "email already sent";
        }
        else
        {
            try
            {
                string converted = Encoding.UTF8.GetString(mySbMsg.Body, 0, mySbMsg.Body.Length);

                sendMessageRequest = JsonConvert.DeserializeObject<SendMessageRequest>(converted);

                logDetails.Job = Enum.GetName(typeof(CommunicationJobTypes), sendMessageRequest.CommunicationJobType);
                logDetails.RecipientUserId = sendMessageRequest.RecipientUserID;

                IMessage message = _messageFactory.Create(sendMessageRequest);

                EmailBuildData emailBuildData = await message.PrepareTemplateData(sendMessageRequest.BatchID, sendMessageRequest.RecipientUserID, sendMessageRequest.JobID, sendMessageRequest.GroupID, sendMessageRequest.RequestID, sendMessageRequest.AdditionalParameters, sendMessageRequest.TemplateName);
                
                if (emailBuildData != null)
                {
                    emailBuildData.JobID = emailBuildData.JobID.HasValue ? emailBuildData.JobID : sendMessageRequest.JobID;
                    emailBuildData.GroupID = emailBuildData.GroupID.HasValue ? emailBuildData.GroupID : sendMessageRequest.GroupID;
                    emailBuildData.RecipientUserID = sendMessageRequest.RecipientUserID;
                    emailBuildData.RequestID = emailBuildData.RequestID.HasValue ? emailBuildData.RequestID : sendMessageRequest.RequestID;
                    //var result = await _connectSendGridService.SendDynamicEmail(mySbMsg.MessageId, sendMessageRequest.TemplateName, message.GetUnsubscriptionGroupName(sendMessageRequest.RecipientUserID), emailBuildData);
                    //logDetails.Status = $"SendDynamicEmail: {result}";
                }
                else
                {
                    logDetails.Status = "no emailBuildData";
                }
            }
            catch (AggregateException exc)
            {
                RetryHandler(mySbMsg, sendMessageRequest, exc.InnerException, log);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Calling retry handler...");

                // Manage retries using our message retry handler
                RetryHandler(mySbMsg, sendMessageRequest, ex, log);
            }
        }

        logDetails.Finished = DateTime.Now;
        string json = JsonConvert.SerializeObject(logDetails);

        log.LogInformation(json);
    }

    public void RetryHandler(Message mySbMsg, SendMessageRequest sendMessageRequest, Exception ex, ILogger log)
    {
        log.LogError(ex.ToString());
        AddErrorToCosmos(ex, mySbMsg, sendMessageRequest);
        RetryPolicy policy = new RetryPolicy()
        {
            MaxRetryCount = 3,
            RetryInterval = 2000
        };

        if (ex.GetType() == typeof(BadRequestException))
        {
            policy = new RetryPolicy()
            {
                MaxRetryCount = 0,
                RetryInterval = 0
            };
        }
        // Protocol errors policy
        else if (ex.GetType() == typeof(TimeoutException))
        {
            policy = new RetryPolicy()
            {
                MaxRetryCount = 5,
                RetryInterval = 3000
            };
        }

        // Check message delivery count against policy
        if (mySbMsg.SystemProperties.DeliveryCount >= policy.MaxRetryCount)
        {
            // Log action taken
            log.LogInformation($"RetryHandler: Max delivery attempts {mySbMsg.SystemProperties.DeliveryCount} exceeded");
        }
        else
        {
            // Log action taken
            log.LogInformation($"RetryHandler: Requeuing message. Delivery attempts {mySbMsg.SystemProperties.DeliveryCount} of {policy.MaxRetryCount}");

            // Sleep for the defined interval
            System.Threading.Thread.Sleep(policy.RetryInterval);

            // Throw the exception to trigger requeue
            throw ex;
        }
    }

    private void AddCommunicationRequestToCosmos(Message mySbMsg, string status, SendMessageRequest sendMessageRequest, string emailAddress)
    {
        try
        {
            dynamic message;

            message = new ExpandoObject();
            message.id = Guid.NewGuid();
            message.QueueName = "message";
            message.MessageId = mySbMsg.MessageId;
            message.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;
            message.Status = status;

            if (sendMessageRequest != null)
            {
                message.RecipientUserID = sendMessageRequest.RecipientUserID;
                message.TemplateName = sendMessageRequest.TemplateName;
                message.JobId = sendMessageRequest.JobID;
                message.CommunicationJob = sendMessageRequest.CommunicationJobType;
                message.GroupId = sendMessageRequest.GroupID;
                message.BatchId = sendMessageRequest.BatchID.ToString();
                message.RequestId = sendMessageRequest.RequestID;
            }

            if(!string.IsNullOrEmpty(emailAddress))
            {
                message.EmailAddress = emailAddress;
            }
            _cosmosDbService.AddItemAsync(message);
        }
        catch (Exception exc)
        {
            string m = exc.ToString();
        }
    }

    private void AddErrorToCosmos(Exception ex,Message mySbMsg, SendMessageRequest sendMessageRequest)
    {
        try
        {
            dynamic message;

            message = new ExpandoObject();
            message.id = Guid.NewGuid();
            message.QueueName = "message";
            message.MessageId = mySbMsg.MessageId;
            message.DeliveryCount = mySbMsg.SystemProperties.DeliveryCount;
            message.Error =ex.ToString();
            message.StackTrack = ex.StackTrace;

            if (sendMessageRequest != null)
            {
                message.RecipientUserID = sendMessageRequest.RecipientUserID;
                message.TemplateName = sendMessageRequest.TemplateName;
                message.BatchId = sendMessageRequest.BatchID.ToString();
            }
            _cosmosDbService.AddItemAsync(message);
        }
        catch (Exception exc)
        {
            string m = exc.ToString();
        }
    }

}
public class RetryPolicy
{
    public int MaxRetryCount { get; set; }
    public int RetryInterval { get; set; }
}
